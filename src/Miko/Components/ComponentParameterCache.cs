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
/// <para>Property writes go through a setter cached in the descriptor. It is
/// <see cref="PropertyInfo.SetValue(object, object)"/>: a typed delegate would need
/// <c>MakeGenericMethod</c>, which Native AOT does not support (see <c>CreateSetter</c>). The
/// value stays typed as <see cref="object"/> because both sources (a DI container and the
/// cascading-value stack) hand back <see cref="object"/> already.</para>
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
    /// Builds the setter for a property: <see cref="PropertyInfo.SetValue(object, object)"/>.
    ///
    /// <para>This used to try <c>setMethod.CreateDelegate&lt;Action&lt;object, object?&gt;&gt;()</c>
    /// first and fall back on <see cref="ArgumentException"/>. That fast path could never succeed:
    /// an open instance delegate binds the target as its first parameter, and delegate binding only
    /// accepts a parameter the method can <em>receive</em> — <see cref="object"/> is not assignable
    /// to the component type, so every property threw and landed on the fallback anyway. The cost
    /// was one thrown exception per injected or cascading property of every component type, paid on
    /// the frame that first builds it. On the Android Mono runtime, where a throw is far more
    /// expensive than on CoreCLR, that is exactly the frame a user sees stutter: opening the Anime
    /// detail page threw 49 of them inside a single navigation (ISSUE-146).</para>
    ///
    /// <para>A genuinely typed delegate needs <c>MakeGenericMethod</c>, which Native AOT does not
    /// support (ISSUE-140). The reflection write is what actually ran all along, so this changes no
    /// behaviour; the descriptor is still built once per type, keeping the attribute scan — the
    /// expensive part — off the render path.</para>
    /// </summary>
    private static Action<object, object?> CreateSetter(PropertyInfo property) => property.SetValue;
}
