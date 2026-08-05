using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;

namespace Miko.DevTools.Panels;

internal static class StyleInspector
{
    public static DivElement Build(Element? selectedElement, MikoEngine? engine)
    {
        var panel = new DivElement { Class = "style-panel" };

        if (selectedElement == null || engine == null)
        {
            panel.AddChild(new DivElement
            {
                Class = "console-empty",
                TextContent = "Select an element to inspect"
            });
            return panel;
        }

        var layoutBox = FindLayoutBox(engine.GetCurrentLayout(), selectedElement);
        if (layoutBox == null)
        {
            panel.AddChild(new DivElement
            {
                Class = "console-empty",
                TextContent = "No layout data available"
            });
            return panel;
        }

        panel.AddChild(BuildBoxModel(layoutBox));
        panel.AddChild(BuildComputedStyles(layoutBox.ComputedStyle));

        return panel;
    }

    private static DivElement BuildBoxModel(LayoutBox box)
    {
        var container = new DivElement { Class = "box-model" };
        var title = new DivElement { Class = "style-section-title", TextContent = "Box Model" };
        container.AddChild(title);

        var margin = box.BoxModel.Margin;
        var padding = box.BoxModel.Padding;
        var content = box.BoxModel.Content;
        var border = box.BoxModel.Border;

        var marginBox = new DivElement { Class = "box-margin" };
        marginBox.AddChild(new DivElement { Class = "box-label", TextContent = "margin" });
        marginBox.AddChild(new DivElement
        {
            Class = "box-value",
            TextContent = $"{margin.Top:0.#}  {margin.Right:0.#}  {margin.Bottom:0.#}  {margin.Left:0.#}"
        });

        var borderBox = new DivElement { Class = "box-border" };
        borderBox.AddChild(new DivElement { Class = "box-label", TextContent = "border" });
        borderBox.AddChild(new DivElement
        {
            Class = "box-value",
            TextContent = $"{border.Top:0.#}  {border.Right:0.#}  {border.Bottom:0.#}  {border.Left:0.#}"
        });

        var paddingBox = new DivElement { Class = "box-padding" };
        paddingBox.AddChild(new DivElement { Class = "box-label", TextContent = "padding" });
        paddingBox.AddChild(new DivElement
        {
            Class = "box-value",
            TextContent = $"{padding.Top:0.#}  {padding.Right:0.#}  {padding.Bottom:0.#}  {padding.Left:0.#}"
        });

        var contentBox = new DivElement { Class = "box-content" };
        contentBox.AddChild(new DivElement
        {
            Class = "box-value",
            TextContent = $"{content.Width:0.#} x {content.Height:0.#}"
        });

        paddingBox.AddChild(contentBox);
        borderBox.AddChild(paddingBox);
        marginBox.AddChild(borderBox);
        container.AddChild(marginBox);

        return container;
    }

    private static DivElement BuildComputedStyles(ComputedStyle cs)
    {
        var container = new DivElement();
        var title = new DivElement { Class = "style-section-title", TextContent = "Computed Styles" };
        container.AddChild(title);

        // 先收集再统一按属性名排序输出（浏览器 DevTools 的 Computed 面板同样按字母序），
        // 便于在长列表里定位属性。收集顺序因此不再影响显示顺序。
        var rows = new List<(string Property, string Value)>(64);
        CollectComputedStyles(cs, rows);

        rows.Sort(static (a, b) => string.CompareOrdinal(a.Property, b.Property));

        foreach (var (property, value) in rows)
            AddRow(container, property, value);

        return container;
    }

