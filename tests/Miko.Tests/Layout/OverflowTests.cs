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
    public void ScrollbarWidthNone_ShouldNotReserveWidthButRemainScrollable()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Scroll,
                ScrollbarWidth = ScrollbarWidth.None,
            },
            Children = { new DivElement { Style = new Style { Height = Length.Px(300) } } }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        layout.HasVerticalScrollbar.ShouldBeTrue();
        layout.ShowsVerticalScrollbar.ShouldBeFalse();
        layout.Children[0].BoxModel.Content.Width.ShouldBe(200f);
    }

    [Fact]
    public void ScrollbarWidthThin_ShouldReserveThinWidth()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
                OverflowY = Overflow.Scroll,
                ScrollbarWidth = ScrollbarWidth.Thin,
            },
            Children = { new DivElement() }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        layout.ShowsVerticalScrollbar.ShouldBeTrue();
        layout.VerticalScrollbarThickness.ShouldBe(LayoutBox.ThinScrollbarThickness);
        layout.Children[0].BoxModel.Content.Width.ShouldBe(200 - LayoutBox.ThinScrollbarThickness);
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
        computed.ScrollbarWidth.ShouldBe(ScrollbarWidth.Auto);
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

    #region ISSUE-105：块流中 height:auto + overflow 不应被父级定高撑满

    [Theory]
    [InlineData(Overflow.Hidden)]
    [InlineData(Overflow.Auto)]
    [InlineData(Overflow.Scroll)]
    public void BlockChild_AutoHeight_WithOverflow_ShouldSizeToContent_NotFillParent(Overflow overflow)
    {
        // ISSUE-105：块流中父级的确定高度只是子孙百分比解析基准，不是填充指令；
        // height:auto + overflow 的块级子元素高度仍由内容决定，不应被撑满父级高度。
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(500), Height = Length.Px(500) },
            Children =
            {
                new DivElement
                {
                    Style = new Style
                    {
                        Width = Length.Px(200),
                        Border = new Border(Length.Px(1), BorderStyle.Solid, Color.Black),
                        Padding = new Padding(Length.Px(2), Length.Px(5)),
                        OverflowY = overflow,
                    },
                    Children =
                    {
                        new DivElement { Style = new Style { Height = Length.Px(30) } }
                    }
                }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        var child = layout.Children[0];
        // 内容高 = 子内容 30px；border-box 高 = 30 + padding(2+2) + border(1+1) = 36，而非父高 500。
        child.BoxModel.Content.Height.ShouldBe(30);
        child.BoxModel.MarginBox.Height.ShouldBe(36);
    }

    [Fact]
    public void BlockChild_AutoHeight_OverflowHidden_TextContent_ShouldKeepLineHeight()
    {
        // ISSUE-105 的原始复现：nowrap + ellipsis 的单行文本 div 设置 overflow:hidden 后
        // 高度应保持为一行文本高度（与 overflow:visible 一致），而非撑满 500px 的父级。
        static DivElement BuildRoot(Overflow overflow) => new()
        {
            Style = new Style { Width = Length.Px(500), Height = Length.Px(500) },
            Children =
            {
                new DivElement
                {
                    Style = new Style
                    {
                        Width = Length.Px(200),
                        Border = new Border(Length.Px(1), BorderStyle.Solid, Color.Black),
                        Padding = new Padding(Length.Px(2), Length.Px(5)),
                        WhiteSpace = WhiteSpace.Nowrap,
                        Overflow = overflow,
                        TextOverflow = TextOverflow.Ellipsis,
                    },
                    TextContent = "asdadasdadadadaadasdadaddasdadaasdadadadadadas"
                }
            }
        };

        var hiddenLayout = _layoutEngine.Layout(BuildRoot(Overflow.Hidden), new List<StyleSheet>(), 800, 600);
        var visibleLayout = _layoutEngine.Layout(BuildRoot(Overflow.Visible), new List<StyleSheet>(), 800, 600);

        var txtHidden = hiddenLayout.Children[0];
        var txtVisible = visibleLayout.Children[0];

        // overflow:hidden 与 overflow:visible 的自动高度必须一致（单行文本高度）。
        txtHidden.BoxModel.MarginBox.Height.ShouldBe(txtVisible.BoxModel.MarginBox.Height, 0.01f);
        // 明确回归断言：远小于父级 500px（修复前会被撑满为 500）。
        txtHidden.BoxModel.MarginBox.Height.ShouldBeLessThan(100);
    }

    [Fact]
    public void FlexContainer_AsBlockChild_AutoHeight_WithOverflow_ShouldSizeToContent_NotFillParent()
    {
        // ISSUE-105 的 flex 变体：display:flex 的盒子作为块流子元素时同样不应被父级定高撑满。
        // 第二个子元素 height:100%：容器高度为 auto 时它对子元素百分比而言是不确定包含块，
        // 百分比应退化（修复前会相对父级 500px 解析，把容器撑满）。
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(500), Height = Length.Px(500) },
            Children =
            {
                new DivElement
                {
                    Style = new Style
                    {
                        Display = Display.Flex,
                        OverflowY = Overflow.Hidden,
                    },
                    Children =
                    {
                        new DivElement { Style = new Style { Width = Length.Px(50), Height = Length.Px(30) } },
                        new DivElement { Style = new Style { Width = Length.Px(50), Height = Length.Percent(100) } }
                    }
                }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        layout.Children[0].BoxModel.Content.Height.ShouldBe(30);
    }

    [Fact]
    public void GridContainer_AsBlockChild_AutoHeight_WithOverflow_ShouldSizeToContent_NotFillParent()
    {
        // ISSUE-105 的 grid 变体：display:grid 的盒子作为块流子元素时同样不应被父级定高撑满。
        // 百分比行轨道相对 auto 高度的容器应退化（修复前容器被当作定高，第二行解析为
        // 500px，把容器撑满）。
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(500), Height = Length.Px(500) },
            Children =
            {
                new DivElement
                {
                    Style = new Style
                    {
                        Display = Display.Grid,
                        OverflowY = Overflow.Hidden,
                        GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100) },
                        GridTemplateRows = new List<GridTrackSize> { GridTrackSize.Px(30), GridTrackSize.Percent(100) },
                    },
                    Children =
                    {
                        new DivElement(),
                        new DivElement()
                    }
                }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        layout.Children[0].BoxModel.Content.Height.ShouldBe(30);
    }

    [Fact]
    public void PercentGrandchild_ShouldNotResolveAgainstGrandparentHeight_WhenParentIsAutoHeightBlockFlow()
    {
        // ISSUE-105 的级联变体：height:auto 的块流子元素（即便带 overflow）其高度对孙元素
        // 百分比而言是不确定的——孙元素 height:100% 不应相对祖父的 500px 解析，
        // 应退化为内容尺寸（空内容即为 0）。
        var root = new DivElement
        {
            Style = new Style { Width = Length.Px(500), Height = Length.Px(500) },
            Children =
            {
                new DivElement
                {
                    Style = new Style
                    {
                        Width = Length.Px(200),
                        OverflowY = Overflow.Hidden,
                    },
                    Children =
                    {
                        new DivElement
                        {
                            Style = new Style { Height = Length.Percent(100) }
                        }
                    }
                }
            }
        };

        var layout = _layoutEngine.Layout(root, new List<StyleSheet>(), 800, 600);

        var grandchild = layout.Children[0].Children[0];
        grandchild.BoxModel.Content.Height.ShouldBe(0);
    }

    #endregion
}
