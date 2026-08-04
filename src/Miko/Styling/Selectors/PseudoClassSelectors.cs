using Miko.Core;
using Miko.Core.DomElements;

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
/// :first-child 伪类选择器。
/// 依据 CSS 规范，子序号只计元素子节点，文本节点（TextNode）不计入（见 ISSUE-086）。
/// </summary>
public class FirstChildSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        // 索引循环而非 LINQ（样式解析热路径，见 ISSUE-113）。
        var siblings = element.Parent?.Children;
        if (siblings == null) return false;
        for (int i = 0; i < siblings.Count; i++)
        {
            if (siblings[i] is TextNode) continue;
            return ReferenceEquals(siblings[i], element);
        }
        return false;
    }
}

/// <summary>
/// :last-child 伪类选择器。文本节点不计入子序号（见 ISSUE-086）。
/// </summary>
public class LastChildSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        var siblings = element.Parent?.Children;
        if (siblings == null) return false;
        for (int i = siblings.Count - 1; i >= 0; i--)
        {
            if (siblings[i] is TextNode) continue;
            return ReferenceEquals(siblings[i], element);
        }
        return false;
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
        var siblings = element.Parent.Children;
        for (int i = 0; i < siblings.Count; i++)
        {
            if (siblings[i].TagName == element.TagName)
                return ReferenceEquals(siblings[i], element);
        }
        return false;
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
        var siblings = element.Parent.Children;
        for (int i = siblings.Count - 1; i >= 0; i--)
        {
            if (siblings[i].TagName == element.TagName)
                return ReferenceEquals(siblings[i], element);
        }
        return false;
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
/// :empty 伪类选择器 - 匹配没有子元素且没有文本内容的元素。
/// 文本以 TextNode 子节点承载（见 ISSUE-086），因此需排除文本节点后判断是否还有元素子节点，
/// 并确认无非空直接文本（TextContent facade 聚合直接文本节点）。
/// </summary>
public class EmptySelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        // 单趟索引循环：既排除元素子节点，也检查是否存在非空文本节点。
        // 避免 LINQ 枚举器，也避免读取 TextContent facade（其 get 会拼接/分配，见 ISSUE-113）。
        var children = element.Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (children[i] is TextNode textNode)
            {
                if (!string.IsNullOrEmpty(textNode.RawTextContent)) return false;
            }
            else
            {
                return false;
            }
        }
        return true;
    }
}

/// <summary>
/// :checked 伪类选择器 - 匹配选中的 checkbox 或 radio 元素
/// </summary>
public class CheckedSelector : PseudoClassSelector
{
    public override bool Matches(Element element)
    {
        return element is InputElement input && input.Checked;
    }
}
