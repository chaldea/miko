using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

public class PercentWidthScrollbarTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void FlexContainer_PercentWidth_ScrollbarShouldBeWithinCanvas()
    {
        // 模拟 MikoApp1 的布局：layout 宽度 100%，viewport 1017px
        var root = new DivElement
        {
            Class = "layout",
            Children =
            {
                new DivElement { Class = "sidebar" },
                new DivElement
                {
                    Class = "main-content",
                    Children =
                    {
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                    }
                }
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
                            Padding = Length.Px(16),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("main-content"),
                        Style = new Style
                        {
                            FlexGrow = 1,
                            Padding = Length.Px(16),
                            OverflowY = Overflow.Scroll,
                        }
                    },
                }
            }
        };

        float viewportWidth = 1017;
        float viewportHeight = 700;

        var layout = _layoutEngine.Layout(root, styleSheets, viewportWidth, viewportHeight);

        // 根容器宽度应该等于 viewport 宽度
        layout.BoxModel.Content.Width.ShouldBe(viewportWidth);

        var sidebar = layout.Children[0];
        var mainContent = layout.Children[1];

        // main-content 应该有滚动条
        mainContent.HasVerticalScrollbar.ShouldBeTrue();

        // 滚动条右边界应该紧贴 padding box 右边缘（即 border box 右边缘 - border width）
        var paddingBox = mainContent.BoxModel.PaddingBox;
        paddingBox.Right.ShouldBe(viewportWidth,
            $"Scrollbar right edge (paddingBox.Right={paddingBox.Right}) should align with canvas right ({viewportWidth})");

        // main-content 的 border box 右边界不应超出画布
        mainContent.BoxModel.BorderBox.Right.ShouldBeLessThanOrEqualTo(viewportWidth,
            $"MainContent border box right ({mainContent.BoxModel.BorderBox.Right}) exceeds canvas width ({viewportWidth})");
    }

    [Fact]
    public void Diagnostic_FlexContainer_PercentWidth_LayoutValues()
    {
        var root = new DivElement
        {
            Class = "layout",
            Children =
            {
                new DivElement { Class = "sidebar" },
                new DivElement
                {
                    Class = "main-content",
                    Children =
                    {
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                    }
                }
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
                            Padding = Length.Px(16),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("main-content"),
                        Style = new Style
                        {
                            FlexGrow = 1,
                            Padding = Length.Px(16),
                            OverflowY = Overflow.Scroll,
                        }
                    },
                }
            }
        };

        float viewportWidth = 1017;
        float viewportHeight = 700;

        var layout = _layoutEngine.Layout(root, styleSheets, viewportWidth, viewportHeight);

        var sidebarBox = layout.Children[0];
        var mainContentBox = layout.Children[1];

        // 输出所有关键值
        var msg = $"\nRoot content: {layout.BoxModel.Content}" +
                  $"\nSidebar content: {sidebarBox.BoxModel.Content}" +
                  $"\nSidebar margin box: {sidebarBox.BoxModel.MarginBox}" +
                  $"\nMainContent content: {mainContentBox.BoxModel.Content}" +
                  $"\nMainContent padding: {mainContentBox.BoxModel.Padding}" +
                  $"\nMainContent border box: {mainContentBox.BoxModel.BorderBox}" +
                  $"\nMainContent margin box: {mainContentBox.BoxModel.MarginBox}";

        // flex-grow 子元素的 content width 应该 = 容器宽度 - sidebar 占用宽度 - 自身 padding
        float expectedMainContentWidth = viewportWidth - sidebarBox.BoxModel.MarginBox.Width - 32; // 32 = padding left + right
        mainContentBox.BoxModel.Content.Width.ShouldBe(expectedMainContentWidth, msg);
    }

    [Fact]
    public void FlexContainer_FixedWidth_ScrollbarShouldBeWithinCanvas()
    {
        // 对比：使用固定宽度 1000px
        var root = new DivElement
        {
            Class = "layout",
            Children =
            {
                new DivElement { Class = "sidebar" },
                new DivElement
                {
                    Class = "main-content",
                    Children =
                    {
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                    }
                }
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
                            Width = Length.Px(1000),
                            Height = Length.Px(700),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("sidebar"),
                        Style = new Style
                        {
                            Width = Length.Px(200),
                            Height = Length.Percent(100),
                            Padding = Length.Px(16),
                        }
                    },
                    new()
                    {
                        Selector = new ClassSelector("main-content"),
                        Style = new Style
                        {
                            FlexGrow = 1,
                            Padding = Length.Px(16),
                            OverflowY = Overflow.Scroll,
                        }
                    },
                }
            }
        };

        float viewportWidth = 1017;
        float viewportHeight = 700;

        var layout = _layoutEngine.Layout(root, styleSheets, viewportWidth, viewportHeight);

        var mainContent = layout.Children[1];

        // 固定宽度时滚动条应该正常显示
        mainContent.HasVerticalScrollbar.ShouldBeTrue();

        // 滚动条右边界不应超出容器
        float scrollbarRight = mainContent.BoxModel.Content.Right;
        scrollbarRight.ShouldBeLessThanOrEqualTo(1000f);
    }
}
