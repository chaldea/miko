using Miko.Styling;

namespace Miko.Components;

/// <summary>
/// The style rules the Razor compiler generates from components' CSS (ISSUE-161): the content of
/// their <c>&lt;style&gt;</c> elements and of their <c>.razor.css</c> files. The CSS is translated
/// into C# when the component is compiled; at run time a component registers its rules here the
/// first time it renders, and they stay registered for the life of the app — as the browser keeps a
/// Blazor app's bundled component CSS loaded.
///
/// <para>One registry per DI container, so per app and engine (ISSUE-129): <see cref="StyleSheet"/>
/// is added to the app's style sheets by <c>MikoAppBuilder</c> and to a test's by
/// <c>Miko.Testing.TestContext</c>. A component reaches it through the service provider in scope
/// while it renders; a component built with no service provider in scope registers nothing.</para>
///
/// <para>Rules of different components sit in one sheet at layer 0, after the app's own sheets,
/// in the order the components first rendered — the order a Blazor app's bundled CSS would have
/// is not known at run time either. Scoped (<c>.razor.css</c>) rules only match their own
/// component's elements, so the order matters for <c>&lt;style&gt;</c> rules and <c>::deep</c>
/// rules alone.</para>
/// </summary>
public sealed class ComponentStyleRegistry
{
    private readonly object _gate = new();
    private readonly Dictionary<Type, Entry> _entries = new();
    private readonly List<Type> _order = new();

    private sealed record Entry(string Hash, StyleSheet Rules);

    /// <summary>The sheet holding every registered component's rules. Its identity never changes.</summary>
    public StyleSheet StyleSheet { get; } = new();

    /// <summary>
    /// Registers the rules of <paramref name="componentType"/> unless they are registered already.
    /// <paramref name="hash"/> identifies the content: a component recompiled with different CSS
    /// (hot reload) replaces its previous rules instead of adding to them.
    /// </summary>
    public void Register(Type componentType, string hash, Func<StyleSheet> build)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        ArgumentNullException.ThrowIfNull(build);

        lock (_gate)
        {
            if (_entries.TryGetValue(componentType, out var existing))
            {
                if (existing.Hash == hash) return;

                _entries[componentType] = new Entry(hash, build());
                StyleSheet.ReplaceRules(_order.Select(type => _entries[type].Rules));
                return;
            }

            var entry = new Entry(hash, build());
            _entries.Add(componentType, entry);
            _order.Add(componentType);
            StyleSheet.AppendRules(entry.Rules);
        }
    }

    /// <summary>Whether <paramref name="componentType"/> has registered its rules.</summary>
    public bool IsRegistered(Type componentType)
    {
        lock (_gate)
        {
            return _entries.ContainsKey(componentType);
        }
    }
}
