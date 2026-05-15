using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;

namespace MikoApp1;

public class MainLayout : LayoutComponentBase
{
    public override Element Build()
    {
        var sidebar = new DivElement
        {
            Class = "sidebar",
            Children =
            {
                new H2Element
                {
                    TextContent = "Miko Demo",
                    Style = new Style { Color = Color.White }
                },
                CreateNavItem("Button", "/button"),
                CreateNavItem("Form", "/form"),
                CreateNavItem("List", "/list"),
                CreateNavItem("Icon", "/icon"),
                CreateNavItem("Table", "/table"),
                CreateNavItem("Animation", "/animation"),
            }
        };

        var content = new DivElement
        {
            Class = "main-content",
            Children = { BodyElement! }
        };

        return new DivElement
        {
            Class = "layout",
            Children = { sidebar, content }
        };
    }

    private Element CreateNavItem(string label, string path)
    {
        var item = new DivElement
        {
            Class = "nav-item",
            Children = { new SpanElement { TextContent = label } }
        };

        item.OnClick = _ => NavigationManager?.NavigateTo(path);
        return item;
    }

    public static StyleSheet CreateLayoutStyleSheet()
    {
        return new StyleSheet
        {
            Rules = new List<StyleRule>
            {
                new()
                {
                    Selector = new Miko.Styling.Selectors.ClassSelector("layout"),
                    Style = new Style
                    {
                        Display = Display.Flex,
                        Width = Length.Percent(100),
                        Height = Length.Percent(100),
                    }
                },
                new()
                {
                    Selector = new Miko.Styling.Selectors.ClassSelector("sidebar"),
                    Style = new Style
                    {
                        Width = Length.Px(200),
                        Height = Length.Percent(100),
                        Padding = Length.Px(16),
                        BackgroundColor = Color.FromRgb(52, 58, 64),
                        Color = Color.White,
                    }
                },
                new()
                {
                    Selector = new Miko.Styling.Selectors.ClassSelector("main-content"),
                    Style = new Style
                    {
                        FlexGrow = 1,
                        Padding = Length.Px(16),
                        OverflowY = Overflow.Scroll,
                    }
                },
                new()
                {
                    Selector = new Miko.Styling.Selectors.ClassSelector("nav-item"),
                    Style = new Style
                    {
                        Padding = Length.Px(10),
                        MarginBottom = Length.Px(4),
                        Color = Color.White,
                    }
                },
            }
        };
    }
}
