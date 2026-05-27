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
