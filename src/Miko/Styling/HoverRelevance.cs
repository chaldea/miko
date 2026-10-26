using Miko.Core;
using Miko.Styling.Selectors;

namespace Miko.Styling;

/// <summary>
/// :hover 相关性分析（ISSUE-104 问题1）。
///
/// 悬停状态（<see cref="ElementState.Hover"/>）只有在元素的 Hover 状态可能影响某条
/// 规则的选择器匹配时，才需要标脏并触发样式重算。本分析从样式表的选择器树中提取
/// 所有含 :hover 的复合选择器，并把其中的 hover 相关伪类剥离，得到一组"悬停相关
/// 模式"：只要元素匹配其中任一模式，其 Hover 状态就可能影响级联结果。
///
/// 剥离只会削弱匹配条件（如 .btn:hover → .btn，:hover → 空模式即匹配一切），
/// 因此判定是保守的——只多报、不漏报，最终匹配仍由级联按完整选择器决定。
///
/// <para>函数式伪类（ISSUE-158）的参数可能把 :hover 放在<b>别的元素</b>上：<c>.card:has(.btn:hover)</c>
/// 里被悬停的是 .btn，<c>:is(.a:hover .b)</c> 里是祖先 .a，<c>:nth-child(2 of .a:hover)</c> 里是兄弟。
/// 只剥离外层复合选择器会得到 .card 这类模式，漏掉真正被悬停的元素，因此还要深入参数为那些元素
/// 收集模式（见 <see cref="CollectNested"/>）。</para>
/// </summary>
internal static class HoverRelevance
{
    /// <summary>
    /// 从选择器树中收集悬停相关模式。每个模式是一组需全部匹配的简单选择器；
    /// 空数组表示通用模式（任意元素都相关，如裸 :hover 或 :not(:hover) 分支）。
    /// </summary>
    public static void Collect(Selector node, List<Selector[]> patterns)
    {
        switch (node)
        {
            case CompoundSelector compound:
            {
                List<Selector>? stripped = null;
                bool subjectHover = false;
                for (int i = 0; i < compound.Selectors.Count; i++)
                {
                    var simple = compound.Selectors[i];
                    if (ContainsHover(simple))
                    {
                        subjectHover |= ReferencesSubjectHover(simple);
                        CollectNested(simple, patterns);
                        continue;   // 剥离 hover 相关伪类（含 :not(:hover)）
                    }
                    stripped ??= new List<Selector>();
                    stripped.Add(simple);
                }
                // 只有主体自身的悬停会影响匹配时才需要外层模式：.card:has(.btn:hover) 只关心 .btn。
                if (subjectHover)
                    patterns.Add(stripped?.ToArray() ?? Array.Empty<Selector>());
                break;
            }
            case GroupSelector group:
                foreach (var branch in group.Selectors)
                    Collect(branch, patterns);
                break;
            case DescendantSelector d:
                Collect(d.Ancestor, patterns);
                Collect(d.Descendant, patterns);
                break;
            case ChildSelector c:
                Collect(c.Parent, patterns);
                Collect(c.Child, patterns);
                break;
            case AdjacentSiblingSelector a:
                Collect(a.Previous, patterns);
                Collect(a.Target, patterns);
                break;
            case GeneralSiblingSelector g:
                Collect(g.Previous, patterns);
                Collect(g.Target, patterns);
                break;
            case HoverSelector:
                // 裸 :hover 分支（如 ":hover"、"A :hover B" 的某一段）：任意元素相关。
                patterns.Add(Array.Empty<Selector>());
                break;
            case PseudoClassSelector functional when ContainsHover(functional):
                // 单独成段的函数式伪类（:not(:hover)、:is(.a:hover) …）：主体悬停相关时剥离后为空模式，再深入参数。
                if (ReferencesSubjectHover(functional))
                    patterns.Add(Array.Empty<Selector>());
                CollectNested(functional, patterns);
                break;
        }
    }

    /// <summary>
    /// 简单选择器是否读取<b>主体元素自身</b>的 Hover 状态：<c>:hover</c>，或主体段含 :hover 的
    /// <c>:not()</c>/<c>:is()</c>/<c>:where()</c>。<c>:has()</c> 的参数从不作用于主体；<c>of S</c> 作用于主体时
    /// 由 <see cref="CollectNested"/> 对 S 的完整收集覆盖（主体须匹配 S 才可能匹配）。
    /// </summary>
    private static bool ReferencesSubjectHover(Selector simple) => simple switch
    {
        HoverSelector => true,
        NotSelector not => SubjectSegmentHover(not.Inner),
        IsSelector @is => SubjectSegmentHover(@is.Inner),
        WhereSelector where => SubjectSegmentHover(where.Inner),
        _ => false,
    };

