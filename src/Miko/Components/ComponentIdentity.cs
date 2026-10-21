using System.Diagnostics.CodeAnalysis;

namespace Miko.Components;

/// <summary>
/// Where in a component's render a nested component sits, used to decide whether the instance from
/// the previous render can be reused (ISSUE-154 BUG-01). A parent re-render keeps a child instance
/// — its fields, subscriptions and pending async work — exactly when the child's identity is
/// unchanged; otherwise the old instance is disposed and a new one is created.
///
/// <list type="bullet">
/// <item><see cref="Scope"/>: the chain of render-fragment call sites and keyed elements enclosing
/// the component. Fragments (<c>@ChildContent</c>, templates) are rendered by the component that
/// receives them, so their components share that component's render; the call-site scope keeps the
/// sequence numbers of different files apart. A keyed element (<c>&lt;div @key="x"&gt;</c>) scopes
/// everything inside it, so the components in a keyed list item follow the item.</item>
/// <item><see cref="Type"/> and <see cref="Sequence"/>: the call site. The Razor compiler numbers
/// every <c>OpenComponent</c> call; hand-written code that passes no number uses -1.</item>
/// <item><see cref="Key"/>: the <c>@key</c>, when given. Keyed components match by key regardless of
/// their position, so inserting, removing or reordering items keeps each item's instance.</item>
/// <item><see cref="Ordinal"/>: for unkeyed components, how many earlier components of the same
/// call site this render has opened — i.e. matching by position, as in Blazor without <c>@key</c>.</item>
/// </list>
/// </summary>
internal readonly record struct ComponentIdentity(RenderScope? Scope, Type Type, int Sequence, object? Key, int Ordinal);

/// <summary>
/// One level of identity scope: a render-fragment call site (<see cref="Sequence"/>) or a keyed
/// element (<see cref="Key"/>), nested in <see cref="Parent"/>. Compared by value, so the scope of
/// the same call site in two renders is equal.
/// </summary>
internal sealed record RenderScope(RenderScope? Parent, int Sequence, object? Key);

/// <summary>
/// The nested component instances one render of a component produced, by identity. The next render
/// claims instances from it; whatever is left unclaimed was dropped by that render.
/// </summary>
internal sealed class ComponentRegistry
{
    private readonly Dictionary<ComponentIdentity, ComponentBase> _components = new();
    private Dictionary<(RenderScope? Scope, Type Type, int Sequence), int>? _ordinals;

    public int Count => _components.Count;

    public IEnumerable<ComponentBase> Components => _components.Values;

    /// <summary>Removes and returns the instance registered under <paramref name="identity"/>.</summary>
    public bool TryClaim(in ComponentIdentity identity, [NotNullWhen(true)] out ComponentBase? component)
        => _components.Remove(identity, out component);

    public bool Contains(in ComponentIdentity identity) => _components.ContainsKey(identity);

    /// <summary>Registers <paramref name="component"/>; false when the identity is already taken.</summary>
    public bool TryAdd(in ComponentIdentity identity, ComponentBase component)
        => _components.TryAdd(identity, component);

    /// <summary>Empties the registry so the next render can reuse its storage.</summary>
    public void Clear()
    {
        _components.Clear();
        _ordinals?.Clear();
    }

    /// <summary>The position of the next unkeyed component opened at this call site in this render.</summary>
    public int NextOrdinal(RenderScope? scope, Type type, int sequence)
    {
        _ordinals ??= new();
        var site = (scope, type, sequence);
        _ordinals.TryGetValue(site, out var ordinal);
        _ordinals[site] = ordinal + 1;
        return ordinal;
    }
}
