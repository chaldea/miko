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
                bool hasHover = false;
                for (int i = 0; i < compound.Selectors.Count; i++)
                {
                    var simple = compound.Selectors[i];
                    if (ContainsHover(simple))
                    {
                        hasHover = true;
                        continue;   // 剥离 hover 相关伪类（含 :not(:hover)）
                    }
                    stripped ??= new List<Selector>();
                    stripped.Add(simple);
                }
                if (hasHover)
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
            case NotSelector not when ContainsHover(not.Inner):
                patterns.Add(Array.Empty<Selector>());
                break;
        }
    }

    /// <summary>选择器树的任意位置是否引用了 :hover（含 :not 内）。</summary>
    public static bool ContainsHover(Selector selector) => selector switch
    {
        HoverSelector => true,
        NotSelector not => ContainsHover(not.Inner),
        CompoundSelector compound => compound.Selectors.Any(ContainsHover),
        GroupSelector group => group.Selectors.Any(ContainsHover),
        DescendantSelector d => ContainsHover(d.Ancestor) || ContainsHover(d.Descendant),
        ChildSelector c => ContainsHover(c.Parent) || ContainsHover(c.Child),
        AdjacentSiblingSelector a => ContainsHover(a.Previous) || ContainsHover(a.Target),
        GeneralSiblingSelector g => ContainsHover(g.Previous) || ContainsHover(g.Target),
        _ => false,
    };
}