    // 以外层元素为主体的参数：其最右段（作用于主体的那一段）是否读取主体的 Hover。
    private static bool SubjectSegmentHover(Selector argument) => argument switch
    {
        GroupSelector group => group.Selectors.Any(SubjectSegmentHover),
        DescendantSelector d => SubjectSegmentHover(d.Descendant),
        ChildSelector c => SubjectSegmentHover(c.Child),
        AdjacentSiblingSelector a => SubjectSegmentHover(a.Target),
        GeneralSiblingSelector g => SubjectSegmentHover(g.Target),
        CompoundSelector compound => compound.Selectors.Any(ReferencesSubjectHover),
        _ => ReferencesSubjectHover(argument),
    };

    /// <summary>
    /// 为函数式伪类参数里位于<b>其他元素</b>上的 :hover 收集模式。主体自身的 :hover 已由外层剥离后的模式覆盖。
    /// </summary>
    private static void CollectNested(Selector simple, List<Selector[]> patterns)
    {
        switch (simple)
        {
            // 参数以本元素为主体：只有组合器左侧（祖先/兄弟）的 :hover 落在别的元素上。
            // 不能对参数整体 Collect：:not(:hover) 会因此产出通用模式，让每个元素的悬停都触发重算。
            case NotSelector not:
                CollectArgument(not.Inner, patterns);
                break;
            case IsSelector @is:
                CollectArgument(@is.Inner, patterns);
                break;
            case WhereSelector where:
                CollectArgument(where.Inner, patterns);
                break;
            // 相对选择器的每一步都作用于锚点之外的元素（后代或后续兄弟）。
            case HasSelector has:
                foreach (var argument in has.Arguments)
                    foreach (var step in argument.Steps)
                        Collect(step.Compound, patterns);
                break;
            // of S 用来给兄弟计数：S 作用于每个兄弟（也作用于主体）。
            case NthSelector { Of: { } of }:
                Collect(of, patterns);
                break;
        }
    }

    /// <summary>以外层元素为主体的参数：左侧各段完整收集，主体段只深入其中的函数式伪类。</summary>
    private static void CollectArgument(Selector argument, List<Selector[]> patterns)
    {
        switch (argument)
        {
            case GroupSelector group:
                foreach (var branch in group.Selectors)
                    CollectArgument(branch, patterns);
                break;
            case DescendantSelector d:
                Collect(d.Ancestor, patterns);
                CollectArgument(d.Descendant, patterns);
                break;
            case ChildSelector c:
                Collect(c.Parent, patterns);
                CollectArgument(c.Child, patterns);
                break;
            case AdjacentSiblingSelector a:
                Collect(a.Previous, patterns);
                CollectArgument(a.Target, patterns);
                break;
            case GeneralSiblingSelector g:
                Collect(g.Previous, patterns);
                CollectArgument(g.Target, patterns);
                break;
            case CompoundSelector compound:
                foreach (var part in compound.Selectors)
                    CollectNested(part, patterns);
                break;
            default:
                CollectNested(argument, patterns);
                break;
        }
    }

    /// <summary>选择器树的任意位置是否引用了 :hover（含 :not/:is/:where/:has/:nth-child(of) 的参数内）。</summary>
    public static bool ContainsHover(Selector selector) => selector switch
    {
        HoverSelector => true,
        NotSelector not => ContainsHover(not.Inner),
        IsSelector @is => ContainsHover(@is.Inner),
        WhereSelector where => ContainsHover(where.Inner),
        HasSelector has => has.Arguments.Any(argument => argument.Steps.Any(step => ContainsHover(step.Compound))),
        NthSelector { Of: { } of } => ContainsHover(of),
        CompoundSelector compound => compound.Selectors.Any(ContainsHover),
        GroupSelector group => group.Selectors.Any(ContainsHover),
        DescendantSelector d => ContainsHover(d.Ancestor) || ContainsHover(d.Descendant),
        ChildSelector c => ContainsHover(c.Parent) || ContainsHover(c.Child),
        AdjacentSiblingSelector a => ContainsHover(a.Previous) || ContainsHover(a.Target),
        GeneralSiblingSelector g => ContainsHover(g.Previous) || ContainsHover(g.Target),
        _ => false,
    };
}
