namespace Miko.Components;

public delegate void RenderFragment(RenderTreeBuilder builder);

/// <summary>
/// A template that renders content for a value: <c>&lt;ItemContent Context="row"&gt;@row.Name&lt;/ItemContent&gt;</c>
/// compiles to <c>row =&gt; builder =&gt; …</c>. Same shape as Blazor's <c>RenderFragment&lt;TValue&gt;</c>.
/// </summary>
public delegate RenderFragment RenderFragment<in TValue>(TValue value);
