using Miko.Styling.Selectors;

namespace Miko.Styling;

/// <summary>
/// 样式表
/// </summary>
public class StyleSheet
{
    public List<StyleRule> Rules { get; set; } = new();
    public List<MediaRule> MediaRules { get; set; } = new();

    public void AddRule(Selector selector, Style style)
    {
        Rules.Add(new StyleRule { Selector = selector, Style = style });
    }

    public void AddMediaRule(MediaCondition condition, Selector selector, Style style)
    {
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
