using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.Styling.Selectors;

/// <summary>
/// <c>:is()</c> 伪类选择器（ISSUE-158）：参数列表中任一选择器匹配即匹配。参数可以是带组合器的复杂选择器
/// （<c>:is(.toolbar .title, h1)</c>），它们以本元素为主体求值；多支参数以 <see cref="GroupSelector"/> 承载。
/// 特异性取参数中最具体的一支（与 <see cref="NotSelector"/> 相同）。
/// </summary>
public class IsSelector : PseudoClassSelector
{
    public IsSelector(Selector inner) => Inner = inner;

    public Selector Inner { get; }

    public override bool Matches(Element element) => Inner.Matches(element);
    public override int Specificity => Inner.Specificity;
    public override bool MayReadContentOrInlineStyle => Inner.MayReadContentOrInlineStyle;
}

/// <summary>
/// <c>:where()</c> 伪类选择器（ISSUE-158）：匹配与 <see cref="IsSelector"/> 完全相同，但特异性恒为 0，
/// 用来写可被任何规则覆盖的默认样式（<c>:where(.card) .title</c> 的特异性只算 <c>.title</c>）。
/// </summary>
public class WhereSelector : PseudoClassSelector
{
    public WhereSelector(Selector inner) => Inner = inner;

    public Selector Inner { get; }

    public override bool Matches(Element element) => Inner.Matches(element);
    public override int Specificity => 0;
    public override bool MayReadContentOrInlineStyle => Inner.MayReadContentOrInlineStyle;
}

/// <summary>
/// <c>:has()</c> 伪类选择器（ISSUE-158）：以本元素为锚点，参数中任一<see cref="RelativeSelector">相对选择器</see>
/// 能找到匹配的元素即匹配。<c>.card:has(img)</c> 是含 img 后代的 .card；<c>:has(&gt; .a)</c> 看子元素；
/// <c>:has(+ .a)</c> 看紧邻的后一个兄弟；<c>:has(~ .a)</c> 看之后的任一兄弟。特异性取参数中最具体的一支。
///
/// <para>无需额外的失效记账：后代与后续兄弟的结构、类名、状态变化都按结构变更触发整树级联，锚点随之重新匹配。
/// 悬停相关性分析把参数里的 <c>:hover</c> 归到被悬停的那个元素上（见 <c>HoverRelevance</c>）。</para>
///
/// <para>代价：每次求值都要遍历锚点的子树（或后续兄弟），找到即停。写进复合选择器让类名先过滤
/// （<c>.card:has(…)</c> 按 .card 入规则索引的桶），不要单独写 <c>:has(…)</c>——那会进通用桶，
/// 让每个元素都遍历一遍自己的子树。</para>
/// </summary>
public class HasSelector : PseudoClassSelector
{
    private readonly RelativeSelector[] _arguments;

    public HasSelector(IEnumerable<RelativeSelector> arguments)
    {
        _arguments = arguments.ToArray();
        if (_arguments.Length == 0)
            throw new ArgumentException(":has() requires at least one relative selector", nameof(arguments));
    }

    public HasSelector(params RelativeSelector[] arguments) : this((IEnumerable<RelativeSelector>)arguments) { }

    public IReadOnlyList<RelativeSelector> Arguments => _arguments;

    public override bool Matches(Element element)
    {
        if (element is TextNode) return false;
        for (int i = 0; i < _arguments.Length; i++)
        {
            if (_arguments[i].MatchesFrom(element)) return true;
        }
        return false;
    }

    public override int Specificity
    {
        get
        {
            int max = 0;
            for (int i = 0; i < _arguments.Length; i++)
            {
                int s = _arguments[i].Specificity;
                if (s > max) max = s;
            }
            return max;
        }
    }

    public override bool MayReadContentOrInlineStyle
    {
        get
        {
            for (int i = 0; i < _arguments.Length; i++)
            {
                if (_arguments[i].MayReadContentOrInlineStyle) return true;
            }
            return false;
        }
    }
}

/// <summary>相对选择器中一步所用的组合器。</summary>
public enum SelectorCombinator
{
    /// <summary>后代（空格）。</summary>
    Descendant,

