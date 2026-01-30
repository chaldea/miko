namespace Miko.Core.DomElements;

/// <summary>
/// 无序列表元素 (ul)
/// </summary>
public class UlElement : Element
{
    public override string TagName => "ul";
}

/// <summary>
/// 有序列表元素 (ol)
/// </summary>
public class OlElement : Element
{
    public override string TagName => "ol";

    /// <summary>
    /// 列表起始编号（默认为1）
    /// </summary>
    public int Start { get; set; } = 1;
}

/// <summary>
/// 列表项元素 (li)
/// </summary>
public class LiElement : Element
{
    public override string TagName => "li";

    /// <summary>
    /// 自定义列表项值（用于有序列表）
    /// </summary>
    public int? Value { get; set; }
}
