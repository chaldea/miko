using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>Styles for <c>ion-chip</c>.</summary>
internal static class ChipStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            [$".ion-chip.{mode}"] = new()
            {
                Display = Display.InlineBlock,
                Position = Position.Relative,
                MinHeight = Length.Px(32),
                PaddingTop = Length.Px(6),
                PaddingRight = Length.Px(12),
                PaddingBottom = Length.Px(6),
                PaddingLeft = Length.Px(12),
                MarginTop = Length.Px(4),
                MarginRight = Length.Px(4),
                MarginBottom = Length.Px(4),
                MarginLeft = Length.Px(4),
                BackgroundColor = t.ChipBackground,
                Color = t.ChipColor,
                FontSize = Length.Px(t.ChipFontSize),
                BorderRadius = new BorderRadius(Length.Px(16)),
                Cursor = Cursor.Pointer,
                VerticalAlign = VerticalAlign.Middle,
                BoxSizing = BoxSizing.BorderBox,
                Overflow = Overflow.Hidden,
            },

            [$".ion-chip.{mode}.chip-disabled"] = new()
            {
                Opacity = 0.4f,
                Cursor = Cursor.Default,
                PointerEvents = PointerEvents.None,
            },

            [$".ion-chip.{mode}.chip-outline"] = new()
            {
                BackgroundColor = Color.Transparent,
                BorderWidth = Length.Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = t.ChipBorderColor,
            },

            [$".ion-chip.{mode} .ion-icon"] = new()
            {
                Width = Length.Px(18),
                Height = Length.Px(18),
                MarginRight = Length.Px(8),
            },

            [$".ion-chip.{mode} .ion-avatar"] = new()
            {
                Width = Length.Px(24),
                Height = Length.Px(24),
                MarginRight = Length.Px(8),
                FlexShrink = 0,
            },
        };

        AddColor(css, mode, "primary", t.Primary);
        AddColor(css, mode, "secondary", t.Secondary);
        AddColor(css, mode, "tertiary", t.Tertiary);
        AddColor(css, mode, "success", t.Success);
        AddColor(css, mode, "warning", t.Warning);
        AddColor(css, mode, "danger", t.Danger);
        AddColor(css, mode, "light", t.Light);
        AddColor(css, mode, "medium", t.Medium);
        AddColor(css, mode, "dark", t.Dark);

        return css;
    }

    private static void AddColor(CssObject css, string mode, string name, Color color)
    {
        css[$".ion-chip.{mode}.ion-color-{name}"] = new()
        {
            BackgroundColor = new Color(color.R, color.G, color.B, 20),
            Color = color,
        };
        css[$".ion-chip.{mode}.chip-outline.ion-color-{name}"] = new()
        {
            BackgroundColor = Color.Transparent,
            BorderColor = new Color(color.R, color.G, color.B, 82),
            Color = color,
        };
    }
}