    /// <summary>子代（<c>&gt;</c>）。</summary>
    Child,

    /// <summary>紧邻的后一个兄弟（<c>+</c>）。</summary>
    NextSibling,

    /// <summary>之后的任一兄弟（<c>~</c>）。</summary>
    SubsequentSibling,
}

/// <summary>相对选择器的一步：从上一步的元素经 <see cref="Combinator"/> 找到匹配 <see cref="Compound"/> 的元素。</summary>
public readonly record struct RelativeSelectorStep(SelectorCombinator Combinator, Selector Compound);

/// <summary>
/// <see cref="HasSelector"/> 的一个参数：从锚点出发的一串（组合器, 复合选择器）步骤。<c>&gt; .a .b</c> 为
/// [(子代, .a), (后代, .b)]；省略开头的组合器即后代（<c>:has(img)</c>）。
///
/// <para><b>自左向右</b>求值：每一步按组合器从上一步的元素枚举候选（子元素、全部后代、下一个或之后的兄弟），
/// 候选匹配复合选择器后进入下一步，最后一步匹配即成功。这样锚点身份只沿递归参数传递，选择器对象本身
/// 不持有任何可变状态，多个引擎在各自线程上共用同一张样式表也安全。文本节点不参与（它们不是元素）。</para>
/// </summary>
public sealed class RelativeSelector
{
    private readonly RelativeSelectorStep[] _steps;

    public RelativeSelector(IEnumerable<RelativeSelectorStep> steps)
    {
        _steps = steps.ToArray();
        if (_steps.Length == 0)
            throw new ArgumentException("A relative selector needs at least one step", nameof(steps));
    }

    public RelativeSelector(params RelativeSelectorStep[] steps) : this((IEnumerable<RelativeSelectorStep>)steps) { }

    public IReadOnlyList<RelativeSelectorStep> Steps => _steps;

    /// <summary>各步复合选择器的特异性之和（与等价的普通组合器选择器一致）。</summary>
    public int Specificity
    {
        get
        {
            int sum = 0;
            for (int i = 0; i < _steps.Length; i++) sum += _steps[i].Compound.Specificity;
            return sum;
        }
    }

    public bool MayReadContentOrInlineStyle
    {
        get
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                if (_steps[i].Compound.MayReadContentOrInlineStyle) return true;
            }
            return false;
        }
    }

    /// <summary>以 <paramref name="anchor"/> 为锚点，能否走完全部步骤。</summary>
    public bool MatchesFrom(Element anchor) => MatchStep(anchor, 0);

    private bool MatchStep(Element from, int step)
    {
        switch (_steps[step].Combinator)
        {
            case SelectorCombinator.Child:
            {
                var children = from.Children;
                for (int i = 0; i < children.Count; i++)
                {
                    if (Accepts(children[i], step)) return true;
                }
                return false;
            }
            case SelectorCombinator.Descendant:
                return AnyDescendant(from, step);
            case SelectorCombinator.NextSibling:
            case SelectorCombinator.SubsequentSibling:
            {
                var siblings = from.Parent?.Children;
                if (siblings == null) return false;
                bool nextOnly = _steps[step].Combinator == SelectorCombinator.NextSibling;
                for (int i = siblings.IndexOf(from) + 1; i < siblings.Count; i++)
                {
                    var sibling = siblings[i];
                    if (sibling is TextNode) continue;
                    if (Accepts(sibling, step)) return true;
                    if (nextOnly) return false;
                }
                return false;
            }
            default:
                return false;
        }
    }

    private bool AnyDescendant(Element from, int step)
    {
        var children = from.Children;
        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child is TextNode) continue;
            if (Accepts(child, step) || AnyDescendant(child, step)) return true;
        }
        return false;
    }

    // 候选匹配本步的复合选择器，且（若还有后续步骤）能从它走完剩下的步骤。
    private bool Accepts(Element candidate, int step)
    {
        if (candidate is TextNode || !_steps[step].Compound.Matches(candidate)) return false;
        return step == _steps.Length - 1 || MatchStep(candidate, step + 1);
    }
}
