using Miko.Common;
using Miko.Styling;

namespace MikoApp1.Razor;

internal class GlobalStyles
{
    public static StyleSheet Create()
    {
        var styleSheet = new StyleSheet();

        styleSheet.Add(new CssObject
        {
            [".layout"] = new()
            {
                Display = Display.Flex,
                Width = Length.Percent(100),
                Height = Length.Percent(100)
            },
            [".sidebar"] = new()
            {
                Width = Length.Px(250),
                FlexShrink = 0,
                Height = Length.Percent(100),
                Padding = new Padding(16),
                BackgroundColor = Color.FromRgb(52, 58, 64),
                Color = Color.White,
                OverflowY = Overflow.Scroll,
            },
            [".title"] = new()
            {
                Color = Color.White
            },
            [".main-content"] = new()
            {
                FlexGrow = 1,
                Padding = new Padding(16),
                OverflowY = Overflow.Scroll
            },
            [".nav-item"] = new()
            {
                Padding = new Padding(10),
                MarginBottom = Length.Px(4),
                Color = Color.White,
                Cursor = Cursor.Pointer,
            },
            ["p"] = new()
            {
                LineHeight = Px(32),
            },
            [".playground"] = new()
            {
                [".playground-container"] = new()
                {
                    Border = new Border(1, BorderStyle.Solid, "#dee3ea"),
                    BorderRadius = new BorderRadius(Rem(0.4f)),
                    Display = Display.Block,
                },
                [".playground-preview"] = new()
                {
                    Position = Position.Relative,
                    Display = Display.Flex,
                    AlignItems = AlignItems.Center,
                    JustifyContent = JustifyContent.Center,
                    Margin = new Margin(Px(18), Px(0)),
                    Padding = new Padding(Px(16), Px(0)),
                },
            },
            [".container"] = new()
            {
                Display = Display.Flex,
                AlignItems = AlignItems.Center,
                JustifyContent = JustifyContent.Center,
            },
            // A positioned demo stage for the absolutely-positioned FAB. position:relative makes the
            // fab anchor to this box (via its horizontal/vertical classes) instead of the viewport.
            [".fab-demo-container"] = new()
            {
                Position = Position.Relative,
                Display = Display.Block,
                Height = Px(260),
            },
            ["img"] = new CssObject()
            {
                MaxWidth = Percent(100),
            },
            [".sc-ion-label-md-h"] = new ()
            {
                ["h1,h2,h3,h4,h5,h6"] = new()
                {
                    TextOverflow = Css.Inherit,
                },
                ["h1"] = new()
                {
                    MarginLeft = Px(0),
                    MarginRight = Px(0),
                    MarginTop = Px(2),
                    MarginBottom = Px(2),
                    FontSize = Rem(1.5f),
                    FontWeight = FontWeight.Normal,
                },
                ["p"] = new()
                {
                    MarginLeft = Px(0),
                    MarginRight = Px(0),
                    MarginTop = Px(0),
                    MarginBottom = Px(2),
                    FontSize = Rem(0.875f),
                    LineHeight = Rem(1.25f),
                },
                ["&>p"] = new()
                {
                    Color = (Color)"#666666",
                }
            }
        });

        return styleSheet;
    }
}
