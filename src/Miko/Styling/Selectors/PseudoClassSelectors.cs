using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.Styling.Selectors;

/// <summary>
/// 伪类选择器基类
/// </summary>
public abstract class PseudoClassSelector : Selector
{
    public override int Specificity => 10; // 与类选择器相同（CSS规范）

    // 内置伪类只读元素状态或树结构（:empty 只看空与非空，见 Selector 的说明）。
    public override bool MayReadContentOrInlineStyle => false;
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
/// :focus-within 伪类选择器：元素自身或其任一后代持有焦点。
/// <para>焦点变化经 <c>SetState</c> 递增变更版本、触发整树级联，祖先因此会重新匹配，无需额外记账。
/// 匹配时向下查找，找到即停；它几乎总出现在组合器左侧（<c>.ion-input:focus-within .input-highlight</c>），
/// 只有右侧先命中时才会被求值，代价是一个组件子树的遍历。Ionic 的 <c>has-focus</c> 类由组件在焦点事件里
/// 维护，Miko 的组件实例随重渲染重建，类会丢，故改用它（ISSUE-153）。</para>
/// </summary>
public class FocusWithinSelector : PseudoClassSelector
{
    public override bool Matches(Element element) => ContainsFocus(element);

    private static bool ContainsFocus(Element element)
    {
        if (element.HasState(ElementState.Focus)) return true;
        var children = element.Children;
        for (int i = 0; i < children.Count; i++)
        {
            if (ContainsFocus(children[i])) return true;
        }
        return false;
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
/// :only-child 伪类选择器：既是第一个也是最后一个元素子节点（ISSUE-161）。单个伪类的特异性，
/// 不能写成 <c>:first-child:last-child</c>（那是两个伪类的特异性）。
/// </summary>
public class OnlyChildSelector : PseudoClassSelector
{
    private static readonly FirstChildSelector s_first = new();
    private static readonly LastChildSelector s_last = new();

    public override bool Matches(Element element) => s_first.Matches(element) && s_last.Matches(element);
}

/// <summary>:only-of-type 伪类选择器：同标签兄弟中唯一的一个（ISSUE-161）。</summary>
public class OnlyOfTypeSelector : PseudoClassSelector
{
    private static readonly FirstOfTypeSelector s_first = new();
    private static readonly LastOfTypeSelector s_last = new();

    public override bool Matches(Element element) => s_first.Matches(element) && s_last.Matches(element);
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
    public override bool MayReadContentOrInlineStyle => _inner.MayReadContentOrInlineStyle;
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
/// :root 伪类选择器：匹配没有父元素的元素，即所在树的根（ISSUE-161）。CSS 里它常用来声明全局变量
/// （<c>:root { --brand: #0054e9; }</c>），变量沿树继承，因此根上的定义对整棵树可见。
/// Miko 没有 <c>html</c> 元素，应用的根组件产出的元素就是根；转场期间两棵页面树各有一个根。
/// </summary>
public class RootSelector : PseudoClassSelector
{
    public override bool Matches(Element element) => element.Parent == null;
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
