using Miko.Core;
using Miko.Styling.Selectors;

namespace Miko.Styling;

/// <summary>
/// 样式表
/// </summary>
public class StyleSheet
{
    public List<StyleRule> Rules { get; set; } = new();
    public List<PseudoElementRule> PseudoElementRules { get; set; } = new();
    public List<MediaRule> MediaRules { get; set; } = new();

    // :hover 相关性分析缓存（见 HoverRelevance）。样式表在交给引擎后不可变
    // （ISSUE-096 契约），此处的失效仅覆盖注册前的增量构建。
    private List<Selector[]>? _hoverPatterns;

    public void Add(CssObject css)
    {
        _hoverPatterns = null;
        CssObjectResolver.Resolve(css, this);
    }

    public void AddRule(Selector selector, Style style)
    {
        _hoverPatterns = null;
        Rules.Add(new StyleRule { Selector = selector, Style = style });
    }

    /// <summary>样式表是否包含任何 :hover 规则（含媒体查询与伪元素规则内）。</summary>
    public bool UsesHoverPseudo => HoverPatterns.Count > 0;

    /// <summary>
    /// 元素的 <see cref="ElementState.Hover"/> 状态是否可能影响本表任何规则的匹配。
    /// 保守判定（只多不漏）：为 false 时悬停状态变化对本表样式结果无任何影响，
    /// 可跳过重算（ISSUE-104 问题1）。
    /// </summary>
    internal bool IsHoverRelevant(Element element)
    {
        var patterns = HoverPatterns;
        for (int i = 0; i < patterns.Count; i++)
        {
            var pattern = patterns[i];
            bool all = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (!pattern[j].Matches(element)) { all = false; break; }
            }
            if (all) return true;
        }
        return false;
    }

    private List<Selector[]> HoverPatterns
    {
        get
        {
            if (_hoverPatterns == null)
            {
                var patterns = new List<Selector[]>();
                foreach (var rule in Rules)
                    HoverRelevance.Collect(rule.Selector, patterns);
                foreach (var rule in PseudoElementRules)
                    HoverRelevance.Collect(rule.Selector, patterns);
                foreach (var media in MediaRules)
                    foreach (var rule in media.Rules)
                        HoverRelevance.Collect(rule.Selector, patterns);
                _hoverPatterns = patterns;
            }
            return _hoverPatterns;
        }
    }


    [Obsolete("Use StyleSheet.Register(CssObject) instead.")]
    public void AddRule<T>(TypedStyleBuilder<T> builder) where T : Element
    {
        var (selector, style) = builder.Build();
        Rules.Add(new StyleRule { Selector = selector, Style = style });
    }

    [Obsolete("Use StyleSheet.Register(CssObject) instead.")]
    public void AddRule<T>(CombinatorStyleBuilder<T> builder) where T : Element
    {
        var (selector, style) = builder.Build();
        Rules.Add(new StyleRule { Selector = selector, Style = style });
    }

    [Obsolete("Use StyleSheet.Register(CssObject) instead.")]
    public void AddRule<T>(PseudoElementStyleBuilder<T> builder) where T : Element
    {
        var (selector, type, style) = builder.Build();
        PseudoElementRules.Add(new PseudoElementRule { Selector = selector, Type = type, Style = style });
    }

    public void AddMediaRule<T>(MediaCondition condition, TypedStyleBuilder<T> builder) where T : Element
    {
        var (selector, style) = builder.Build();
        AddMediaRule(condition, selector, style);
    }

    public void AddMediaRule(MediaCondition condition, Selector selector, Style style)
    {
        _hoverPatterns = null;
        var existing = MediaRules.FirstOrDefault(m => m.Condition == condition);
        if (existing != null)
        {
            existing.Rules.Add(new StyleRule { Selector = selector, Style = style });
        }
        else
        {
            var mediaRule = new MediaRule
            {
                Condition = condition,
                Rules = new List<StyleRule>
                {
                    new StyleRule { Selector = selector, Style = style }
                }
            };
            MediaRules.Add(mediaRule);
        }
    }
}

/// <summary>
/// 样式规则
/// </summary>
public class StyleRule
{
    public Selector Selector { get; set; } = null!;
    public Style Style { get; set; } = null!;
}
