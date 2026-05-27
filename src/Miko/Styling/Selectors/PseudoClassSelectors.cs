using Miko.Core;

namespace Miko.Styling.Selectors;

/// <summary>
/// 伪类选择器基类
/// </summary>
public abstract class PseudoClassSelector : Selector
{
    public override int Specificity => 10; // 与类选择器相同（CSS规范）
}

/// <summary>
/// :hover 伪类选择器
/// </summary>
public class HoverSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        return element.HasState(ElementState.Hover);
    }
}

/// <summary>
/// :active 伪类选择器
/// </summary>
public class ActiveSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        return element.HasState(ElementState.Active);
    }
}

/// <summary>
/// :focus 伪类选择器
/// </summary>
public class FocusSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        return element.HasState(ElementState.Focus);
    }
}

/// <summary>
/// :disabled 伪类选择器
/// </summary>
public class DisabledSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        return element.IsDisabled;
    }
}

/// <summary>
/// :enabled 伪类选择器（:disabled的反义）
/// </summary>
public class EnabledSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        return !element.IsDisabled;
    }
}

/// <summary>
/// :first-child 伪类选择器
/// </summary>
public class FirstChildSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        return element.Parent?.Children.FirstOrDefault() == element;
    }
}

/// <summary>
/// :last-child 伪类选择器
/// </summary>
public class LastChildSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        return element.Parent?.Children.LastOrDefault() == element;
    }
}

/// <summary>
/// :first-of-type 伪类选择器
/// </summary>
public class FirstOfTypeSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        if (element.Parent == null) return false;
        return element.Parent.Children.FirstOrDefault(e => e.TagName == element.TagName) == element;
    }
}

/// <summary>
/// :last-of-type 伪类选择器
/// </summary>
public class LastOfTypeSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        if (element.Parent == null) return false;
        return element.Parent.Children.LastOrDefault(e => e.TagName == element.TagName) == element;
    }
}

/// <summary>
/// :not() 伪类选择器
/// </summary>
public class NotSelector : PseudoClassSelector
{
    private readonly Selector _inner;

    public NotSelector(Selector inner) => _inner = inner;

    public Selector Inner => _inner;

    public override bool Matches(Element element) => !_inner.Matches(element);
    public override int Specificity => _inner.Specificity;
}

/// <summary>
/// :empty 伪类选择器 - 匹配没有子元素且没有文本内容的元素
/// </summary>
public class EmptySelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        return element.Children.Count == 0 && string.IsNullOrEmpty(element.TextContent);
    }
}
