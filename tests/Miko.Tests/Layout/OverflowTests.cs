using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

public class OverflowTests
{
    private readonly LayoutEngine _layoutEngine = new();

    [Fact]
    public void OverflowVisible_ShouldNotClipContent()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                Overflow = Overflow.Visible
            },
            Children =
            {
                new DivElement
                {
                    Style = new Style { Height = Length.Px(300) }
                }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        layout.BoxModel.Content.Height.ShouldBe(100);
        layout.HasVerticalScrollbar.ShouldBeFalse();
        layout.HasHorizontalScrollbar.ShouldBeFalse();
    }

    [Fact]
    public void OverflowScroll_ShouldAlwaysShowScrollbar()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Scroll
            },
            Children =
            {
                new DivElement
                {
                    Style = new Style { Height = Length.Px(50) }
                }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        // overflow: scroll 始终显示滚动条，即使内容没有溢出
        layout.HasVerticalScrollbar.ShouldBeTrue();
    }

    [Fact]
    public void OverflowAuto_ShouldShowScrollbarOnlyWhenContentOverflows()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Auto
            },
            Children =
            {
                new DivElement
                {
                    Style = new Style { Height = Length.Px(50) }
                }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        // 内容没有溢出，不显示滚动条
        layout.HasVerticalScrollbar.ShouldBeFalse();
    }

    [Fact]
    public void OverflowAuto_ShouldShowScrollbarWhenContentOverflows()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Auto
            },
            Children =
            {
                new DivElement
                {
                    Style = new Style { Height = Length.Px(300) }
                }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        // 内容溢出，显示滚动条
        layout.HasVerticalScrollbar.ShouldBeTrue();
    }

    [Fact]
    public void OverflowScroll_ShouldReserveScrollbarWidth()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Scroll
            },
            Children =
            {
                new DivElement { Id = "child" }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        // Classic 模式下，滚动条占用 17px 宽度
        var child = layout.Children[0];
        child.BoxModel.Content.Width.ShouldBe(200 - LayoutBox.ScrollbarThickness);
    }

    [Fact]
    public void OverflowHidden_ShouldNotShowScrollbar()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Hidden
            },
            Children =
            {
                new DivElement
                {
                    Style = new Style { Height = Length.Px(300) }
                }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        // overflow: hidden 不显示滚动条
        layout.HasVerticalScrollbar.ShouldBeFalse();
        layout.BoxModel.Content.Height.ShouldBe(100);
    }

    [Fact]
    public void ScrollableContentHeight_ShouldTrackActualContentSize()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Auto
            },
            Children =
            {
                new DivElement { Style = new Style { Height = Length.Px(150) } },
                new DivElement { Style = new Style { Height = Length.Px(100) } }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        layout.ScrollableContentHeight.ShouldBe(250);
    }

    [Fact]
    public void OverflowX_Scroll_ShouldShowHorizontalScrollbar()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowX = Overflow.Scroll
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        layout.HasHorizontalScrollbar.ShouldBeTrue();
    }

    [Fact]
    public void OverflowShorthand_ShouldSetBothAxes()
    {
        var style = new Style { Overflow = Overflow.Auto };

        style.OverflowX.ShouldBe(Overflow.Auto);
        style.OverflowY.ShouldBe(Overflow.Auto);
    }

    [Fact]
    public void ComputedStyle_OverflowDefault_ShouldBeVisible()
    {
        var computed = ComputedStyle.FromStyle(null);

        computed.OverflowX.ShouldBe(Overflow.Visible);
        computed.OverflowY.ShouldBe(Overflow.Visible);
    }

    [Fact]
    public void ComputedStyle_OverflowFromStyle_ShouldResolve()
    {
        var style = new Style { OverflowY = Overflow.Scroll };
        var computed = ComputedStyle.FromStyle(style);

        computed.OverflowX.ShouldBe(Overflow.Visible);
        computed.OverflowY.ShouldBe(Overflow.Scroll);
    }

    [Fact]
    public void StyleMerge_ShouldCascadeOverflow()
    {
        var style1 = new Style { OverflowY = Overflow.Auto };
        var style2 = new Style { OverflowX = Overflow.Hidden };

        style1.Merge(style2);

        style1.OverflowY.ShouldBe(Overflow.Auto);
        style1.OverflowX.ShouldBe(Overflow.Hidden);
    }

    [Fact]
    public void FlexChild_WithOverflowAuto_ShouldShowScrollbarWhenContentOverflows()
    {
        // 模拟 MikoApp1 的布局结构：flex 容器 + flex-grow 子元素 + overflow-y: auto
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Flex,
                Width = Length.Px(800),
                Height = Length.Px(600),
            },
            Children =
            {
                new DivElement
                {
                    Style = new Style
                    {
                        Width = Length.Px(200),
                        Height = Length.Percent(100),
                    }
                },
                new DivElement
                {
                    Id = "main-content",
                    Style = new Style
                    {
                        FlexGrow = 1,
                        OverflowY = Overflow.Auto,
                    },
                    Children =
                    {
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                        new DivElement { Style = new Style { Height = Length.Px(300) } },
                    }
                }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        var mainContent = layout.Children[1];
        mainContent.BoxModel.Content.Height.ShouldBe(600);
        mainContent.ScrollableContentHeight.ShouldBe(900);
        mainContent.HasVerticalScrollbar.ShouldBeTrue();
    }
}