    private static void CollectComputedStyles(ComputedStyle cs, List<(string, string)> rows)
    {
        void Add(string property, string value) => rows.Add((property, value));

        Add("display", ToCssKeyword(cs.Display.ToString()));
        Add("position", cs.Position.ToString().ToLower());
        Add("box-sizing", ToCssKeyword(cs.BoxSizing.ToString()));
        Add("width", FormatLength(cs.Width));
        Add("height", FormatLength(cs.Height));
        Add("min-width", FormatLength(cs.MinWidth));
        Add("min-height", FormatLength(cs.MinHeight));
        Add("max-width", FormatLength(cs.MaxWidth));
        Add("max-height", FormatLength(cs.MaxHeight));

        // 定位偏移（inset）：static 定位下不生效，故仅在参与定位时显示，避免一列 auto 噪声。
        if (cs.Position != Common.Position.Static)
        {
            Add("top", FormatLength(cs.Top));
            Add("right", FormatLength(cs.Right));
            Add("bottom", FormatLength(cs.Bottom));
            Add("left", FormatLength(cs.Left));
        }

        // 盒模型四边拆分显示（与浏览器 Computed 面板一致），便于逐边核对。
        Add("padding-top", FormatLength(cs.PaddingTop));
        Add("padding-right", FormatLength(cs.PaddingRight));
        Add("padding-bottom", FormatLength(cs.PaddingBottom));
        Add("padding-left", FormatLength(cs.PaddingLeft));
        Add("margin-top", FormatLength(cs.MarginTop));
        Add("margin-right", FormatLength(cs.MarginRight));
        Add("margin-bottom", FormatLength(cs.MarginBottom));
        Add("margin-left", FormatLength(cs.MarginLeft));

        Add("background-color", FormatColor(cs.BackgroundColor));
        Add("color", FormatColor(cs.Color));
        Add("font-family", cs.FontFamily);
        Add("font-size", FormatLength(cs.FontSize));
        Add("font-weight", cs.FontWeight.ToString().ToLower());
        Add("font-style", cs.FontStyle.ToString().ToLower());
        Add("line-height", FormatLineHeight(cs.LineHeight));
        Add("text-align", cs.TextAlign.ToString().ToLower());
        Add("direction", cs.Direction.ToString().ToLower());
        Add("white-space", ToCssKeyword(cs.WhiteSpace.ToString()));
        Add("vertical-align", ToCssKeyword(cs.VerticalAlign.ToString()));
        Add("cursor", ToCssKeyword(cs.Cursor.ToString()));

        if (cs.TextDecoration != Common.TextDecoration.None)
            Add("text-decoration", ToCssKeyword(cs.TextDecoration.ToString()));
        if (cs.WritingMode != Common.WritingMode.HorizontalTb)
            Add("writing-mode", ToCssKeyword(cs.WritingMode.ToString()));
        if (cs.PointerEvents != Common.PointerEvents.Auto)
            Add("pointer-events", ToCssKeyword(cs.PointerEvents.ToString()));

        // 文本排版补充属性（仅在非默认时显示，避免冗余）。
        if (cs.TextTransform != Common.TextTransform.None)
            Add("text-transform", cs.TextTransform.ToString().ToLower());
        if (cs.LetterSpacing.Value != 0)
            Add("letter-spacing", FormatLength(cs.LetterSpacing));
        if (cs.OverflowWrap != Common.OverflowWrap.Normal)
            Add("overflow-wrap", ToCssKeyword(cs.OverflowWrap.ToString()));
        if (cs.WordBreak != Common.WordBreak.Normal)
            Add("word-break", ToCssKeyword(cs.WordBreak.ToString()));
        if (cs.TextOverflow != Common.TextOverflow.Clip)
            Add("text-overflow", cs.TextOverflow.ToString().ToLower());

        if (cs.Display is Common.Display.Flex or Common.Display.InlineFlex)
        {
            Add("flex-direction", ToCssKeyword(cs.FlexDirection.ToString()));
            Add("flex-wrap", ToCssKeyword(cs.FlexWrap.ToString()));
            Add("flex-basis", FormatLength(cs.FlexBasis));
            Add("justify-content", ToCssKeyword(cs.JustifyContent.ToString()));
            Add("align-items", ToCssKeyword(cs.AlignItems.ToString()));
            Add("align-content", ToCssKeyword(cs.AlignContent.ToString()));
        }

        if (cs.Display == Common.Display.Grid)
        {
            if (cs.GridTemplateColumns != null)
                Add("grid-template-columns", string.Join(' ', cs.GridTemplateColumns));
            if (cs.GridTemplateRows != null)
                Add("grid-template-rows", string.Join(' ', cs.GridTemplateRows));
            Add("justify-content", ToCssKeyword(cs.JustifyContent.ToString()));
            Add("justify-items", ToCssKeyword(cs.JustifyItems.ToString()));
            Add("align-items", ToCssKeyword(cs.AlignItems.ToString()));
            Add("align-content", ToCssKeyword(cs.AlignContent.ToString()));
        }

        // gap 对 flex 与 grid 容器都生效。
        if (cs.Display is Common.Display.Flex or Common.Display.InlineFlex or Common.Display.Grid)
        {
            if (!cs.RowGap.IsAuto || !cs.ColumnGap.IsAuto || cs.Gap.Value != 0)
            {
                Add("row-gap", cs.RowGap.IsAuto ? FormatLength(cs.Gap) : FormatLength(cs.RowGap));
                Add("column-gap", cs.ColumnGap.IsAuto ? FormatLength(cs.Gap) : FormatLength(cs.ColumnGap));
            }
        }

        // flex/grid 子项属性：容器由父级决定，故不按自身 display 过滤。
        if (cs.FlexGrow != 0)
            Add("flex-grow", cs.FlexGrow.ToString("0.##"));
        if (cs.FlexShrink != 1)
            Add("flex-shrink", cs.FlexShrink.ToString("0.##"));
        if (cs.AlignSelf != Common.AlignSelf.Auto)
            Add("align-self", ToCssKeyword(cs.AlignSelf.ToString()));
        if (cs.JustifySelf != Common.JustifySelf.Auto)
            Add("justify-self", ToCssKeyword(cs.JustifySelf.ToString()));
        if (cs.Order != 0)
            Add("order", cs.Order.ToString());

        if (cs.Visibility != Common.Visibility.Visible)
            Add("visibility", cs.Visibility.ToString().ToLower());
        if (cs.UserSelect != Common.UserSelect.Auto)
            Add("user-select", cs.UserSelect.ToString().ToLower());

        if (cs.HasVisibleOutline)
        {
            Add("outline", $"{FormatLength(cs.OutlineWidth)} {cs.OutlineStyle.ToString().ToLower()} {FormatColor(cs.OutlineColor)}");
            if (cs.OutlineOffset.Value != 0)
                Add("outline-offset", FormatLength(cs.OutlineOffset));
        }

        if (cs.Opacity < 1f)
            Add("opacity", cs.Opacity.ToString("0.##"));

        if (cs.OverflowX != Common.Overflow.Visible)
            Add("overflow-x", cs.OverflowX.ToString().ToLower());
        if (cs.OverflowY != Common.Overflow.Visible)
            Add("overflow-y", cs.OverflowY.ToString().ToLower());

        // 边框逐边显示：四边不一致时单边写法才能反映真实计算值。
        AddBorderSide(rows, "top", cs.BorderTopWidth, cs.BorderTopStyle, cs.BorderTopColor);
        AddBorderSide(rows, "right", cs.BorderRightWidth, cs.BorderRightStyle, cs.BorderRightColor);
        AddBorderSide(rows, "bottom", cs.BorderBottomWidth, cs.BorderBottomStyle, cs.BorderBottomColor);
        AddBorderSide(rows, "left", cs.BorderLeftWidth, cs.BorderLeftStyle, cs.BorderLeftColor);

        if (cs.BorderTopLeftRadius.Value != 0) Add("border-top-left-radius", FormatLength(cs.BorderTopLeftRadius));
        if (cs.BorderTopRightRadius.Value != 0) Add("border-top-right-radius", FormatLength(cs.BorderTopRightRadius));
        if (cs.BorderBottomRightRadius.Value != 0) Add("border-bottom-right-radius", FormatLength(cs.BorderBottomRightRadius));
        if (cs.BorderBottomLeftRadius.Value != 0) Add("border-bottom-left-radius", FormatLength(cs.BorderBottomLeftRadius));

        if (cs.ZIndex != 0)
            Add("z-index", cs.ZIndex.ToString());

        var boxShadow = cs.BoxShadow.RefValueOrNull();
        if (boxShadow != null && boxShadow.Count > 0)
            Add("box-shadow", FormatBoxShadow(boxShadow));
    }

