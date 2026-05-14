using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;

namespace Miko.Styling;

/// <summary>
/// 样式解析器（计算元素的最终样式）
/// </summary>
public class StyleResolver
{
    /// <summary>
    /// 解析元素的最终样式
    /// </summary>
    public ComputedStyle Resolve(Element element, List<StyleSheet> styleSheets, ViewportInfo? viewport = null)
    {
        // 1. 收集所有匹配的规则（带有定义顺序索引）
        var matchedRules = new List<(StyleRule rule, int specificity, int index)>();
        int ruleIndex = 0;

        foreach (var sheet in styleSheets)
        {
            foreach (var rule in sheet.Rules)
            {
                if (rule.Selector.Matches(element))
                {
                    matchedRules.Add((rule, rule.Selector.Specificity, ruleIndex));
                }
                ruleIndex++;
            }

            if (viewport != null)
            {
                foreach (var mediaRule in sheet.MediaRules)
                {
                    if (mediaRule.Condition.Matches(viewport))
                    {
                        foreach (var rule in mediaRule.Rules)
                        {
                            if (rule.Selector.Matches(element))
                            {
                                matchedRules.Add((rule, rule.Selector.Specificity, ruleIndex));
                            }
                            ruleIndex++;
                        }
                    }
                }
            }
        }

        // 2. 按特异性排序（特异性高的优先，同特异性时后定义的优先，因为 Merge 只在 null 时赋值）
        // 使用 OrderByDescending 保证稳定排序
        var sortedRules = matchedRules
            .OrderByDescending(r => r.specificity)
            .ThenByDescending(r => r.index)
            .ToList();

        // 3. 创建基础样式（空）
        var baseStyle = new Style();

        // 4. 应用行内样式（最高优先级）
        if (element.Style != null)
        {
            baseStyle.Merge(element.Style);
        }

        // 5. 应用匹配的规则（按特异性从高到低，同特异性按定义顺序从后到前）
        foreach (var (rule, _, _) in sortedRules)
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
                // Input elements have different default styles based on type
                ApplyInputDefaultStyles(element, style);
                break;

            case "img":
                // Browser default: display: inline, border: 0
                style.Display ??= Common.Display.Inline;
                style.BorderWidth ??= Common.Length.Px(0);
                style.BorderStyle ??= Common.BorderStyle.None;
                break;

            case "select":
                // Browser default: display: inline-block, border: 1px solid
                // Padding for text and dropdown arrow
                style.Display ??= Common.Display.InlineBlock;
                style.PaddingTop ??= Common.Length.Px(1);
                style.PaddingRight ??= Common.Length.Px(20);  // Extra space for dropdown arrow
                style.PaddingBottom ??= Common.Length.Px(1);
                style.PaddingLeft ??= Common.Length.Px(4);
                style.BorderWidth ??= Common.Length.Px(1);
                style.BorderStyle ??= Common.BorderStyle.Solid;
                style.BorderColor ??= Common.Color.Gray;
                style.BackgroundColor ??= Common.Color.White;
                style.MinWidth ??= Common.Length.Px(100);
                style.Height ??= Common.Length.Px(22);
                break;

            case "option":
                // Browser default: display: block
                // Options are typically rendered in dropdown list
                style.Display ??= Common.Display.Block;
                style.PaddingTop ??= Common.Length.Px(2);
                style.PaddingRight ??= Common.Length.Px(8);
                style.PaddingBottom ??= Common.Length.Px(2);
                style.PaddingLeft ??= Common.Length.Px(8);
                break;

            case "optgroup":
                // Browser default: display: block, font-weight: bold
                // OptGroup label styling
                style.Display ??= Common.Display.Block;
                style.FontWeight ??= Common.FontWeight.Bold;
                style.PaddingTop ??= Common.Length.Px(2);
                style.PaddingRight ??= Common.Length.Px(4);
                style.PaddingBottom ??= Common.Length.Px(2);
                style.PaddingLeft ??= Common.Length.Px(4);
                break;

            case "label":
                // Browser default: display: inline
                // Labels are inline elements that associate with form controls
                style.Display ??= Common.Display.Inline;
                break;

            case "a":
                // Browser default: display: inline, color: blue, text-decoration: underline
                // Cursor: pointer (not implemented)
                style.Display ??= Common.Display.Inline;
                style.Color ??= new Common.Color(0, 0, 238);  // Browser default link blue
                style.TextDecoration ??= Common.TextDecoration.Underline;
                break;

            case "ul":
                // Browser default: display: block, margin: 1em 0, padding-left: 40px
                // list-style-type: disc (not implemented - visual bullets would need Painter support)
                style.Display ??= Common.Display.Block;
                style.MarginTop ??= Common.Length.Px(16);     // 1em at 16px
                style.MarginBottom ??= Common.Length.Px(16);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                style.PaddingLeft ??= Common.Length.Px(40);
                style.PaddingTop ??= Common.Length.Px(0);
                style.PaddingRight ??= Common.Length.Px(0);
                style.PaddingBottom ??= Common.Length.Px(0);
                break;

            case "ol":
                // Browser default: display: block, margin: 1em 0, padding-left: 40px
                // list-style-type: decimal (not implemented - numbers would need Painter support)
                style.Display ??= Common.Display.Block;
                style.MarginTop ??= Common.Length.Px(16);     // 1em at 16px
                style.MarginBottom ??= Common.Length.Px(16);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                style.PaddingLeft ??= Common.Length.Px(40);
                style.PaddingTop ??= Common.Length.Px(0);
                style.PaddingRight ??= Common.Length.Px(0);
                style.PaddingBottom ??= Common.Length.Px(0);
                break;

            case "li":
                // Browser default: display: list-item (using block as list-item not supported)
                // List markers (bullets/numbers) would need separate Painter implementation
                style.Display ??= Common.Display.Block;
                break;

            case "table":
                // Browser default: display: table (using block for now), border-collapse: separate
                // border-spacing: 2px, border-color: gray
                style.Display ??= Common.Display.Block;
                style.BorderWidth ??= Common.Length.Px(0);
                style.BorderStyle ??= Common.BorderStyle.None;
                // Note: border-collapse and border-spacing not yet implemented
                break;

            case "caption":
                // Browser default: display: table-caption (using block), text-align: center
                style.Display ??= Common.Display.Block;
                style.TextAlign ??= Common.TextAlign.Center;
                style.PaddingTop ??= Common.Length.Px(2);
                style.PaddingBottom ??= Common.Length.Px(2);
                break;

            case "thead":
            case "tbody":
            case "tfoot":
                // Browser default: display: table-row-group (using block for now)
                // vertical-align: middle, border-color: inherit
                style.Display ??= Common.Display.Block;
                break;

            case "colgroup":
            case "col":
                // Browser default: display: table-column-group / table-column
                // These are typically not rendered but affect table layout
                style.Display ??= Common.Display.None;
                break;

            case "tr":
                // Browser default: display: table-row (using block for now)
                // vertical-align: inherit, border-color: inherit
                style.Display ??= Common.Display.Block;
                break;

            case "th":
                // Browser default: display: table-cell (using inline-block for now)
                // font-weight: bold, text-align: center, vertical-align: inherit
                // padding: 1px
                style.Display ??= Common.Display.InlineBlock;
                style.FontWeight ??= Common.FontWeight.Bold;
                style.TextAlign ??= Common.TextAlign.Center;
                style.PaddingTop ??= Common.Length.Px(1);
                style.PaddingRight ??= Common.Length.Px(1);
                style.PaddingBottom ??= Common.Length.Px(1);
                style.PaddingLeft ??= Common.Length.Px(1);
                break;

            case "td":
                // Browser default: display: table-cell (using inline-block for now)
                // vertical-align: inherit, text-align: left
                // padding: 1px
                style.Display ??= Common.Display.InlineBlock;
                style.TextAlign ??= Common.TextAlign.Left;
                style.PaddingTop ??= Common.Length.Px(1);
                style.PaddingRight ??= Common.Length.Px(1);
                style.PaddingBottom ??= Common.Length.Px(1);
                style.PaddingLeft ??= Common.Length.Px(1);
                break;
        }
    }

    /// <summary>
    /// 应用输入框元素的默认样式（根据 InputType 不同设置不同的样式）
    /// </summary>
    /// <remarks>
    /// Based on browser default behavior:
    /// - Text/Password: Similar to input fields with padding, border, and default dimensions
    /// - Checkbox/Radio: Fixed 13x13px size (browser default), no padding/border in layout
    /// - Range: Fixed height with default width
    /// </remarks>
    private void ApplyInputDefaultStyles(Element element, Style style)
    {
        // Common style for all input types
        style.Display ??= Display.InlineBlock;

        // Check if it's an InputElement to get the type
        if (element is InputElement inputElement)
        {
            switch (inputElement.Type)
            {
                case InputType.Checkbox:
                case InputType.Radio:
                    // Browser default: checkbox/radio are 13x13px
                    // No padding or border in layout (visuals are drawn separately)
                    style.Width ??= Length.Px(13);
                    style.Height ??= Length.Px(13);
                    style.PaddingTop ??= Length.Px(0);
                    style.PaddingRight ??= Length.Px(0);
                    style.PaddingBottom ??= Length.Px(0);
                    style.PaddingLeft ??= Length.Px(0);
                    style.BorderWidth ??= Length.Px(0);
                    style.BorderStyle ??= BorderStyle.None;
                    break;

                case InputType.Range:
                    // Browser default: Range has fixed height ~21px and width ~129px (Chrome)
                    style.Width ??= Length.Px(129);
                    style.Height ??= Length.Px(21);
                    style.PaddingTop ??= Length.Px(0);
                    style.PaddingRight ??= Length.Px(0);
                    style.PaddingBottom ??= Length.Px(0);
                    style.PaddingLeft ??= Length.Px(0);
                    style.BorderWidth ??= Length.Px(0);
                    style.BorderStyle ??= BorderStyle.None;
                    break;

                case InputType.Text:
                case InputType.Password:
                default:
                    // Browser default: text input has padding, border, and default dimensions
                    // Default width: ~173px (Chrome), height: ~21px (including padding/border)
                    style.Width ??= Length.Px(173);
                    style.Height ??= Length.Px(21);
                    style.PaddingTop ??= Length.Px(1);
                    style.PaddingRight ??= Length.Px(2);
                    style.PaddingBottom ??= Length.Px(1);
                    style.PaddingLeft ??= Length.Px(2);
                    style.BorderWidth ??= Length.Px(1);
                    style.BorderStyle ??= BorderStyle.Solid;
                    style.BorderColor ??= Color.Gray;
                    break;
            }
        }
        else
        {
            // Fallback for generic input elements (treat as text input)
            style.Width ??= Length.Px(173);
            style.Height ??= Length.Px(21);
            style.PaddingTop ??= Length.Px(1);
            style.PaddingRight ??= Length.Px(2);
            style.PaddingBottom ??= Length.Px(1);
            style.PaddingLeft ??= Length.Px(2);
            style.BorderWidth ??= Length.Px(1);
            style.BorderStyle ??= BorderStyle.Solid;
            style.BorderColor ??= Color.Gray;
        }
    }
}
