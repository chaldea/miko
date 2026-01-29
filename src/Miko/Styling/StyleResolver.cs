using Miko.Core;

namespace Miko.Styling;

/// <summary>
/// 样式解析器（计算元素的最终样式）
/// </summary>
public class StyleResolver
{
    /// <summary>
    /// 解析元素的最终样式
    /// </summary>
    public ComputedStyle Resolve(Element element, List<StyleSheet> styleSheets)
    {
        // 1. 收集所有匹配的规则
        var matchedRules = new List<(StyleRule rule, int specificity)>();

        foreach (var sheet in styleSheets)
        {
            foreach (var rule in sheet.Rules)
            {
                if (rule.Selector.Matches(element))
                {
                    matchedRules.Add((rule, rule.Selector.Specificity));
                }
            }
        }

        // 2. 按特异性排序（特异性高的优先，因为 Merge 只在 null 时赋值）
        matchedRules.Sort((a, b) => b.specificity.CompareTo(a.specificity));

        // 3. 创建基础样式（空）
        var baseStyle = new Style();

        // 4. 应用行内样式（最高优先级）
        if (element.Style != null)
        {
            baseStyle.Merge(element.Style);
        }

        // 5. 应用匹配的规则（按特异性从高到低）
        foreach (var (rule, _) in matchedRules)
        {
            baseStyle.Merge(rule.Style);
        }

        // 6. 从父元素继承可继承属性
        if (element.Parent != null && element.Parent.LayoutBox?.ComputedStyle != null)
        {
            InheritFromParent(baseStyle, element.Parent.LayoutBox.ComputedStyle);
        }

        // 7. 应用标签默认样式（最低优先级）
        ApplyDefaultStyles(element, baseStyle);

        // 8. 转换为计算样式
        return ComputedStyle.FromStyle(baseStyle);
    }

    /// <summary>
    /// 从父元素继承可继承属性
    /// </summary>
    private void InheritFromParent(Style style, ComputedStyle parentStyle)
    {
        // 可继承的属性
        style.Color ??= parentStyle.Color;
        style.FontFamily ??= parentStyle.FontFamily;
        style.FontSize ??= parentStyle.FontSize;
        style.FontWeight ??= parentStyle.FontWeight;
        style.TextAlign ??= parentStyle.TextAlign;
        style.LineHeight ??= parentStyle.LineHeight;
    }

