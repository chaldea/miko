using Miko.Common;
using Miko.Styling;
using static Miko.Styling.Css;

namespace Miko.Ionic.Components;

/// <summary>Styles ported from input-otp.scss and the iOS / Material mode overrides.</summary>
internal static class InputOtpStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var outlineBorder = mode == "ios" ? t.InputBorderColor : Color.FromHex("b3b3b3");
        var borderWidth = mode == "ios" ? 0.55f : 1f;
        var neutral50 = Color.FromHex("f2f2f2");
        var neutral100 = Color.FromHex("e6e6e6");
        var neutral150 = Color.FromHex("d9d9d9");
        var disabledText = Color.FromHex("a6a6a6");

        var css = new CssObject
        {
            [$".ion-input-otp.{mode}"] = new()
            {
                Vars = new()
                {
                    ["--background"] = Color.Transparent,
                    ["--border-width"] = Length.Px(borderWidth),
                    ["--border-color"] = outlineBorder,
                    ["--color"] = t.ItemColor,
                    ["--min-width"] = Length.Px(40),
                    ["--separator-width"] = Length.Px(8),
                    ["--separator-height"] = Length.Px(8),
                    ["--separator-color"] = neutral150,
                    ["--highlight-color"] = t.Primary,
                },
                Display = Display.Block,
                Position = Position.Relative,
                FontSize = Length.Px(14),
            },
            [$".ion-input-otp.{mode} .input-otp-group"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                PaddingTop = Length.Px(16),
                PaddingRight = Length.Px(0),
                PaddingBottom = Length.Px(16),
                PaddingLeft = Length.Px(0),
            },
            [$".ion-input-otp.{mode} .native-wrapper"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
                MinWidth = Var("--min-width"),
            },
            [$".ion-input-otp.{mode} .native-input"] = new()
            {
                MinWidth = Var("--min-width"),
                BorderTopWidth = Var("--border-width"),
                BorderRightWidth = Var("--border-width"),
                BorderBottomWidth = Var("--border-width"),
                BorderLeftWidth = Var("--border-width"),
                BorderTopStyle = BorderStyle.Solid,
                BorderRightStyle = BorderStyle.Solid,
                BorderBottomStyle = BorderStyle.Solid,
                BorderLeftStyle = BorderStyle.Solid,
                BorderTopColor = Var("--border-color"),
                BorderRightColor = Var("--border-color"),
                BorderBottomColor = Var("--border-color"),
                BorderLeftColor = Var("--border-color"),
                BackgroundColor = Var("--background"),
                Color = Var("--color"),
                CaretColor = Var("--highlight-color"),
                FontSize = Length.Px(14),
                TextAlign = TextAlign.Center,
                Cursor = Cursor.Text,
            },
            [$".ion-input-otp.{mode} .native-input:focus"] = new()
            {
                Vars = new()
                {
                    ["--border-width"] = Length.Px(mode == "ios" ? 1 : 2),
                    ["--border-color"] = t.Primary,
                },
            },
            [$".ion-input-otp.{mode} .input-otp-description"] = new()
            {
                Color = t.InputHelperColor,
                FontSize = Length.Px(12),
                LineHeight = Length.Px(20),
                TextAlign = TextAlign.Center,
            },
            [$".ion-input-otp.{mode} .input-otp-description-hidden"] = new()
            {
                Display = Display.None,
            },
            [$".ion-input-otp.{mode} .input-otp-separator"] = new()
            {
                FlexShrink = 0,
                Width = Var("--separator-width"),
                Height = Var("--separator-height"),
                BorderRadius = new BorderRadius(Length.Px(999)),
                BackgroundColor = Var("--separator-color"),
            },

            [$".ion-input-otp.{mode}.input-otp-size-small .native-input"] = new()
            {
                Width = Length.Px(40),
                Height = Length.Px(40),
            },
            [$".ion-input-otp.{mode}.input-otp-size-small .input-otp-group"] = new()
            {
                Gap = Length.Px(8),
            },
            [$".ion-input-otp.{mode}.input-otp-size-medium .native-input"] = new()
            {
                Width = Length.Px(48),
                Height = Length.Px(48),
            },
            [$".ion-input-otp.{mode}.input-otp-size-large .native-input"] = new()
            {
                Width = Length.Px(56),
                Height = Length.Px(56),
            },
            [$".ion-input-otp.{mode}.input-otp-size-medium .input-otp-group"] = new()
            {
                Gap = Length.Px(12),
            },
            [$".ion-input-otp.{mode}.input-otp-size-large .input-otp-group"] = new()
            {
                Gap = Length.Px(12),
            },

            [$".ion-input-otp.{mode}.input-otp-shape-round .native-input"] = new()
            {
                BorderRadius = new BorderRadius(Length.Px(16)),
            },
            [$".ion-input-otp.{mode}.input-otp-shape-soft .native-input"] = new()
            {
                BorderRadius = new BorderRadius(Length.Px(8)),
            },
            [$".ion-input-otp.{mode}.input-otp-shape-rectangular .native-input"] = new()
            {
                BorderRadius = new BorderRadius(Length.Px(0)),
            },

            [$".ion-input-otp.{mode}.input-otp-fill-outline"] = new()
            {
                Vars = new()
                {
                    ["--background"] = Color.Transparent,
                    ["--border-color"] = outlineBorder,
                },
            },
            [$".ion-input-otp.{mode}.input-otp-fill-solid"] = new()
            {
                Vars = new()
                {
                    ["--background"] = neutral50,
                    ["--border-color"] = neutral50,
                },
            },
            [$".ion-input-otp.{mode}.input-otp-disabled"] = new()
            {
                Vars = new() { ["--color"] = disabledText },
                Cursor = Cursor.NotAllowed,
                PointerEvents = PointerEvents.None,
            },
            [$".ion-input-otp.{mode}.input-otp-fill-outline.input-otp-disabled"] = new()
            {
                Vars = new()
                {
                    ["--background"] = neutral50,
                    ["--border-color"] = neutral100,
                },
            },
            [$".ion-input-otp.{mode}.input-otp-fill-outline.input-otp-readonly"] = new()
            {
                Vars = new() { ["--background"] = neutral50 },
            },
            [$".ion-input-otp.{mode}.input-otp-fill-solid.input-otp-disabled"] = new()
            {
                Vars = new()
                {
                    ["--background"] = neutral100,
                    ["--border-color"] = neutral100,
                },
            },
            [$".ion-input-otp.{mode}.input-otp-fill-solid.input-otp-readonly"] = new()
            {
                Vars = new()
                {
                    ["--background"] = neutral100,
                    ["--border-color"] = neutral100,
                },
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
        css[$".ion-input-otp.{mode}.ion-color-{name}"] = new()
        {
            Vars = new() { ["--highlight-color"] = color },
        };
        css[$".ion-input-otp.{mode}.ion-color-{name}.input-otp-fill-outline .native-input"] = new()
        {
            BorderTopColor = new Color(color.R, color.G, color.B, 153),
            BorderRightColor = new Color(color.R, color.G, color.B, 153),
            BorderBottomColor = new Color(color.R, color.G, color.B, 153),
            BorderLeftColor = new Color(color.R, color.G, color.B, 153),
        };
        css[$".ion-input-otp.{mode}.ion-color-{name} .native-input:focus"] = new()
        {
            BorderTopColor = color,
            BorderRightColor = color,
            BorderBottomColor = color,
            BorderLeftColor = color,
        };
    }
}
