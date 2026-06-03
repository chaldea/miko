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
    public ComputedStyle Resolve(Element element, List<StyleSheet> styleSheets, ViewportInfo? viewport = null,
        CustomPropertyScope? parentScope = null)
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

        // 5.5 构建自定义属性作用域
        var scope = BuildCustomPropertyScope(baseStyle, parentScope);

        // 5.6 解析变量引用
        ResolveVarBindings(baseStyle, scope);

        // 6. 从父元素继承可继承属性
        if (element.Parent != null && element.Parent.LayoutBox?.ComputedStyle != null)
        {
            InheritFromParent(baseStyle, element.Parent.LayoutBox.ComputedStyle);
        }

        // 7. 应用标签默认样式（最低优先级）
        ApplyDefaultStyles(element, baseStyle);

        // 8. 转换为计算样式
        var computed = ComputedStyle.FromStyle(baseStyle);
        computed.CustomPropertyScope = scope;
        return computed;
    }

    /// <summary>
    /// 从父元素继承可继承属性
    /// </summary>
    private void InheritFromParent(Style style, ComputedStyle parentStyle)
    {
        // 可继承的属性
        style.Color ??= (StyleProperty<Color>)parentStyle.Color;
        style.FontFamily ??= parentStyle.FontFamily;
        style.FontSize ??= (StyleProperty<Length>)parentStyle.FontSize;
        style.FontWeight ??= (StyleProperty<FontWeight>)parentStyle.FontWeight;
        style.TextAlign ??= (StyleProperty<TextAlign>)parentStyle.TextAlign;
        style.LineHeight ??= (StyleProperty<Length>)parentStyle.LineHeight;
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
                style.BoxSizing ??= Common.BoxSizing.BorderBox;
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
                style.Display ??= Common.Display.Block;
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
                style.Display ??= Common.Display.Block;
                break;

            case "th":
                style.Display ??= Common.Display.InlineBlock;
                style.FontWeight ??= Common.FontWeight.Bold;
                style.TextAlign ??= Common.TextAlign.Center;
                style.PaddingTop ??= Common.Length.Px(1);
                style.PaddingRight ??= Common.Length.Px(1);
                style.PaddingBottom ??= Common.Length.Px(1);
                style.PaddingLeft ??= Common.Length.Px(1);
                break;

            case "td":
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
                    style.Width ??= Length.Px(173);
                    style.Height ??= Length.Px(21);
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
            style.Width ??= Length.Px(173);
            style.Height ??= Length.Px(21);
            style.PaddingTop ??= Length.Px(1);
            style.PaddingRight ??= Length.Px(2);
            style.PaddingBottom ??= Length.Px(1);
            style.PaddingLeft ??= Length.Px(2);
            style.BorderWidth ??= Length.Px(1);
            style.BorderStyle ??= Common.BorderStyle.Solid;
            style.BorderColor ??= Color.Gray;
        }
    }

    private static CustomPropertyScope BuildCustomPropertyScope(Style mergedStyle, CustomPropertyScope? parentScope)
    {
        if (mergedStyle.CustomProperties == null || mergedStyle.CustomProperties.Count == 0)
            return parentScope ?? new CustomPropertyScope();

        var scope = parentScope?.CreateChild() ?? new CustomPropertyScope();
        foreach (var (name, value) in mergedStyle.CustomProperties)
            scope.Set(name, value);
        return scope;
    }

    private static StyleProperty<T>? ResolveVar<T>(StyleProperty<T>? prop, CustomPropertyScope scope) where T : struct
    {
        if (prop is { IsVar: true } sp)
        {
            var resolved = scope.Get<T>(sp.Var.Name);
            if (resolved.HasValue)
                return new StyleProperty<T>(resolved.Value);
            if (sp.Var.Fallback is T fallback)
                return new StyleProperty<T>(fallback);
            return null;
        }

        if (prop is { IsCalc: true } cp)
        {
            return ResolveCalc<T>(cp.Calc, scope);
        }

        return prop;
    }

    /// <summary>
    /// 求值 calc 表达式。仅支持 Length / float / int 数值类型，其它类型（枚举/Color）返回 null（忽略）。
    /// </summary>
    private static StyleProperty<T>? ResolveCalc<T>(Func<CustomPropertyScope, CalcExpr> calc, CustomPropertyScope scope)
        where T : struct
    {
        var expr = calc(scope);

        if (typeof(T) == typeof(Length))
        {
            Length result = expr.ToLength(scope);
            return new StyleProperty<T>((T)(object)result);
        }
        if (typeof(T) == typeof(float))
        {
            float result = expr.ToFloat(scope);
            return new StyleProperty<T>((T)(object)result);
        }
        if (typeof(T) == typeof(int))
        {
            int result = expr.ToInt(scope);
            return new StyleProperty<T>((T)(object)result);
        }

        return null;
    }

    private static void ResolveVarBindings(Style style, CustomPropertyScope scope)
    {
        style.Display = ResolveVar(style.Display, scope);
        style.FlexDirection = ResolveVar(style.FlexDirection, scope);
        style.JustifyContent = ResolveVar(style.JustifyContent, scope);
        style.AlignItems = ResolveVar(style.AlignItems, scope);

        style.FlexGrow = ResolveVar(style.FlexGrow, scope);
        style.FlexShrink = ResolveVar(style.FlexShrink, scope);
        style.FlexBasis = ResolveVar(style.FlexBasis, scope);

        style.BoxSizing = ResolveVar(style.BoxSizing, scope);
        style.Width = ResolveVar(style.Width, scope);
        style.Height = ResolveVar(style.Height, scope);
        style.MinWidth = ResolveVar(style.MinWidth, scope);
        style.MinHeight = ResolveVar(style.MinHeight, scope);
        style.MaxWidth = ResolveVar(style.MaxWidth, scope);
        style.MaxHeight = ResolveVar(style.MaxHeight, scope);

        style.PaddingTop = ResolveVar(style.PaddingTop, scope);
        style.PaddingRight = ResolveVar(style.PaddingRight, scope);
        style.PaddingBottom = ResolveVar(style.PaddingBottom, scope);
        style.PaddingLeft = ResolveVar(style.PaddingLeft, scope);

        style.MarginTop = ResolveVar(style.MarginTop, scope);
        style.MarginRight = ResolveVar(style.MarginRight, scope);
        style.MarginBottom = ResolveVar(style.MarginBottom, scope);
        style.MarginLeft = ResolveVar(style.MarginLeft, scope);

        style.BorderWidth = ResolveVar(style.BorderWidth, scope);
        style.BorderColor = ResolveVar(style.BorderColor, scope);
        style.BorderStyle = ResolveVar(style.BorderStyle, scope);

        style.BorderTopWidth = ResolveVar(style.BorderTopWidth, scope);
        style.BorderRightWidth = ResolveVar(style.BorderRightWidth, scope);
        style.BorderBottomWidth = ResolveVar(style.BorderBottomWidth, scope);
        style.BorderLeftWidth = ResolveVar(style.BorderLeftWidth, scope);

        style.BorderTopColor = ResolveVar(style.BorderTopColor, scope);
        style.BorderRightColor = ResolveVar(style.BorderRightColor, scope);
        style.BorderBottomColor = ResolveVar(style.BorderBottomColor, scope);
        style.BorderLeftColor = ResolveVar(style.BorderLeftColor, scope);

        style.BorderTopStyle = ResolveVar(style.BorderTopStyle, scope);
        style.BorderRightStyle = ResolveVar(style.BorderRightStyle, scope);
        style.BorderBottomStyle = ResolveVar(style.BorderBottomStyle, scope);
        style.BorderLeftStyle = ResolveVar(style.BorderLeftStyle, scope);

        style.BorderTopLeftRadius = ResolveVar(style.BorderTopLeftRadius, scope);
        style.BorderTopRightRadius = ResolveVar(style.BorderTopRightRadius, scope);
        style.BorderBottomRightRadius = ResolveVar(style.BorderBottomRightRadius, scope);
        style.BorderBottomLeftRadius = ResolveVar(style.BorderBottomLeftRadius, scope);

        style.BackgroundColor = ResolveVar(style.BackgroundColor, scope);
        style.BackgroundRepeat = ResolveVar(style.BackgroundRepeat, scope);
        style.BackgroundSize = ResolveVar(style.BackgroundSize, scope);
        style.BackgroundPosition = ResolveVar(style.BackgroundPosition, scope);
        style.Color = ResolveVar(style.Color, scope);
        style.FontSize = ResolveVar(style.FontSize, scope);
        style.FontWeight = ResolveVar(style.FontWeight, scope);
        style.TextAlign = ResolveVar(style.TextAlign, scope);
        style.LineHeight = ResolveVar(style.LineHeight, scope);

        style.Position = ResolveVar(style.Position, scope);
        style.Top = ResolveVar(style.Top, scope);
        style.Right = ResolveVar(style.Right, scope);
        style.Bottom = ResolveVar(style.Bottom, scope);
        style.Left = ResolveVar(style.Left, scope);

        style.TextDecoration = ResolveVar(style.TextDecoration, scope);
        style.TextTransform = ResolveVar(style.TextTransform, scope);
        style.FontStyle = ResolveVar(style.FontStyle, scope);
        style.WhiteSpace = ResolveVar(style.WhiteSpace, scope);
        style.LetterSpacing = ResolveVar(style.LetterSpacing, scope);
        style.VerticalAlign = ResolveVar(style.VerticalAlign, scope);

        style.Opacity = ResolveVar(style.Opacity, scope);
        style.ZIndex = ResolveVar(style.ZIndex, scope);
        style.Visibility = ResolveVar(style.Visibility, scope);
        style.Cursor = ResolveVar(style.Cursor, scope);
        style.UserSelect = ResolveVar(style.UserSelect, scope);

        style.FlexWrap = ResolveVar(style.FlexWrap, scope);
        style.AlignSelf = ResolveVar(style.AlignSelf, scope);
        style.AlignContent = ResolveVar(style.AlignContent, scope);
        style.Gap = ResolveVar(style.Gap, scope);
        style.RowGap = ResolveVar(style.RowGap, scope);
        style.ColumnGap = ResolveVar(style.ColumnGap, scope);

        style.OverflowX = ResolveVar(style.OverflowX, scope);
        style.OverflowY = ResolveVar(style.OverflowY, scope);

        style.TransformOrigin = ResolveVar(style.TransformOrigin, scope);
    }
}
