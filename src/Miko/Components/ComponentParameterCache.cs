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
/// <para>Property writes go through a compiled <see cref="Action{T1, T2}"/> rather than
/// <see cref="PropertyInfo.SetValue"/>, removing the per-call member lookup and the argument-array
/// allocation. The value stays typed as <see cref="object"/> because both sources (a DI container
/// and the cascading-value stack) hand back <see cref="object"/> already — so this introduces no
/// boxing that was not already there.</para>
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
        => GetDescriptor(type).Cascading;

    internal static ParameterSetter[] GetInjectedProperties(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
        => GetDescriptor(type).Injected;

    private static Descriptor GetDescriptor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
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
    /// </summary>
    private static Action<object, object?> CreateSetter(PropertyInfo property)
    {
        var setMethod = property.GetSetMethod(nonPublic: true);
        if (setMethod == null || property.DeclaringType == null)
        {
            return property.SetValue;
        }

        try
        {
            // (instance, value) => ((TDeclaring)instance).Prop = (TValue)value
            var open = typeof(ComponentParameterCache)
                .GetMethod(nameof(CreateTypedSetter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(property.DeclaringType, property.PropertyType);

            return (Action<object, object?>)open.Invoke(null, [setMethod])!;
        }
        catch (Exception)
        {
            // 任何构造失败（受限类型、AOT 裁剪掉了泛型实例化等）都退回反射写入：
            // 这只是性能优化，不能因此改变可用性。
            return property.SetValue;
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Falls back to PropertyInfo.SetValue when the generic instantiation is unavailable.")]
    [UnconditionalSuppressMessage("Trimming", "IL2060",
        Justification = "Falls back to PropertyInfo.SetValue when the generic instantiation is unavailable.")]
    private static Action<object, object?> CreateTypedSetter<TDeclaring, TValue>(MethodInfo setMethod)
    {
        var typed = (Action<TDeclaring, TValue>)Delegate.CreateDelegate(
            typeof(Action<TDeclaring, TValue>), setMethod);

        return (instance, value) => typed((TDeclaring)instance, (TValue)value!);
    }
}
