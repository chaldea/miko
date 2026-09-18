using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Miko.Components;

/// <summary>
/// Per-component-type cache of the reflection needed to populate
/// <see cref="CascadingParameterAttribute"/> and <see cref="InjectAttribute"/> properties
/// (ISSUE-136).
///
/// <para>Without it, <see cref="ComponentBase.Build"/> ran
/// <c>GetProperties(Public | NonPublic | Instance)</c> plus a
/// <c>GetCustomAttribute</c> per property on <em>every</em> render, and
/// <see cref="ComponentBase.StateHasChanged"/> renders on every frame that changes state.
/// Attribute lookup is by far the expensive part — it materializes attribute instances — and the
/// answer is a property of the type, not of the instance, so it is computed once per type.</para>
///
/// <para>Property writes go through a bound <see cref="Action{T1, T2}"/> rather than
/// <see cref="PropertyInfo.SetValue"/>, removing the per-call member lookup and the argument-array
/// allocation. The value stays typed as <see cref="object"/> because both sources (a DI container
/// and the cascading-value stack) hand back <see cref="object"/> already — so this introduces no
/// boxing that was not already there.</para>
///
/// <para>Callers must flow <see cref="ComponentTypeMembers.Parameters"/> onto the
/// <see cref="Type"/> they pass in, or the trimmer removes the very properties this looks for
/// (ISSUE-140).</para>
/// </summary>
internal static class ComponentParameterCache
{
    /// <summary>One settable property plus its pre-built setter.</summary>
    internal readonly struct ParameterSetter
    {
        internal ParameterSetter(PropertyInfo property, Action<object, object?> setValue, string? name)
        {
            Property = property;
            SetValue = setValue;
            Name = name;
        }

        /// <summary>The property itself; its <see cref="PropertyInfo.PropertyType"/> is the lookup key.</summary>
        public PropertyInfo Property { get; }

        public Action<object, object?> SetValue { get; }

        /// <summary>The cascading value's name, when the attribute declared one.</summary>
        public string? Name { get; }
    }

    private sealed record Descriptor(ParameterSetter[] Cascading, ParameterSetter[] Injected);

    // 以类型为键，全进程共享。组件类型数量有限且生命周期与程序集相同，故用普通字典 +
    // 读多写少的复制写入，避免每次渲染都走锁。
    private static readonly object s_gate = new();
    private static Dictionary<Type, Descriptor> s_descriptors = new();

    internal static ParameterSetter[] GetCascadingParameters(
        [DynamicallyAccessedMembers(ComponentTypeMembers.Parameters)] Type type)
        => GetDescriptor(type).Cascading;

    internal static ParameterSetter[] GetInjectedProperties(
        [DynamicallyAccessedMembers(ComponentTypeMembers.Parameters)] Type type)
        => GetDescriptor(type).Injected;

    private static Descriptor GetDescriptor(
        [DynamicallyAccessedMembers(ComponentTypeMembers.Parameters)] Type type)
    {
        // 读路径无锁：s_descriptors 只会被整体替换，不会就地修改。
        if (s_descriptors.TryGetValue(type, out var cached))
            return cached;

        lock (s_gate)
        {
            if (s_descriptors.TryGetValue(type, out cached))
                return cached;

            var descriptor = Build(type);
            var updated = new Dictionary<Type, Descriptor>(s_descriptors) { [type] = descriptor };
            // 发布替换后的字典（引用赋值是原子的），读路径随即看到新条目。
            Volatile.Write(ref s_descriptors, updated);
            return descriptor;
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Cascading/inject properties are declared on the component type and preserved with it.")]
    private static Descriptor Build(
        [DynamicallyAccessedMembers(ComponentTypeMembers.Parameters)] Type type)
    {
        List<ParameterSetter>? cascading = null;
        List<ParameterSetter>? injected = null;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            // 只读属性跳过（与此前一致）。
            if (!prop.CanWrite) continue;

            var cascadingAttr = prop.GetCustomAttribute<CascadingParameterAttribute>();
            if (cascadingAttr != null)
            {
                (cascading ??= new()).Add(new ParameterSetter(prop, CreateSetter(prop), cascadingAttr.Name));
            }

            if (prop.GetCustomAttribute<InjectAttribute>() != null)
            {
                (injected ??= new()).Add(new ParameterSetter(prop, CreateSetter(prop), null));
            }
        }

        return new Descriptor(
            cascading?.ToArray() ?? [],
            injected?.ToArray() ?? []);
    }

    /// <summary>
    /// Builds a setter delegate for a property. Falls back to
    /// <see cref="PropertyInfo.SetValue"/> when a delegate cannot be created — e.g. an
    /// init-only or ref-returning property, where the fallback preserves the previous behaviour
    /// rather than failing the render.
    ///
    /// <para>The fast path binds the setter <b>directly</b> as <c>Action&lt;object, object?&gt;</c>.
    /// That only type-checks when both the declaring type and the property type are reference
    /// types, because the runtime allows delegate binding to widen a reference parameter to
    /// <see cref="object"/> but never to box a value type for one — so value-typed properties take
    /// the reflection path.</para>
    ///
    /// <para>Previously this went through <c>MakeGenericMethod</c> to build a typed
    /// <c>Action&lt;TDeclaring, TValue&gt;</c>. That is unsupported under Native AOT (IL3050): it
    /// threw for <em>every</em> property, and the <c>catch</c> silently swallowed it — so AOT paid
    /// a thrown exception per property and then used the slow path anyway (ISSUE-140).</para>
    /// </summary>
    private static Action<object, object?> CreateSetter(PropertyInfo property)
    {
        var setMethod = property.GetSetMethod(nonPublic: true);
        if (setMethod == null || property.DeclaringType == null)
        {
            return property.SetValue;
        }

        // 值类型的声明类型/属性类型无法直接绑定到 object 形参（委托绑定只放宽引用类型，不装箱）。
        if (property.DeclaringType.IsValueType || property.PropertyType.IsValueType)
        {
            return property.SetValue;
        }

        try
        {
            return setMethod.CreateDelegate<Action<object, object?>>();
        }
        catch (ArgumentException)
        {
            // 签名意外不兼容（如 ref 返回、受限类型）时退回反射写入：
            // 这只是性能优化，不能因此改变可用性。
            return property.SetValue;
        }
    }
}
