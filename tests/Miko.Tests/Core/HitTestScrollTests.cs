using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

public class HitTestScrollTests
{
    private MikoEngine CreateEngine(Element root, float width = 400, float height = 300)
    {
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)width, (int)height));
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, width, height);
        return engine;
    }

    [Fact]
    public void HitTest_NoScroll_ShouldHitChildAtLayoutPosition()
    {
        var child = new DivElement
        {
            Id = "target",
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(100),
                Height = Length.Px(50),
            }
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
                new DivElement { Style = new Style { Height = Length.Px(800) } }
            }
        };

        var engine = CreateEngine(root);

        var hit = engine.HitTest(50, 25);
        hit.ShouldBe(child);
    }

    [Fact]
    public void HitTest_AfterScroll_ShouldHitCorrectChild()
    {
        var child1 = new DivElement
        {
            Id = "child1",
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(200),
            }
        };

        var child2 = new DivElement
        {
            Id = "child2",
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(200),
            }
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
            Children = { child1, child2 }
        };

        var engine = CreateEngine(root);

        // Before scroll: clicking at y=100 should hit child1 (0..200)
        engine.HitTest(200, 100).ShouldBe(child1);

        // Scroll down by 150px
        engine.ScrollBy(200, 150, 0, 150);

        // After scroll: child1 is now at screen y=-150..50, child2 at screen y=50..250
        // Clicking at y=100 should now hit child2 (not child1)
        engine.HitTest(200, 100).ShouldBe(child2);
    }

    [Fact]
    public void HitTest_AfterScroll_ShouldNotHitScrolledOutChild()
    {
        var child1 = new DivElement
        {
            Id = "child1",
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(100),
            }
        };

        var filler = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(600),
            }
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
            Children = { child1, filler }
        };

        var engine = CreateEngine(root);

        // Scroll down so child1 is completely above the viewport
        engine.ScrollBy(200, 150, 0, 200);

        // Click at y=10 (top of viewport) should NOT hit child1
        // because child1 is scrolled out of view
        var hit = engine.HitTest(200, 10);
        hit.ShouldNotBe(child1);
    }

    [Fact]
    public void HitTest_HorizontalScroll_ShouldHitCorrectChild()
    {
        var child1 = new DivElement
        {
            Id = "child1",
            Style = new Style
            {
                Display = Display.InlineBlock,
                Width = Length.Px(200),
                Height = Length.Px(100),
            }
        };

        var child2 = new DivElement
        {
            Id = "child2",
            Style = new Style
            {
                Display = Display.InlineBlock,
                Width = Length.Px(200),
                Height = Length.Px(100),
            }
        };

        var root = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(300),
                Height = Length.Px(300),
                OverflowX = Overflow.Scroll,
            },
            Children = { child1, child2 }
        };

        var engine = CreateEngine(root);

        // Before scroll: child1 at x=0..200, child2 at x=200..400
        engine.HitTest(100, 50).ShouldBe(child1);

        // Scroll right by 150px
        engine.ScrollBy(150, 150, 150, 0);

        // After scroll: child1 at screen x=-150..50, child2 at screen x=50..250
        // Click at x=100 should now hit child2
        engine.HitTest(100, 50).ShouldBe(child2);
    }

    [Fact]
    public void HitTest_NestedScroll_ShouldAccumulateOffsets()
    {
        var innerChild = new DivElement
        {
            Id = "inner-target",
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(100),
            }
        };

        var innerFiller = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(400),
            }
        };

        var innerContainer = new DivElement
        {
            Id = "inner-scroll",
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(200),
                Height = Length.Px(150),
                OverflowY = Overflow.Auto,
            },
            Children = { innerChild, innerFiller }
        };

        var outerFiller = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(600),
            }
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
            Children = { innerContainer, outerFiller }
        };

        var engine = CreateEngine(root);

        // innerChild is at layout y=0 inside innerContainer
        // innerContainer is at layout y=0 inside root
        // Click at y=50 should hit innerChild
        engine.HitTest(100, 50).ShouldBe(innerChild);

        // Scroll the outer container down by 50
        engine.ScrollBy(200, 200, 0, 50);

        // Now innerContainer is at screen y=-50..100
        // innerChild inside it is at screen y=-50..50
        // Click at y=25 should still hit innerChild (it's partially visible)
        engine.HitTest(100, 25).ShouldBe(innerChild);

        // Click at y=75 should hit innerFiller (screen y=50..visible)
        var hitAt75 = engine.HitTest(100, 75);
        hitAt75.ShouldNotBe(innerChild);
    }

    [Fact]
    public void HitTest_AfterScroll_ClickOnContainerArea_ShouldHitContainer()
    {
        var child = new DivElement
        {
            Id = "child",
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(100),
            }
        };

        var filler = new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(500),
            }
        };

        var root = new DivElement
        {
            Id = "container",
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children = { child, filler }
        };

        var engine = CreateEngine(root);

        // Scroll down so child is partially out of view
        engine.ScrollBy(200, 150, 0, 80);

        // Click at y=250 - this is in the container area where filler is visible
        var hit = engine.HitTest(200, 250);
        hit.ShouldBe(filler);
    }

    [Fact]
    public void HitTest_ScrolledElement_ClickBelowOriginalPosition_ShouldNotHitChild()
    {
        var child = new DivElement
        {
            Id = "child",
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(50),
            }
        };

        var filler = new DivElement
        {
            Id = "filler",
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(800),
            }
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
            Children = { child, filler }
        };

        var engine = CreateEngine(root);

        // Scroll down by 100 - child (layout y=0..50) is now at screen y=-100..-50
        engine.ScrollBy(200, 150, 0, 100);

        // Click at y=25 (where child used to be before scroll) should NOT hit child
        var hit = engine.HitTest(200, 25);
        hit.ShouldNotBe(child);
        hit.ShouldBe(filler);
    }
}
