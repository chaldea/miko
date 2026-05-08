using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Layout;

public class ScrollEventTests
{
    private MikoEngine CreateEngine(Element root, float width = 400, float height = 300)
    {
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)width, (int)height));
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, width, height);
        return engine;
    }

    [Fact]
    public void ScrollBy_ShouldUpdateScrollTop()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children =
            {
                new DivElement { Style = new Style { Height = Length.Px(800) } }
            }
        };

        var engine = CreateEngine(root);

        // 在容器中心滚动
        bool scrolled = engine.ScrollBy(200, 150, 0, 100);

        scrolled.ShouldBeTrue();
        var layout = engine.GetCurrentLayout()!;
        layout.ScrollTop.ShouldBe(100);
    }

    [Fact]
    public void ScrollBy_ShouldClampToZero()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children =
            {
                new DivElement { Style = new Style { Height = Length.Px(800) } }
            }
        };

        var engine = CreateEngine(root);

        // 向上滚动（负值），应该被 clamp 到 0
        bool scrolled = engine.ScrollBy(200, 150, 0, -100);

        scrolled.ShouldBeFalse();
        var layout = engine.GetCurrentLayout()!;
        layout.ScrollTop.ShouldBe(0);
    }

    [Fact]
    public void ScrollBy_ShouldClampToMaxScroll()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children =
            {
                new DivElement { Style = new Style { Height = Length.Px(800) } }
            }
        };

        var engine = CreateEngine(root);

        // 滚动超过最大值
        bool scrolled = engine.ScrollBy(200, 150, 0, 9999);

        scrolled.ShouldBeTrue();
        var layout = engine.GetCurrentLayout()!;
        // maxScroll = contentHeight(800) - paddingBoxHeight(300) = 500
        layout.ScrollTop.ShouldBe(500);
    }

    [Fact]
    public void ScrollBy_ShouldFindScrollableAncestor()
    {
        // 子元素不可滚动，父元素可滚动
        var child = new DivElement
        {
            Id = "child",
            Style = new Style { Height = Length.Px(200) }
        };

        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children =
            {
                child,
                new DivElement { Style = new Style { Height = Length.Px(600) } }
            }
        };

        var engine = CreateEngine(root);

        // 在子元素上滚动，应该找到父容器进行滚动
        bool scrolled = engine.ScrollBy(200, 100, 0, 50);

        scrolled.ShouldBeTrue();
        var layout = engine.GetCurrentLayout()!;
        layout.ScrollTop.ShouldBe(50);
    }

    [Fact]
    public void ScrollBy_ShouldNotScrollNonOverflowElement()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
            },
            Children =
            {
                new DivElement { Style = new Style { Height = Length.Px(800) } }
            }
        };

        var engine = CreateEngine(root);

        // 没有 overflow 设置，不应该滚动
        bool scrolled = engine.ScrollBy(200, 150, 0, 100);

        scrolled.ShouldBeFalse();
    }

    [Fact]
    public void ScrollBy_ShouldDispatchScrollEvent()
    {
        ScrollEventArgs? receivedArgs = null;

        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children =
            {
                new DivElement { Style = new Style { Height = Length.Px(800) } }
            }
        };

        root.OnScroll = args => receivedArgs = args;

        var engine = CreateEngine(root);
        engine.ScrollBy(200, 150, 0, 75);

        receivedArgs.ShouldNotBeNull();
        receivedArgs.DeltaY.ShouldBe(75);
        receivedArgs.ScrollTop.ShouldBe(75);
        receivedArgs.Target.ShouldBe(root);
    }

    [Fact]
    public void ScrollBy_HorizontalScroll_ShouldUpdateScrollLeft()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(300),
                Height = Length.Px(300),
                OverflowX = Overflow.Scroll,
            },
            Children =
            {
                new DivElement
                {
                    Style = new Style
                    {
                        Display = Display.InlineBlock,
                        Width = Length.Px(800),
                        Height = Length.Px(100),
                    }
                }
            }
        };

        var engine = CreateEngine(root);

        bool scrolled = engine.ScrollBy(150, 150, 100, 0);

        scrolled.ShouldBeTrue();
        var layout = engine.GetCurrentLayout()!;
        layout.ScrollLeft.ShouldBe(100);
    }

    [Fact]
    public void ScrollBy_MultipleScrolls_ShouldAccumulate()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children =
            {
                new DivElement { Style = new Style { Height = Length.Px(800) } }
            }
        };

        var engine = CreateEngine(root);

        engine.ScrollBy(200, 150, 0, 50);
        engine.ScrollBy(200, 150, 0, 30);

        var layout = engine.GetCurrentLayout()!;
        layout.ScrollTop.ShouldBe(80);
    }

    [Fact]
    public void ScrollBy_OverflowHidden_ShouldNotScroll()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Hidden,
            },
            Children =
            {
                new DivElement { Style = new Style { Height = Length.Px(800) } }
            }
        };

        var engine = CreateEngine(root);

        // overflow: hidden 裁剪内容但不允许滚动
        bool scrolled = engine.ScrollBy(200, 150, 0, 100);

        scrolled.ShouldBeFalse();
    }

    [Fact]
    public void ScrollBy_ShouldPreserveScrollStateAfterReRender()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children =
            {
                new DivElement { Style = new Style { Height = Length.Px(800) } }
            }
        };

        var engine = CreateEngine(root);

        // 滚动
        engine.ScrollBy(200, 150, 0, 100);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(100);

        // 重新渲染（模拟 WinUI 每帧调用 Render）
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));
        engine.Render(surface.Canvas);

        // 滚动状态应该被保留
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(100);
    }
}
