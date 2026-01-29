namespace Miko.Core.DomElements;

/// <summary>
/// 按钮元素
/// </summary>
public class ButtonElement : Element
{
    public override string TagName => "button";
}

/// <summary>
/// 输入框元素
/// </summary>
public class InputElement : Element
{
    public override string TagName => "input";
    public string? Value { get; set; }
    public string? Placeholder { get; set; }
}