    /// <summary>宽度为 0 的边不绘制，故仅在有实际宽度时列出该边。</summary>
    private static void AddBorderSide(List<(string, string)> rows, string side,
        Common.Length width, Common.BorderStyle style, Common.Color color)
    {
        if (width.Value <= 0) return;
        rows.Add(($"border-{side}", $"{FormatLength(width)} {style.ToString().ToLower()} {FormatColor(color)}"));
    }

    private static string FormatBoxShadow(List<Common.BoxShadow> shadows)
    {
        return string.Join(", ", shadows.Select(s =>
        {
            var inset = s.Inset ? "inset " : "";
            return $"{inset}{s.OffsetX:0.#}px {s.OffsetY:0.#}px {s.BlurRadius:0.#}px {s.SpreadRadius:0.#}px {FormatColor(s.Color)}";
        }));
    }

    private static void AddRow(DivElement container, string property, string value)
    {
        var row = new DivElement { Class = "style-row" };
        row.AddChild(new SpanElement { Class = "style-prop", TextContent = property });
        row.AddChild(new SpanElement { Class = "style-value", TextContent = value });
        container.AddChild(row);
    }

    private static string FormatLength(Common.Length length)
    {
        return length.ToString();
    }

    /// <summary>
    /// 格式化 line-height：未设置（值 0）显示为 "normal"，否则使用 Length 默认格式。
    /// </summary>
    private static string FormatLineHeight(Common.Length lineHeight)
    {
        if (lineHeight.IsAuto || lineHeight.Value == 0)
            return "normal";
        return lineHeight.ToString();
    }

    private static string FormatColor(Common.Color color)
    {
        if (color.A == 0) return "transparent";
        if (color.A == 255) return $"rgb({color.R}, {color.G}, {color.B})";
        return $"rgba({color.R}, {color.G}, {color.B}, {color.A / 255f:0.##})";
    }

    /// <summary>
    /// 将 PascalCase 枚举名转为 CSS 关键字（在大写字母前插入连字符并转小写），
    /// 例如 <c>BreakWord</c> → <c>break-word</c>，<c>WrapReverse</c> → <c>wrap-reverse</c>。
    /// </summary>
    private static string ToCssKeyword(string pascal)
    {
        if (string.IsNullOrEmpty(pascal)) return pascal;
        var sb = new System.Text.StringBuilder(pascal.Length + 4);
        for (int i = 0; i < pascal.Length; i++)
        {
            char c = pascal[i];
            if (i > 0 && char.IsUpper(c)) sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    private static LayoutBox? FindLayoutBox(LayoutBox? root, Element element)
    {
        if (root == null) return null;
        if (root.Element == element) return root;
        foreach (var child in root.Children)
        {
            var found = FindLayoutBox(child, element);
            if (found != null) return found;
        }
        return null;
    }
}
