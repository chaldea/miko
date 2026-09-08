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
            [".demo-dark .component-content .background-content"] = new() { BackgroundColor = (Color)"#121212" },
            [".demo-dark .component-content .ion-picker"] = new() { BackgroundColor = (Color)"#202124" },
            [".demo-dark .component-detail .component-description"] = new() { Color = (Color)"#adb0b5" },
            [".demo-dark .component-icons .ion-icon"] = new() { Color = (Color)"#f1f3f4" },
            [".component-content"] = new()
            {
                [".background-content"] = new()
                {
                    BackgroundColor = (Color)"#f2f2f7",
                },
                [".ion-picker"] = new()
                {
                    BackgroundColor = (Color)"#fff",
                }
            },
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
            [".component-icons .ion-icon"] = new()
            {
                FontSize = Rem(2.25f),
                Margin = Px(3),
                Color = (Color)"#444",
            },

            [".sc-ion-label-md-s"] = new()
            {
                ["h1,h2,h3,h4,h5,h6"] = new()
                {
                    TextOverflow = Css.Inherit,
                    // Overflow = Css.Inherit,
                },
                ["h1"] = new()
                {
                    MarginLeft = Px(0),
                    MarginRight = Px(0),
                    MarginTop = Px(0),
                    MarginBottom = Px(2),
                    FontSize = Rem(1.5f),
                    FontWeight = FontWeight.Normal
                },
                ["h2"] = new()
                {
                    MarginLeft = Px(0),
                    MarginRight = Px(0),
                    MarginTop = Px(2),
                    MarginBottom = Px(2),
                    FontSize = Rem(1f),
                    FontWeight = FontWeight.Normal
                },
                ["h3,h4,h5,h6"] = new()
                {
                    MarginLeft = Px(0),
                    MarginRight = Px(0),
                    MarginTop = Px(2),
                    MarginBottom = Px(2),
                    FontSize = Rem(0.875f),
                    FontWeight = FontWeight.Normal
                },
                ["p"] = new()
                {
                    MarginLeft = Px(0),
                    MarginRight = Px(0),
                    MarginTop = Px(0),
                    MarginBottom = Px(2),
                    FontSize = Rem(0.875f),
                    LineHeight = Rem(1.25f),
                    TextOverflow = Css.Inherit,
                    Color = (Color)"#666666",
                }
            },

            // ButtonPage — the demo's `section` wrapper (button.css).
            [".button-section"] = new()
            {
                MarginBottom = Px(16),
                PaddingLeft = Px(10),
                PaddingRight = Px(10),
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
                [".ion-card-header"] = new()
                {
                    AlignItems = AlignItems.Center,
                    JustifyContent = JustifyContent.Center,
                },

                ["img"] = new()
                {
                    Width = Px(248),
                    Height = Px(248),
                    BoxShadow = new List<BoxShadow>
                    {
                        new BoxShadow(0, 2, 8, Rgba(2, 8, 20, 0.1f)),
                        new BoxShadow(0, 8, 16, Rgba(2, 8, 20, 0.08f)),
                    },
                    BorderRadius = Px(6),
                },
            },
            [".button-largest"] = new()
            {
                FontSize = Rem(1.75f)
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

            [".modal-custom"] = new()
            {
                AlignItems = AlignItems.FlexEnd,
                [".modal-wrapper"] = new()
                {
                    Height = Length.Auto,
                },
            },
            [".component-segment .ion-header .ion-segment"] = new()
            {
                Left = Px(-30)
            },
            [".component-segment .ion-content .ion-segment"] = new()
            {
                Margin = new Margin(10, Length.Auto)
            },
            [".component-grid .ion-col>div"] = new()
            {
                BackgroundColor = (Color)"#f7f7f7",
                Border = new Border(1, BorderStyle.Solid, "#ddd"),
                Padding = new Padding(10),
            }
        });
        return styleSheet;
    }
}
