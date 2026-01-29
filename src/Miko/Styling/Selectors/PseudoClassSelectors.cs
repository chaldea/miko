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
