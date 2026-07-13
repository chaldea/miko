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

        AddRow(container, "display", cs.Display.ToString().ToLower());
        AddRow(container, "position", cs.Position.ToString().ToLower());
        AddRow(container, "width", FormatLength(cs.Width));
        AddRow(container, "height", FormatLength(cs.Height));
        AddRow(container, "min-width", FormatLength(cs.MinWidth));
        AddRow(container, "min-height", FormatLength(cs.MinHeight));
        AddRow(container, "max-width", FormatLength(cs.MaxWidth));
        AddRow(container, "max-height", FormatLength(cs.MaxHeight));

        AddRow(container, "padding", $"{FormatLength(cs.PaddingTop)} {FormatLength(cs.PaddingRight)} {FormatLength(cs.PaddingBottom)} {FormatLength(cs.PaddingLeft)}");
        AddRow(container, "margin", $"{FormatLength(cs.MarginTop)} {FormatLength(cs.MarginRight)} {FormatLength(cs.MarginBottom)} {FormatLength(cs.MarginLeft)}");

        AddRow(container, "background-color", FormatColor(cs.BackgroundColor));
        AddRow(container, "color", FormatColor(cs.Color));
        AddRow(container, "font-family", cs.FontFamily);
        AddRow(container, "font-size", FormatLength(cs.FontSize));
        AddRow(container, "font-weight", cs.FontWeight.ToString().ToLower());
        AddRow(container, "line-height", FormatLineHeight(cs.LineHeight));
        AddRow(container, "text-align", cs.TextAlign.ToString().ToLower());

        if (cs.Display == Common.Display.Flex)
        {
            AddRow(container, "flex-direction", cs.FlexDirection.ToString().ToLower());
            AddRow(container, "justify-content", cs.JustifyContent.ToString().ToLower());
            AddRow(container, "align-items", cs.AlignItems.ToString().ToLower());
            AddRow(container, "flex-wrap", cs.FlexWrap.ToString().ToLower());
            AddRow(container, "flex-grow", cs.FlexGrow.ToString("0.##"));
            AddRow(container, "flex-shrink", cs.FlexShrink.ToString("0.##"));
        }

        if (cs.Opacity < 1f)
            AddRow(container, "opacity", cs.Opacity.ToString("0.##"));

        if (cs.OverflowX != Common.Overflow.Visible)
            AddRow(container, "overflow-x", cs.OverflowX.ToString().ToLower());
        if (cs.OverflowY != Common.Overflow.Visible)
            AddRow(container, "overflow-y", cs.OverflowY.ToString().ToLower());

        if (cs.BorderTopWidth.Value > 0)
            AddRow(container, "border", $"{FormatLength(cs.BorderTopWidth)} {cs.BorderTopStyle.ToString().ToLower()} {FormatColor(cs.BorderTopColor)}");

        if (cs.ZIndex != 0)
            AddRow(container, "z-index", cs.ZIndex.ToString());

        var boxShadow = cs.BoxShadow.RefValueOrNull();
        if (boxShadow != null && boxShadow.Count > 0)
            AddRow(container, "box-shadow", FormatBoxShadow(boxShadow));

        return container;
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
