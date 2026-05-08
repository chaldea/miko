using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

public class PercentHeightTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void FlexContainer_WithPercentHeight_ShouldResolveToViewportHeight()
    {
        var root = new DivElement
        {
            Class = "layout",
            Children =
            {
                new DivElement { Class = "sidebar" },
                new DivElement { Class = "main-content" }
            }
        };

        var styleSheets = new List<StyleSheet>
        {
            new StyleSheet
            {
                Rules = new List<StyleRule>
                {
                    new()
                    {
                        Selector = new ClassSelector("layout"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            Width = Length.Percent(100),
                            Height = Length.Percent(100),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("sidebar"),
                        Style = new Style
                        {
                            Width = Length.Px(200),
                            Height = Length.Percent(100),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("main-content"),
                        Style = new Style
                        {
                            FlexGrow = 1,
                            OverflowY = Overflow.Auto,
                        }
                    },
                }
            }
        };

        var layout = _layoutEngine.Layout(root, styleSheets, 800, 600);

        // 根容器的 height: 100% 应该解析为 viewport height (600px)
        layout.BoxModel.Content.Height.ShouldBe(600);

        // sidebar 的 height: 100% 应该解析为父容器的 content height (600px)
        var sidebar = layout.Children[0];
        sidebar.BoxModel.Content.Height.ShouldBe(600);

        // main-content 应该从 flex 容器获得 600px 的高度约束
        var mainContent = layout.Children[1];
        mainContent.BoxModel.Content.Height.ShouldBe(600);
    }
}
