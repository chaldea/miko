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

        // 6. 应用标签默认样式（UA stylesheet）。
        //    必须在父元素继承之前：否则父元素的可继承属性（如 FontWeight=Normal）会先填入，
        //    导致 UA 规则（如 th { font-weight: bold }）失效。
        ApplyDefaultStyles(element, baseStyle);

        // 7. 从父元素继承可继承属性（仅填补 UA 也未提供的属性）
        float? parentFontSizePx = null;
        Dictionary<string, VarValue>? parentVarScope = null;
        var parentComputed = element.Parent?.LayoutBox?.ComputedStyle;
        if (parentComputed != null)
        {
            InheritFromParent(baseStyle, parentComputed);
            // 父元素的计算字体大小（始终为 px），作为本元素 font-size 中 em 的解析基准。
            parentFontSizePx = parentComputed.FontSize.Value;
            // 父元素的变量作用域（自定义属性沿 DOM 树继承）。
            parentVarScope = parentComputed.Vars;
        }

        // 8. 构建本元素的变量作用域：继承父作用域，本元素定义的变量覆盖同名项。
        var varScope = BuildVarScope(parentVarScope, baseStyle.Vars);

        // 9. 转换为计算样式（传入父字体大小以正确解析 font-size 中的 em；传入变量作用域解析 Var 引用；
        //    传入父计算样式以解析 inherit/unset 等全局关键词）
        return ComputedStyle.FromStyle(baseStyle, parentFontSizePx, varScope, parentComputed);
    }

    /// <summary>
    /// 构建元素生效的自定义变量作用域：先继承父作用域，再叠加本元素定义（同名覆盖）。
    /// 无任何变量时返回 null，避免为每个元素分配空字典。
    /// </summary>
    private static Dictionary<string, VarValue>? BuildVarScope(
        Dictionary<string, VarValue>? parentScope, Dictionary<string, VarValue>? ownVars)
    {
        if (ownVars == null || ownVars.Count == 0)
            return parentScope;

        var scope = parentScope != null
            ? new Dictionary<string, VarValue>(parentScope)
            : new Dictionary<string, VarValue>();
        foreach (var kv in ownVars)
            scope[kv.Key] = kv.Value;   // 本元素定义覆盖继承
        return scope;
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
        style.PointerEvents ??= parentStyle.PointerEvents;
        // WhiteSpace 在 CSS 中可继承：文本节点（TextNode）依赖它决定是否换行（如 nowrap），
        // 因此必须从父元素继承，否则匿名文本会退回默认 Normal 而错误换行（见 ISSUE-086）。
        style.WhiteSpace ??= parentStyle.WhiteSpace;

        // 文本相关的可继承属性（须与 StylePropertyGenerator.InheritableProperties 保持一致）。
        // text-transform / letter-spacing / overflow-wrap / word-break 在 CSS 中均可继承。
        style.TextTransform ??= parentStyle.TextTransform;
        style.LetterSpacing ??= parentStyle.LetterSpacing;
        style.OverflowWrap ??= parentStyle.OverflowWrap;
        style.WordBreak ??= parentStyle.WordBreak;
        // visibility 可继承（子元素可用 visibility:visible 覆盖被隐藏的父元素）。
        style.Visibility ??= parentStyle.Visibility;
        // user-select 规范上非继承，但实际会向下传播；按继承处理以贴合作者预期。
        style.UserSelect ??= parentStyle.UserSelect;
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
        // Note: line-height defaults to "normal" via ComputedStyle. We do NOT set it here
        // because that would block inheritance — line-height is inheritable in CSS, and
        // the parent's value should fill in before falling back to ComputedStyle's default.

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
                // Browser default: display: inline-block, padding: 2px 6px, text-align: center
                // Note: Border appearance simplified (browsers use 'outset' style)
                style.Display ??= Common.Display.InlineBlock;
                style.BoxSizing ??= Common.BoxSizing.BorderBox;
                style.PaddingTop ??= Common.Length.Px(2);
                style.PaddingRight ??= Common.Length.Px(6);
                style.PaddingBottom ??= Common.Length.Px(2);
                style.PaddingLeft ??= Common.Length.Px(6);
                style.BorderWidth ??= Common.Length.Px(2);
                style.BorderStyle ??= Common.BorderStyle.Solid;
                style.BorderColor ??= Common.Color.Gray;
                // Browsers center button text by default (UA stylesheet text-align: center).
                style.TextAlign ??= Common.TextAlign.Center;
                break;

            case "input":
                // Input elements have different default styles based on type
                ApplyInputDefaultStyles(element, style);
                break;

            case "textarea":
                // Browser default: display: inline-block, 1px solid border, white background,
                // 允许换行（white-space: pre-wrap）与滚动。高度由 rows 行文本在布局阶段撑起
                // （见 BlockLayout.GetTextFormControlContentHeight），此处不写死像素高度。
                style.Display ??= Common.Display.InlineBlock;
                style.BoxSizing ??= Common.BoxSizing.BorderBox;
                style.PaddingTop ??= Common.Length.Px(2);
                style.PaddingRight ??= Common.Length.Px(2);
                style.PaddingBottom ??= Common.Length.Px(2);
                style.PaddingLeft ??= Common.Length.Px(2);
                style.BorderWidth ??= Common.Length.Px(1);
                style.BorderStyle ??= Common.BorderStyle.Solid;
                style.BorderColor ??= Common.Color.Gray;
                style.BackgroundColor ??= Common.Color.White;
                style.WhiteSpace ??= Common.WhiteSpace.PreWrap;
                break;

            case "br":
                // Browser default: display: inline。br 是强制换行标记，不产生可见盒，
                // 由 BlockLayout 识别并结束当前行盒（见 IsForcedLineBreak）。
                style.Display ??= Common.Display.Inline;
                break;

            case "hr":
                // Browser default: display: block, margin: 0.5em auto, border + inset 线条。
                // Miko 无 border 的 3D inset 样式，用一条 1px 实线上边框绘制分隔线，
                // 内容高度为 0，因此直接复用既有的边框绘制路径。
                style.Display ??= Common.Display.Block;
                style.MarginTop ??= Common.Length.Px(8);      // ~0.5em at 16px
                style.MarginBottom ??= Common.Length.Px(8);
                style.MarginLeft ??= Common.Length.Auto;
                style.MarginRight ??= Common.Length.Auto;
                style.BorderTopWidth ??= Common.Length.Px(1);
                style.BorderTopStyle ??= Common.BorderStyle.Solid;
                style.BorderTopColor ??= Common.Color.Gray;
                break;

            case "img":
                // Browser default: display: inline, border: 0
                style.Display ??= Common.Display.Inline;
                style.BorderWidth ??= Common.Length.Px(0);
                style.BorderStyle ??= Common.BorderStyle.None;
                break;

            case "video":
                // Browser default: display: inline, black background (letterbox bars + 首帧前底色)。
                // 内禀尺寸不写入样式，由布局在缺省时回退到 300×150（见 BlockLayout replaced 处理）。
                style.Display ??= Common.Display.Inline;
                style.BorderWidth ??= Common.Length.Px(0);
                style.BorderStyle ??= Common.BorderStyle.None;
                style.BackgroundColor ??= Common.Color.Black;
                break;

            case "select":
                // Browser default: display: inline-block, border: 1px solid
                // Padding for text and dropdown arrow
                style.Display ??= Common.Display.InlineBlock;
                style.BoxSizing ??= Common.BoxSizing.BorderBox;
                style.PaddingTop ??= Common.Length.Px(1);
                style.PaddingRight ??= Common.Length.Px(20);  // Extra space for dropdown arrow
                style.PaddingBottom ??= Common.Length.Px(1);
                style.PaddingLeft ??= Common.Length.Px(4);
                style.BorderWidth ??= Common.Length.Px(1);
                style.BorderStyle ??= Common.BorderStyle.Solid;
                style.BorderColor ??= Common.Color.Gray;
                style.BackgroundColor ??= Common.Color.White;
                style.MinWidth ??= Common.Length.Px(100);
                // 不写死高度：select 高度应由行高/字体度量加 padding、border 撑开（height: auto）。
                // 固定像素会导致内容高度被钳制（border-box 下尤甚），与字体综合计算的实际高度不符
                // （与 input 同类问题，参见 ISSUE-040）。
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

            case "strong":
            case "b":
                // Browser default: display: inline, font-weight: bold
                style.Display ??= Common.Display.Inline;
                style.FontWeight ??= Common.FontWeight.Bold;
                break;

            case "ul":
                style.Display ??= Common.Display.Block;
                style.MarginTop ??= Common.Length.Px(16);
                style.MarginBottom ??= Common.Length.Px(16);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                style.PaddingLeft ??= Common.Length.Px(40);
                style.PaddingTop ??= Common.Length.Px(0);
                style.PaddingRight ??= Common.Length.Px(0);
                style.PaddingBottom ??= Common.Length.Px(0);
                break;

            case "ol":
                style.Display ??= Common.Display.Block;
                style.MarginTop ??= Common.Length.Px(16);
                style.MarginBottom ??= Common.Length.Px(16);
                style.MarginLeft ??= Common.Length.Px(0);
                style.MarginRight ??= Common.Length.Px(0);
                style.PaddingLeft ??= Common.Length.Px(40);
                style.PaddingTop ??= Common.Length.Px(0);
                style.PaddingRight ??= Common.Length.Px(0);
                style.PaddingBottom ??= Common.Length.Px(0);
                break;

            case "li":
                style.Display ??= Common.Display.Block;
                break;

            case "table":
                style.Display ??= Common.Display.Table;
                style.BoxSizing ??= Common.BoxSizing.BorderBox;
                style.BorderWidth ??= Common.Length.Px(0);
                style.BorderStyle ??= Common.BorderStyle.None;
                break;

            case "caption":
                style.Display ??= Common.Display.Block;
                style.TextAlign ??= Common.TextAlign.Center;
                style.PaddingTop ??= Common.Length.Px(2);
                style.PaddingBottom ??= Common.Length.Px(2);
                break;

            case "thead":
            case "tbody":
            case "tfoot":
                style.Display ??= Common.Display.Block;
                break;

            case "colgroup":
            case "col":
                style.Display ??= Common.Display.None;
                break;

            case "tr":
                style.Display ??= Common.Display.TableRow;
                break;

            case "th":
                style.Display ??= Common.Display.TableCell;
                style.FontWeight ??= Common.FontWeight.Bold;
                style.TextAlign ??= Common.TextAlign.Center;
                style.PaddingTop ??= Common.Length.Px(1);
                style.PaddingRight ??= Common.Length.Px(1);
                style.PaddingBottom ??= Common.Length.Px(1);
                style.PaddingLeft ??= Common.Length.Px(1);
                break;

            case "td":
                style.Display ??= Common.Display.TableCell;
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
    private void ApplyInputDefaultStyles(Element element, Style style)
    {
        style.Display ??= Display.InlineBlock;
        style.BoxSizing ??= BoxSizing.BorderBox;

        if (element is InputElement inputElement)
        {
            switch (inputElement.Type)
            {
                case InputType.Checkbox:
                case InputType.Radio:
                    style.Width ??= Length.Px(13);
                    style.Height ??= Length.Px(13);
                    style.PaddingTop ??= Length.Px(0);
                    style.PaddingRight ??= Length.Px(0);
                    style.PaddingBottom ??= Length.Px(0);
                    style.PaddingLeft ??= Length.Px(0);
                    style.BorderWidth ??= Length.Px(0);
                    style.BorderStyle ??= Common.BorderStyle.None;
                    break;

                case InputType.Range:
                    style.Width ??= Length.Px(129);
                    style.Height ??= Length.Px(21);
                    style.PaddingTop ??= Length.Px(0);
                    style.PaddingRight ??= Length.Px(0);
                    style.PaddingBottom ??= Length.Px(0);
                    style.PaddingLeft ??= Length.Px(0);
                    style.BorderWidth ??= Length.Px(0);
                    style.BorderStyle ??= Common.BorderStyle.None;
                    break;

                case InputType.Text:
                case InputType.Password:
                default:
                    // 文本类输入框高度不设固定默认值：浏览器中其高度由
                    // 行高（line-height）/ 字体度量加上 padding、border 撑开（height: auto）。
                    // 固定为像素会导致内容高度被钳制（如 BoxSizing.BorderBox 下内容仅剩 7px），
                    // 与字体/行高综合计算得到的实际高度不符（参见 ISSUE-040）。
                    style.Width ??= Length.Px(173);
                    style.PaddingTop ??= Length.Px(1);
                    style.PaddingRight ??= Length.Px(2);
                    style.PaddingBottom ??= Length.Px(1);
                    style.PaddingLeft ??= Length.Px(2);
                    style.BorderWidth ??= Length.Px(1);
                    style.BorderStyle ??= Common.BorderStyle.Solid;
                    style.BorderColor ??= Color.Gray;
                    break;
            }
        }
        else
        {
            // 未知 input 元素：同样以内容（行高/字体）决定高度，不设固定默认高度。
            style.Width ??= Length.Px(173);
            style.PaddingTop ??= Length.Px(1);
            style.PaddingRight ??= Length.Px(2);
            style.PaddingBottom ??= Length.Px(1);
            style.PaddingLeft ??= Length.Px(2);
            style.BorderWidth ??= Length.Px(1);
            style.BorderStyle ??= Common.BorderStyle.Solid;
            style.BorderColor ??= Color.Gray;
        }
    }
}
