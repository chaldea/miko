using Miko.Animation;
using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic.Components;

/// <summary>Styles for <c>ion-spinner</c>.</summary>
internal static class SpinnerStyles
{
    internal static CssObject GenStyle(string mode, IonicTheme t)
    {
        var css = new CssObject
        {
            [$".ion-spinner.{mode}"] = new()
            {
                Display = Display.InlineBlock,
                Position = Position.Relative,
                Width = Length.Px(t.SpinnerSize),
                Height = Length.Px(t.SpinnerSize),
                Color = t.SpinnerColor,
                VerticalAlign = VerticalAlign.Middle,
                BoxSizing = BoxSizing.BorderBox,
            },

            [$".ion-spinner.{mode}.spinner-paused"] = new()
            {
                Opacity = 0.6f,
            },

            [$".ion-spinner.{mode}.spinner-lines-small"] = new()
            {
                Width = Length.Px(t.SpinnerSmallSize),
                Height = Length.Px(t.SpinnerSmallSize),
            },

            [$".ion-spinner.{mode}.spinner-lines-sharp-small"] = new()
            {
                Width = Length.Px(t.SpinnerSmallSize),
                Height = Length.Px(t.SpinnerSmallSize),
            },

            [$".ion-spinner.{mode} .spinner-line"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Px(2),
                Left = Length.Percent(50),
                Width = Length.Px(2),
                Height = Length.Px(9),
                MarginLeft = Length.Px(-1),
                BackgroundColor = t.SpinnerColor,
                BorderRadius = new BorderRadius(Length.Px(1)),
                TransformOrigin = new TransformOrigin(Length.Percent(50), Length.Px(12)),
            },

            [$".ion-spinner.{mode}.spinner-lines-small .spinner-line"] = new()
            {
                Height = Length.Px(6),
                TransformOrigin = new TransformOrigin(Length.Percent(50), Length.Px(8)),
            },

            [$".ion-spinner.{mode}.spinner-lines-sharp .spinner-line"] = new()
            {
                Width = Length.Px(2),
                Height = Length.Px(8),
                BorderRadius = BorderRadius.None,
                TransformOrigin = new TransformOrigin(Length.Percent(50), Length.Px(12)),
            },

            [$".ion-spinner.{mode}.spinner-lines-sharp-small .spinner-line"] = new()
            {
                Width = Length.Px(2),
                Height = Length.Px(6),
                BorderRadius = BorderRadius.None,
                TransformOrigin = new TransformOrigin(Length.Percent(50), Length.Px(8)),
            },

            [$".ion-spinner.{mode} .spinner-circle"] = new()
            {
                Position = Position.Absolute,
                Top = Length.Percent(50),
                Left = Length.Percent(50),
                Width = Length.Px(6),
                Height = Length.Px(6),
                MarginTop = Length.Px(-3),
                MarginLeft = Length.Px(-3),
                BackgroundColor = t.SpinnerColor,
                BorderRadius = BorderRadius.Circle,
                TransformOrigin = new TransformOrigin(Length.Percent(50), Length.Px(11)),
            },

            [$".ion-spinner.{mode}.spinner-circular .spinner-circle"] = new()
            {
                Top = Length.Px(2),
                Left = Length.Px(2),
                Width = Length.Px(t.SpinnerSize - 4),
                Height = Length.Px(t.SpinnerSize - 4),
                MarginTop = Length.Px(0),
                MarginLeft = Length.Px(0),
                BackgroundColor = Color.Transparent,
                BorderWidth = Length.Px(3),
                BorderStyle = BorderStyle.Solid,
                BorderColor = t.SpinnerTrackColor,
                BorderTopColor = t.SpinnerColor,
                BorderRadius = BorderRadius.Circle,
            },

            [$".ion-spinner.{mode}.spinner-crescent .spinner-circle"] = new()
            {
                Top = Length.Px(2),
                Left = Length.Px(2),
                Width = Length.Px(t.SpinnerSize - 4),
                Height = Length.Px(t.SpinnerSize - 4),
                MarginTop = Length.Px(0),
                MarginLeft = Length.Px(0),
                BackgroundColor = Color.Transparent,
                BorderWidth = Length.Px(3),
                BorderStyle = BorderStyle.Solid,
                BorderColor = Color.Transparent,
                BorderTopColor = t.SpinnerColor,
                BorderRightColor = t.SpinnerColor,
                BorderRadius = BorderRadius.Circle,
            },

            [$".ion-spinner.{mode}.spinner-dots .spinner-circle"] = new()
            {
                Position = Position.Relative,
                Top = Length.Px(0),
                Left = Length.Px(0),
                Display = Display.InlineBlock,
                Width = Length.Px(6),
                Height = Length.Px(6),
                MarginTop = Length.Px(11),
                MarginRight = Length.Px(2),
                MarginBottom = Length.Px(0),
                MarginLeft = Length.Px(2),
                Transform = Transform.None,
            },

            [$".ion-spinner.{mode}.spinner-bubbles .spinner-circle"] = new()
            {
                Width = Length.Px(5),
                Height = Length.Px(5),
                MarginTop = Length.Px(-2.5f),
                MarginLeft = Length.Px(-2.5f),
                TransformOrigin = new TransformOrigin(Length.Percent(50), Length.Px(11)),
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
        css[$".ion-spinner.{mode}.ion-color-{name}"] = new()
        {
            Color = color,
        };

        css[$".ion-spinner.{mode}.ion-color-{name} .spinner-line"] = new()
        {
            BackgroundColor = color,
        };

        css[$".ion-spinner.{mode}.ion-color-{name} .spinner-circle"] = new()
        {
            BackgroundColor = color,
        };

        css[$".ion-spinner.{mode}.ion-color-{name}.spinner-circular .spinner-circle"] = new()
        {
            BackgroundColor = Color.Transparent,
            BorderColor = new Color(color.R, color.G, color.B, 51),
            BorderTopColor = color,
        };

        css[$".ion-spinner.{mode}.ion-color-{name}.spinner-crescent .spinner-circle"] = new()
        {
            BackgroundColor = Color.Transparent,
            BorderTopColor = color,
            BorderRightColor = color,
        };
    }
}
