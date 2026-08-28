using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Miko.Animation;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Animation;

public sealed class PaintOnlyAnimationTests
{
    [Fact]
    public void DeclarativeAnimation_FirstPaintUsesStartKeyframe()
    {
        var root = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(100),
                Height = Length.Px(100),
                BackgroundColor = Color.White
            }
        };
        var child = new DivElement
        {
            Style = new Style
            {
                Width = Length.Px(100),
                Height = Length.Px(100),
                BackgroundColor = Color.Red,
                Opacity = 1f,
                Animations = new List<KeyframeAnimation>
                {
                    new("fade-in", 1f,
                        new Keyframe(0f, new Style { Opacity = 0f }),
                        new Keyframe(1f, new Style { Opacity = 1f }))
                    {
                        TimingFunction = TimingFunction.Linear
                    }
                }
            }
        };
        root.AddChild(child);

        var engine = new MikoEngineBuilder().Build();
        using var bitmap = new SKBitmap(100, 100);
        using var canvas = new SKCanvas(bitmap);

        engine.Initialize(root, new List<StyleSheet>(), canvas, 100, 100);

        var firstFrame = bitmap.GetPixel(50, 50);
        firstFrame.Red.ShouldBeGreaterThan((byte)245);
        firstFrame.Green.ShouldBeGreaterThan((byte)245);
        firstFrame.Blue.ShouldBeGreaterThan((byte)245);
        child.LayoutBox!.ComputedStyle.Opacity.ShouldBe(0f, 0.001f);
    }

    [Fact]
    public void OpacityAnimation_DoesNotInvalidateLayout()
    {
        var root = new DivElement { Style = new Style { Width = Length.Px(200), Height = Length.Px(100) } };
        var child = new DivElement
        {
            Id = "fade",
            Style = new Style
            {
                Width = Length.Px(20),
                Height = Length.Px(20),
                Animations = new List<KeyframeAnimation>
                {
                    new("fade", 1f,
                        new Keyframe(0f, new Style { Opacity = 0f }),
                        new Keyframe(1f, new Style { Opacity = 1f }))
                    {
                        Infinite = true,
                        TimingFunction = TimingFunction.Linear
                    }
                }
            }
        };
        root.AddChild(child);
        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo(200, 100));

        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, 200, 100);
        engine.Render(surface.Canvas);
        var layout = engine.GetCurrentLayout();
        var version = engine.Mutations.Version;

        engine.AnimationManager.Update(0.5f);
        engine.Mutations.Version.ShouldBe(version);
        engine.HasPendingVisualWork.ShouldBeTrue();
        engine.Render(surface.Canvas);

        engine.GetCurrentLayout().ShouldBeSameAs(layout);
        child.LayoutBox!.ComputedStyle.Opacity.ShouldBe(0.5f, 0.05f);
    }

    [Fact]
    public void PaintOnlyOverlay_IsClearedWhenTransitionCompletes()
    {
        var element = new DivElement { Style = new Style { Opacity = 1f } };
        var manager = new AnimationManager();
        manager.TrackPropertyChange(element, nameof(Style.Opacity), 1f, 0f,
            Transition.For(nameof(Style.Opacity), 1f, TimingFunction.Linear));

        manager.Update(0.5f);
        element.Style!.Opacity!.Value.Value.ShouldBe(0.5f, 0.05f);

        manager.Update(0.5f);
        manager.Overlay.TryGet(element, out _).ShouldBeFalse();
    }
}
