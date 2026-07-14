using Miko.Common;
using Miko.Styling;

namespace Miko.Bootstrap.Styles;

internal static class RebootStyles
{
    internal static CssObject GenStyle(Theme t)
    {
        return new CssObject
        {
            ["*"] = new()
            {
                BoxSizing = BoxSizing.BorderBox
            },
            ["hr"] = new()
            {
                Margin = new Margin(Length.Rem(1), 0),
                Border = new Border(0),
                BorderTop = new BorderSide(Length.Px(t.BorderWidth), BorderStyle.Solid),
                Opacity = 0.25f
            },
            ["h1"] = new()
            {
                FontSize = Length.Px(40),
                FontWeight = FontWeight.Medium,
                LineHeight = Number(1.2f),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Rem(0.5f),
                Color = t.HeadingColor
            },
            ["h2"] = new()
            {
                FontSize = Length.Px(32),
                FontWeight = FontWeight.Medium,
                LineHeight = Number(1.2f),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Rem(0.5f),
                Color = t.HeadingColor
            },
            ["h3"] = new()
            {
                FontSize = Length.Px(28),
                FontWeight = FontWeight.Medium,
                LineHeight = Number(1.2f),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Rem(0.5f),
                Color = t.HeadingColor
            },
            ["h4"] = new()
            {
                FontSize = Length.Px(24),
                FontWeight = FontWeight.Medium,
                LineHeight = Number(1.2f),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Rem(0.5f),
                Color = t.HeadingColor
            },
            ["h5"] = new()
            {
                FontSize = Length.Px(20),
                FontWeight = FontWeight.Medium,
                LineHeight = Number(1.2f),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Rem(0.5f),
                Color = t.HeadingColor
            },
            ["h6"] = new()
            {
                FontSize = Length.Px(16),
                FontWeight = FontWeight.Medium,
                LineHeight = Number(1.2f),
                MarginTop = Length.Px(0),
                MarginBottom = Length.Rem(0.5f),
                Color = t.HeadingColor
            },
            ["p"] = new()
            {
                MarginTop = Length.Px(0),
                MarginBottom = Length.Rem(1)
            },
            ["b, strong"] = new()
            {
                FontWeight = FontWeight.Bolder
            },
            ["small"] = new()
            {
                FontSize = Length.Rem(0.875f)
            },
            ["ol, ul"] = new()
            {
                PaddingLeft = Length.Rem(2)
            },
            ["ol, ul, dl"] = new()
            {
                MarginTop = Length.Px(0),
                MarginBottom = Length.Rem(1)
            },
            ["dt"] = new()
            {
                FontWeight = FontWeight.Bold
            },
            ["dd"] = new()
            {
                MarginBottom = Length.Rem(0.5f),
                MarginLeft = Length.Px(0)
            },
            ["blockquote"] = new()
            {
                Margin = new Margin(0, 0, Length.Rem(1), 0)
            },
            ["figure"] = new()
            {
                Margin = new Margin(0, 0, Length.Rem(1), 0)
            },
            ["a"] = new()
            {
                Color = t.LinkColor,
                TextDecoration = t.LinkDecoration,
                ["&:hover"] = new() { Color = t.LinkHoverColor }
            },
            ["pre"] = new()
            {
                Display = Display.Block,
                MarginTop = Length.Px(0),
                MarginBottom = Length.Rem(1),
                FontSize = Length.Rem(0.875f),
                OverflowX = Overflow.Auto
            },
            ["code"] = new()
            {
                FontSize = Length.Rem(0.875f),
                Color = t.CodeColor
            },
            ["table"] = new()
            {
                BorderWidth = Length.Px(0),
                BorderStyle = BorderStyle.Solid
            },
            ["th"] = new()
            {
                TextAlign = TextAlign.Left,
            },
            ["thead,tbody,tfoot,tr,td,th"] = new()
            {
                BorderColor = t.BorderColor,
                BorderStyle = BorderStyle.Solid,
                BorderWidth = Length.Px(0),
            },
            ["label"] = new()
            {
                Display = Display.InlineBlock
            },
            ["button"] = new()
            {
                BorderRadius = 0
            },
            ["input, button, select, textarea"] = new()
            {
                Margin = new Margin(0),
                LineHeight = Length.Px(1.5f)
            },
        };
    }
}
