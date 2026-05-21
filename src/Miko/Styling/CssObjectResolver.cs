using Miko.Styling.Selectors;

namespace Miko.Styling;

/// <summary>
/// 将 CssObject 树展开为扁平的 StyleRule 列表
/// </summary>
internal static class CssObjectResolver
{
    internal static void Resolve(CssObject css, StyleSheet sheet)
    {
        foreach (var (selectorStr, child) in css.Children)
        {
            Flatten(selectorStr, child, "", sheet);
        }
    }

    private static void Flatten(string selectorStr, CssObject css, string parentSelector, StyleSheet sheet)
    {
        var fullSelector = BuildFullSelector(selectorStr, parentSelector);

        // 将当前节点的样式属性提取为 StyleRule
        var style = ExtractStyle(css);
        if (HasAnyProperty(style))
        {
            var selector = CssSelectorParser.Parse(fullSelector);

            // 检查是否包含伪元素选择器
            var pseudoInfo = ExtractPseudoElement(selector, fullSelector);
            if (pseudoInfo != null)
            {
                sheet.PseudoElementRules.Add(new PseudoElementRule
                {
                    Selector = pseudoInfo.Value.parentSelector,
                    Type = pseudoInfo.Value.type,
                    Style = style
                });
            }
            else
            {
                sheet.AddRule(selector, style);
            }
        }

        // 递归处理子选择器
        foreach (var (childSelectorStr, child) in css.Children)
        {
            Flatten(childSelectorStr, child, fullSelector, sheet);
        }
    }

    private static (Selector parentSelector, PseudoElementType type)? ExtractPseudoElement(Selector selector, string fullSelector)
    {
        // Case 1: selector is directly a PseudoElementSelector (e.g., "::after" alone — unlikely but handle)
        if (selector is BeforePseudoElement)
            return (new UniversalSelector(), PseudoElementType.Before);
        if (selector is AfterPseudoElement)
            return (new UniversalSelector(), PseudoElementType.After);

        // Case 2: CompoundSelector containing a PseudoElementSelector (e.g., ".btn::after")
        if (selector is CompoundSelector compound)
        {
            var pseudoPart = compound.Selectors.OfType<PseudoElementSelector>().FirstOrDefault();
            if (pseudoPart != null)
            {
                var type = pseudoPart is BeforePseudoElement ? PseudoElementType.Before : PseudoElementType.After;
                var remaining = compound.Selectors.Where(s => s is not PseudoElementSelector).ToList();
                var parentSelector = remaining.Count == 1 ? remaining[0] : new CompoundSelector(remaining);
                return (parentSelector, type);
            }
        }

        // Case 3: Combinator selector where the rightmost part contains a PseudoElementSelector
        // e.g., ".parent .child::after" → DescendantSelector(ClassSelector("parent"), CompoundSelector([ClassSelector("child"), AfterPseudoElement]))
        // We need to rebuild the selector without the pseudo-element part
        if (selector is DescendantSelector or ChildSelector or AdjacentSiblingSelector or GeneralSiblingSelector)
        {
            var (left, right, combinatorType) = DecomposeCombinator(selector);
            var rightPseudo = ExtractPseudoElement(right, "");
            if (rightPseudo != null)
            {
                // The right side had a pseudo-element; rebuild parent selector without it
                var parentSelector = RecomposeCombinator(left, rightPseudo.Value.parentSelector, combinatorType);
                return (parentSelector, rightPseudo.Value.type);
            }
        }

        return null;
    }

    private static (Selector left, Selector right, char combinator) DecomposeCombinator(Selector selector)
    {
        return selector switch
        {
            DescendantSelector d => (d.Ancestor, d.Descendant, ' '),
            ChildSelector c => (c.Parent, c.Child, '>'),
            AdjacentSiblingSelector a => (a.Previous, a.Target, '+'),
            GeneralSiblingSelector g => (g.Previous, g.Target, '~'),
            _ => throw new InvalidOperationException()
        };
    }

    private static Selector RecomposeCombinator(Selector left, Selector right, char combinator)
    {
        // If the right side became a universal selector (pseudo-element was the only part),
        // just use the left side as the parent selector
        if (right is UniversalSelector)
            return left;

        return combinator switch
        {
            ' ' => new DescendantSelector(left, right),
            '>' => new ChildSelector(left, right),
            '+' => new AdjacentSiblingSelector(left, right),
            '~' => new GeneralSiblingSelector(left, right),
            _ => new DescendantSelector(left, right)
        };
    }

    private static string BuildFullSelector(string current, string parent)
    {
        if (string.IsNullOrEmpty(parent))
            return current;

        if (current.Contains('&'))
            return current.Replace("&", parent);

        // 默认后代关系
        return $"{parent} {current}";
    }

    private static Style ExtractStyle(CssObject css)
    {
        // CssObject 继承自 Style，直接克隆其样式属性
        return css.Clone();
    }

    private static bool HasAnyProperty(Style style)
    {
        return style.Display != null || style.FlexDirection != null || style.JustifyContent != null ||
               style.AlignItems != null || style.FlexGrow != null || style.FlexShrink != null ||
               style.FlexBasis != null || style.Width != null || style.Height != null ||
               style.MinWidth != null || style.MinHeight != null || style.MaxWidth != null ||
               style.MaxHeight != null || style.PaddingTop != null || style.PaddingRight != null ||
               style.PaddingBottom != null || style.PaddingLeft != null || style.MarginTop != null ||
               style.MarginRight != null || style.MarginBottom != null || style.MarginLeft != null ||
               style.BorderWidth != null || style.BorderColor != null || style.BorderStyle != null ||
               style.BorderTopWidth != null || style.BorderRightWidth != null ||
               style.BorderBottomWidth != null || style.BorderLeftWidth != null ||
               style.BorderTopColor != null || style.BorderRightColor != null ||
               style.BorderBottomColor != null || style.BorderLeftColor != null ||
               style.BorderTopStyle != null || style.BorderRightStyle != null ||
               style.BorderBottomStyle != null || style.BorderLeftStyle != null ||
               style.BorderTopLeftRadius != null || style.BorderTopRightRadius != null ||
               style.BorderBottomRightRadius != null || style.BorderBottomLeftRadius != null ||
               style.BackgroundColor != null || style.BackgroundImage != null ||
               style.BackgroundRepeat != null || style.BackgroundSize != null ||
               style.BackgroundPosition != null ||
               style.Color != null || style.FontFamily != null || style.FontSize != null ||
               style.FontWeight != null || style.TextAlign != null || style.LineHeight != null ||
               style.Position != null || style.Top != null || style.Right != null ||
               style.Bottom != null || style.Left != null || style.TextDecoration != null ||
               style.TextTransform != null || style.FontStyle != null || style.WhiteSpace != null ||
               style.LetterSpacing != null || style.VerticalAlign != null || style.Opacity != null ||
               style.ZIndex != null || style.Visibility != null || style.Cursor != null ||
               style.UserSelect != null || style.FlexWrap != null || style.AlignSelf != null ||
               style.AlignContent != null || style.Gap != null || style.RowGap != null ||
               style.ColumnGap != null || style.BoxShadow != null || style.OverflowX != null ||
               style.OverflowY != null || style.Transform != null || style.TransformOrigin != null ||
               style.Transitions != null || style.Animations != null || style.Content != null;
    }
}
