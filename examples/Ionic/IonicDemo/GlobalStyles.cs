using Miko.Common;
using Miko.Styling;

namespace IonicDemo;

internal class GlobalStyles
{
    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject()
        {
            [".component-icon"] = new()
            {
                BorderRadius = Percent(50),
                Padding = Px(7),
                Height = Px(18),
                Width = Px(18),
                MarginTop = Px(5),
                MarginBottom = Px(5),
            },

            [".component-icon-primary"] = new()
            {
                BackgroundColor = (Color)"#0054e9",
                Color = (Color)"#fff",
            },

            [".component-detail"] = new()
            {
                PaddingBottom = Px(0),
                MarginBottom = Px(26),

                [".component-description"] = new()
                {
                    Color = (Color)"#3e4a58",
                    FontSize = Rem(1.125f),
                    LineHeight = Number(1.4f),
                    WhiteSpace = WhiteSpace.Normal,
                    PaddingBottom = Px(16),
                }
            },

            // ButtonPage — the demo's `section` wrapper (button.css).
            [".button-section"] = new()
            {
                MarginBottom = Px(16),
                PaddingLeft = Px(10),
                PaddingRight = Px(10),
            },

            // GridPage — the boxed cell content (grid.css).
            [".ion-col > div"] = new()
            {
                BackgroundColor = (Color)"#f7f7f7",
                BorderWidth = Px(1),
                BorderStyle = BorderStyle.Solid,
                BorderColor = (Color)"#ddd",
                Padding = Px(10),
            },

            // CardPage — the demo's card imagery and music controls (card.css).
            [".header-img"] = new()
            {
                Width = Percent(100),
                Height = Px(120),
            },

            [".coworker-card"] = new()
            {
                [".header-img"] = new()
                {
                    Height = Px(160),
                },
            },

            [".music-card"] = new()
            {
                TextAlign = TextAlign.Center,

                ["img"] = new()
                {
                    Width = Px(248),
                    Height = Px(248),
                    BorderRadius = Px(6),
                },
            },

            [".col-align-end"] = new()
            {
                TextAlign = TextAlign.Right,
            },

            // ProgressPage — spacing between the stacked bars (progress.css).
            [".progress-margin"] = new()
            {
                MarginBottom = Px(40),
            },

            // RefresherPage — the unread indicator dot (refresher.css).
            [".dot"] = new()
            {
                Display = Display.Block,
                Height = Px(10),
                Width = Px(10),
                BorderRadius = Percent(50),
                MarginTop = Px(16),
                MarginBottom = Px(16),
                MarginLeft = Px(9),
                MarginRight = Px(8),
            },

            [".dot-unread"] = new()
            {
                BackgroundColor = (Color)"#3684ff",
            },

            // InputOtpPage — the row of single-character boxes.
            [".otp-row"] = new()
            {
                Display = Display.Flex,
                JustifyContent = JustifyContent.Center,
                Gap = Px(8),
            },

            [".otp-box"] = new()
            {
                Width = Px(56),
                TextAlign = TextAlign.Center,
            },
        });
        return styleSheet;
    }
}
