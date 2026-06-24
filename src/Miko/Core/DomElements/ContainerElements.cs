namespace Miko.Core.DomElements;

/// <summary>
/// Div 容器元素
/// </summary>
public class DivElement : Element
{
    public override string TagName => "div";
}

/// <summary>
/// Span 行内元素
/// </summary>
public class SpanElement : Element
{
    public override string TagName => "span";
}

/// <summary>
/// 段落元素
/// </summary>
public class ParagraphElement : Element
{
    public override string TagName => "p";
}

/// <summary>
/// Nav 导航元素
/// </summary>
public class NavElement : Element
{
    public override string TagName => "nav";
}

/// <summary>
/// Strong 加粗元素
/// </summary>
public class StrongElement : Element
{
    public override string TagName => "strong";
}

/// <summary>
/// 透明片段容器（非用户书写）。当一个组件/页面产出多个顶层元素时，
/// <see cref="Miko.Components.RenderTreeBuilder"/> 用它来承载这些元素。
/// 它留在 DOM 树中作为多根组件的稳定根（供 StateHasChanged 原地重渲染），但对布局透明：
/// LayoutEngine 不为其自身建盒，而是把其子节点的布局盒摊平进父级，等价于 CSS 的
/// <c>display: contents</c>，因此不会引入任何破坏样式的包裹层。仅当片段本身是布局根
/// （无 Layout 的多根页面）时，才按普通块盒充当 issue 所允许的"自动创建的根包裹"。
/// </summary>
public class FragmentElement : Element
{
    public override string TagName => "fragment";
}