    /// <summary>
    /// 应用元素的默认样式（模拟浏览器 User Agent Stylesheet）
    /// </summary>
    /// <remarks>
    /// Based on W3C recommended default stylesheet and browser behavior.
    /// See: https://html.spec.whatwg.org/multipage/rendering.html
    /// </remarks>
    private void ApplyDefaultStyles(Element element, Style style)
    {
        // Note: All elements get line-height: normal (0 = auto calculation)
        style.LineHeight ??= Common.Length.Px(0);

        // 根据标签名应用默认样式
        switch (element.TagName.ToLower())
        {
            case "body":
                // Browser default: margin: 8px
                style.Display ??= Common.Display.Block;
                style.MarginTop ??= Common.Length.Px(8);
                style.MarginRight ??= Common.Length.Px(8);
                style.MarginBottom ??= Common.Length.Px(8);
                style.MarginLeft ??= Common.Length.Px(8);
                break;

            case "h1":
                // Browser default: font-size: 2em, margin: 0.67em 0, font-weight: bold
                style.Display ??= Common.Display.Block;
                style.FontSize ??= Common.Length.Px(32);  // 2em at 16px base
                style.FontWeight ??= Common.FontWeight.Bold;
                // Note: Should be 0.67em but using pixels until em units supported
                style.MarginTop ??= Common.Length.Px(21);    // ~0.67em at 32px
                style.MarginBottom ??= Common.Length.Px(21);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                break;

            case "h2":
                // Browser default: font-size: 1.5em, margin: 0.83em 0, font-weight: bold
                style.Display ??= Common.Display.Block;
                style.FontSize ??= Common.Length.Px(24);  // 1.5em at 16px base
                style.FontWeight ??= Common.FontWeight.Bold;
                style.MarginTop ??= Common.Length.Px(20);    // ~0.83em at 24px
                style.MarginBottom ??= Common.Length.Px(20);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                break;

            case "h3":
                // Browser default: font-size: 1.17em, margin: 1em 0, font-weight: bold
                style.Display ??= Common.Display.Block;
                style.FontSize ??= Common.Length.Px(19);  // ~1.17em at 16px base
                style.FontWeight ??= Common.FontWeight.Bold;
                style.MarginTop ??= Common.Length.Px(19);
                style.MarginBottom ??= Common.Length.Px(19);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                break;

            case "h4":
                // Browser default: font-size: 1em, margin: 1.33em 0, font-weight: bold
                style.Display ??= Common.Display.Block;
                style.FontSize ??= Common.Length.Px(16);
                style.FontWeight ??= Common.FontWeight.Bold;
                style.MarginTop ??= Common.Length.Px(21);    // ~1.33em at 16px
                style.MarginBottom ??= Common.Length.Px(21);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                break;

            case "h5":
                // Browser default: font-size: 0.83em, margin: 1.67em 0, font-weight: bold
                style.Display ??= Common.Display.Block;
                style.FontSize ??= Common.Length.Px(13);  // ~0.83em at 16px base
                style.FontWeight ??= Common.FontWeight.Bold;
                style.MarginTop ??= Common.Length.Px(22);    // ~1.67em at 13px
                style.MarginBottom ??= Common.Length.Px(22);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                break;

            case "h6":
                // Browser default: font-size: 0.67em, margin: 2.33em 0, font-weight: bold
                style.Display ??= Common.Display.Block;
                style.FontSize ??= Common.Length.Px(11);  // ~0.67em at 16px base
                style.FontWeight ??= Common.FontWeight.Bold;
                style.MarginTop ??= Common.Length.Px(26);    // ~2.33em at 11px
                style.MarginBottom ??= Common.Length.Px(26);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                break;

            case "p":
                // Browser default: margin: 1em 0 (16px at default font-size)
                style.Display ??= Common.Display.Block;
                style.MarginTop ??= Common.Length.Px(16);    // 1em at 16px
                style.MarginBottom ??= Common.Length.Px(16);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                break;

            case "div":
                // Browser default: display: block
                style.Display ??= Common.Display.Block;
                break;

            case "span":
                // Browser default: display: inline
                style.Display ??= Common.Display.Inline;
                break;

            case "button":
                // Browser default: display: inline-block, padding: 2px 6px
                // Note: Border appearance simplified (browsers use 'outset' style)
                style.Display ??= Common.Display.InlineBlock;
                style.PaddingTop ??= Common.Length.Px(2);
                style.PaddingRight ??= Common.Length.Px(6);
                style.PaddingBottom ??= Common.Length.Px(2);
                style.PaddingLeft ??= Common.Length.Px(6);
                style.BorderWidth ??= Common.Length.Px(2);
                style.BorderStyle ??= Common.BorderStyle.Solid;
                style.BorderColor ??= Common.Color.Gray;
                break;

            case "input":
                // Browser default: display: inline-block, padding: 1px 2px
                // Note: Border appearance simplified (browsers use 'inset' style)
                style.Display ??= Common.Display.InlineBlock;
                style.PaddingTop ??= Common.Length.Px(1);
                style.PaddingRight ??= Common.Length.Px(2);
                style.PaddingBottom ??= Common.Length.Px(1);
                style.PaddingLeft ??= Common.Length.Px(2);
                style.BorderWidth ??= Common.Length.Px(2);
                style.BorderStyle ??= Common.BorderStyle.Solid;
                style.BorderColor ??= Common.Color.Gray;
                break;

            case "img":
                // Browser default: display: inline, border: 0
                style.Display ??= Common.Display.Inline;
                style.BorderWidth ??= Common.Length.Px(0);
                style.BorderStyle ??= Common.BorderStyle.None;
                break;
        }
    }
}
