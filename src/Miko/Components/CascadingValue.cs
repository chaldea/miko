using Miko.Core;

namespace Miko.Components;

/// <summary>
/// Provides a value to all descendant components in its <see cref="ComponentBase.ChildContent"/>,
/// which they can receive via a <see cref="CascadingParameterAttribute"/>. Mirrors Blazor's
/// <c>CascadingValue&lt;TValue&gt;</c>.
/// <para>
/// Usage in a <c>.razor</c> file:
/// <code>
/// &lt;CascadingValue Value="theme"&gt;
///     &lt;ChildContent&gt;...descendants read [CascadingParameter] Theme...&lt;/ChildContent&gt;
/// &lt;/CascadingValue&gt;
/// </code>
/// </para>
/// <para>
/// This component is transparent: it renders no element of its own, only its child content.
/// It is matched by descendants either by type (<typeparamref name="TValue"/>) or, when
/// <see cref="Name"/> is set, by name.
/// </para>
/// <para>
/// Unlike Blazor there is no <c>IsFixed</c> optimization: Miko re-renders subtrees wholesale via
/// <see cref="ComponentBase.StateHasChanged"/>, so each render rebuilds the children and re-reads
/// the current <see cref="Value"/>.
/// </para>
/// </summary>
/// <typeparam name="TValue">The declared type of the cascading value.</typeparam>
public class CascadingValue<TValue> : ComponentBase
{
    /// <summary>The value to supply to descendant components.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>
    /// Optional name. When set, descendants must request this value via
    /// <c>[CascadingParameter(Name = "...")]</c>; otherwise it is matched by type.
    /// </summary>
    [Parameter] public string? Name { get; set; }

    // ChildContent is inherited from ComponentBase.

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Push the declared TValue (not Value?.GetType()) so a null value still resolves by type.
        // The children are rendered INSIDE this scope so their nested Build() calls (driven by
        // RenderTreeBuilder.CloseComponent, still on the call stack) can resolve this value.
        using var _ = CascadingValueSource.Push(Value, typeof(TValue), Name);

        // Render no element of our own — CascadingValue is transparent, like in Blazor. The child
        // content is emitted directly into the parent so it doesn't disturb layout. When the child
        // content produces several top-level elements, they are carried by a transparent
        // FragmentElement that stays in the DOM but is skipped by the layout engine (display:contents).
        ChildContent?.Invoke(builder);
    }
}
