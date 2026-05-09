using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Layout;

public class ScrollbarInteractionTests
{
    private MikoEngine CreateEngine(Element root, float width = 400, float height = 300)
    {
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)width, (int)height));
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, width, height);
        return engine;
    }

    private DivElement CreateScrollableRoot(float contentHeight = 800, float viewportHeight = 300)
    {
        return new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(400),
                Height = Length.Px(viewportHeight),
                OverflowY = Overflow.Auto,
            },
            Children =
            {
                new DivElement { Style = new Style { Height = Length.Px(contentHeight) } }
            }
        };
    }

    [Fact]
    public void HitTestScrollbar_OnVerticalTrack_ReturnsVerticalTrackHit()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        // Vertical scrollbar is at x = paddingBox.Right - 17 = 400 - 17 = 383
        // Click in the track area (not on thumb)
        var hit = engine.HitTestScrollbar(390, 250);

        hit.ShouldNotBeNull();
        hit!.HitType.ShouldBe(MikoEngine.ScrollbarHitType.VerticalTrack);
    }

    [Fact]
    public void HitTestScrollbar_OnVerticalThumb_ReturnsVerticalThumbHit()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        // Thumb starts at top of track (scrollTop=0), click near top of scrollbar
        var hit = engine.HitTestScrollbar(390, 10);

        hit.ShouldNotBeNull();
        hit!.HitType.ShouldBe(MikoEngine.ScrollbarHitType.VerticalThumb);
    }

    [Fact]
    public void HitTestScrollbar_OutsideScrollbar_ReturnsNull()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        // Click in content area, not on scrollbar
        var hit = engine.HitTestScrollbar(200, 150);

        hit.ShouldBeNull();
    }

    [Fact]
    public void HitTestScrollbar_NoScrollbar_ReturnsNull()
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
                new DivElement { Style = new Style { Height = Length.Px(100) } }
            }
        };
        var engine = CreateEngine(root);

        var hit = engine.HitTestScrollbar(390, 150);

        hit.ShouldBeNull();
    }

    [Fact]
    public void DragVerticalThumb_MovesScrollTop()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        var layout = engine.GetCurrentLayout()!;

        // Drag thumb to middle of track
        // trackHeight = 300, thumbHeight = max(300 * 300/800, 20) ≈ 112.5
        // scrollableTrack ≈ 187.5, drag to y=150 with thumbOffset=0
        engine.DragVerticalThumb(layout, 150, 0);

        layout.ScrollTop.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void DragVerticalThumb_ToTop_SetsScrollTopToZero()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        var layout = engine.GetCurrentLayout()!;
        layout.ScrollTop = 200;

        engine.DragVerticalThumb(layout, 0, 0);

        layout.ScrollTop.ShouldBe(0);
    }

    [Fact]
    public void DragVerticalThumb_ToBottom_SetsScrollTopToMax()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        var layout = engine.GetCurrentLayout()!;

        // Drag to bottom of track
        engine.DragVerticalThumb(layout, 300, 0);

        float maxScroll = layout.ScrollableContentHeight - layout.BoxModel.PaddingBox.Height;
        layout.ScrollTop.ShouldBe(maxScroll, tolerance: 1f);
    }

    [Fact]
    public void ScrollTrackClick_BelowThumb_ScrollsDownByPage()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        var layout = engine.GetCurrentLayout()!;
        float initialScrollTop = layout.ScrollTop; // 0

        // Click below the thumb (thumb is at top when scrollTop=0)
        engine.ScrollTrackClick(layout, MikoEngine.ScrollbarHitType.VerticalTrack, 390, 250);

        float expectedDelta = layout.BoxModel.PaddingBox.Height * 0.875f;
        layout.ScrollTop.ShouldBeGreaterThan(initialScrollTop);
        layout.ScrollTop.ShouldBe(expectedDelta, tolerance: 1f);
    }

    [Fact]
    public void ScrollTrackClick_AboveThumb_ScrollsUpByPage()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        var layout = engine.GetCurrentLayout()!;
        layout.ScrollTop = 400; // scroll to middle

        float initialScrollTop = layout.ScrollTop;
        float expectedDelta = layout.BoxModel.PaddingBox.Height * 0.875f;

        // Click above the thumb (thumb is near bottom when scrollTop=400)
        engine.ScrollTrackClick(layout, MikoEngine.ScrollbarHitType.VerticalTrack, 390, 10);

        layout.ScrollTop.ShouldBeLessThan(initialScrollTop);
        layout.ScrollTop.ShouldBe(initialScrollTop - expectedDelta, tolerance: 1f);
    }

    [Fact]
    public void ScrollTrackClick_ClampsToMaxScroll()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        var layout = engine.GetCurrentLayout()!;
        float maxScroll = layout.ScrollableContentHeight - layout.BoxModel.PaddingBox.Height;
        layout.ScrollTop = maxScroll - 10; // near bottom

        engine.ScrollTrackClick(layout, MikoEngine.ScrollbarHitType.VerticalTrack, 390, 290);

        layout.ScrollTop.ShouldBe(maxScroll, tolerance: 0.1f);
    }

    [Fact]
    public void ScrollTrackClick_ClampsToZero()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        var layout = engine.GetCurrentLayout()!;
        float pageSize = layout.BoxModel.PaddingBox.Height * 0.875f;
        layout.ScrollTop = pageSize / 2; // less than one page from top

        // Click above the thumb — should scroll up but clamp to 0
        engine.ScrollTrackClick(layout, MikoEngine.ScrollbarHitType.VerticalTrack, 390, 1);

        layout.ScrollTop.ShouldBe(0);
    }

    [Fact]
    public void HitTestScrollbar_VerticalThumb_ReturnsCorrectThumbOffset()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        // Thumb is at top (scrollTop=0), click 5px into the thumb
        var hit = engine.HitTestScrollbar(390, 5);

        hit.ShouldNotBeNull();
        hit!.HitType.ShouldBe(MikoEngine.ScrollbarHitType.VerticalThumb);
        hit.ThumbOffset.ShouldBe(5f, tolerance: 1f);
    }

    [Fact]
    public void DragVerticalThumb_InvalidatesElement()
    {
        var root = CreateScrollableRoot();
        var engine = CreateEngine(root);

        var layout = engine.GetCurrentLayout()!;
        bool invalidated = engine.DragVerticalThumb(layout, 150, 0);

        invalidated.ShouldBeTrue();
    }

    [Fact]
    public void HorizontalScrollbar_HitTest_ReturnsHorizontalHit()
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
        var engine = CreateEngine(root, 300, 300);

        // Horizontal scrollbar is at y = paddingBox.Bottom - 17 = 300 - 17 = 283
        var hit = engine.HitTestScrollbar(50, 290);

        hit.ShouldNotBeNull();
        (hit!.HitType == MikoEngine.ScrollbarHitType.HorizontalThumb ||
         hit.HitType == MikoEngine.ScrollbarHitType.HorizontalTrack).ShouldBeTrue();
    }
}
