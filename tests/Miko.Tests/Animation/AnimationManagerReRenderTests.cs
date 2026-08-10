using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Layout;
using Miko.Platform;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Animation;

/// <summary>
/// Tests that animations on dynamically added elements (via re-render) are properly started.
/// Regression test for IonLoading spinner animation issue.
/// </summary>
public class AnimationManagerReRenderTests
{
    [Fact]
    public void AnimationsOnNewElements_AfterReRender_ShouldStart()
    {
        // Arrange: initial tree without animations
        var root = new DivElement { Id = "root" };
        var button = new ButtonElement { Id = "btn", TextContent = "Show" };
        root.AddChild(button);

        var styleSheet = new StyleSheet();
        var engine = CreateEngine();

        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);
        engine.Render(canvas);

        // Initial render should have no active animations
        engine.AnimationManager.ActiveTransitionCount.ShouldBe(0);

        // Act: simulate re-render that adds an element with an animation
        var spinner = new DivElement
        {
            Id = "spinner",
            Style = new Style
            {
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation
                    {
                        Name = "spin",
                        Duration = 1.0f,
                        Infinite = true,
                        Keyframes = new List<Keyframe>
                        {
                            new Keyframe(0f, new Style { Transform = Transform.FromRotate(0) }),
                            new Keyframe(1f, new Style { Transform = Transform.FromRotate(360) })
                        }
                    }
                }
            }
        };
        root.AddChild(spinner);

        // Trigger re-render (simulates StateHasChanged in Razor)
        Element.BumpMutationVersion();
        engine.Render(canvas);

        // Assert: the animation should now be active
        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue();

        // Verify the spinner element has the animation started
        var activeAnimations = GetActiveAnimationCount(engine.AnimationManager);
        activeAnimations.ShouldBe(1);
    }

    [Fact]
    public void AnimationsOnNewElements_InFastPath_ShouldStart()
    {
        // Arrange: set up engine with initial tree
        var root = new DivElement { Id = "root" };
        var styleSheet = new StyleSheet();
        var engine = CreateEngine();

        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);
        engine.Render(canvas);

        // Act: add animated element without changing layout inputs (fast path)
        var spinner = new DivElement
        {
            Id = "spinner",
            Style = new Style
            {
                Width = Length.Px(50),
                Height = Length.Px(50),
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation
                    {
                        Name = "pulse",
                        Duration = 0.5f,
                        Infinite = true,
                        Keyframes = new List<Keyframe>
                        {
                            new Keyframe(0f, new Style { Opacity = 0.5f }),
                            new Keyframe(1f, new Style { Opacity = 1f })
                        }
                    }
                }
            }
        };
        root.AddChild(spinner);
        Element.BumpMutationVersion();

        // First render after adding element
        engine.Render(canvas);
        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue();

        // Second render should take fast path (no layout change) but still have animation running
        engine.Render(canvas);
        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue();

        var activeAnimations = GetActiveAnimationCount(engine.AnimationManager);
        activeAnimations.ShouldBe(1);
    }

    [Fact]
    public void DuplicateAnimation_ShouldNotBeStartedTwice()
    {
        // Arrange
        var spinner = new DivElement
        {
            Id = "spinner",
            Style = new Style
            {
                Animations = new List<KeyframeAnimation>
                {
                    new KeyframeAnimation
                    {
                        Name = "rotate",
                        Duration = 1.0f,
                        Infinite = true,
                        Keyframes = new List<Keyframe>
                        {
                            new Keyframe(0f, new Style { Transform = Transform.FromRotate(0) }),
                            new Keyframe(1f, new Style { Transform = Transform.FromRotate(360) })
                        }
                    }
                }
            }
        };

        var root = new DivElement { Id = "root" };
        root.AddChild(spinner);

        var styleSheet = new StyleSheet();
        var engine = CreateEngine();

        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);

        // Act: render multiple times
        engine.Render(canvas);
        engine.Render(canvas);
        engine.Render(canvas);

        // Assert: should only have one active animation instance
        var activeAnimations = GetActiveAnimationCount(engine.AnimationManager);
        activeAnimations.ShouldBe(1);
    }

    [Fact]
    public void MultipleAnimatedElements_ShouldAllStart()
    {
        // Arrange
        var root = new DivElement { Id = "root" };
        var styleSheet = new StyleSheet();
        var engine = CreateEngine();

        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);
        engine.Render(canvas);

        // Act: add three animated spinners
        for (int i = 0; i < 3; i++)
        {
            var spinner = new DivElement
            {
                Id = $"spinner{i}",
                Style = new Style
                {
                    Animations = new List<KeyframeAnimation>
                    {
                        new KeyframeAnimation
                        {
                            Name = $"anim{i}",
                            Duration = 1.0f,
                            Infinite = true,
                            Keyframes = new List<Keyframe>
                            {
                                new Keyframe(0f, new Style { Opacity = 0f }),
                                new Keyframe(1f, new Style { Opacity = 1f })
                            }
                        }
                    }
                }
            };
            root.AddChild(spinner);
        }

        Element.BumpMutationVersion();
        engine.Render(canvas);

        // Assert: all three animations should be active
        var activeAnimations = GetActiveAnimationCount(engine.AnimationManager);
        activeAnimations.ShouldBe(3);
    }

    private MikoEngine CreateEngine()
    {
        var layoutEngine = new LayoutEngine();
        var renderEngine = new RenderEngine();
        var dirtyManager = new DirtyRegionManager();
        var eventDispatcher = new EventDispatcher();
        var animationManager = new AnimationManager();
        var dispatcher = new MikoDispatcher();

        return new MikoEngine(layoutEngine, renderEngine, dirtyManager, eventDispatcher, animationManager, dispatcher);
    }

    // Helper to access the count of active animations via reflection
    private int GetActiveAnimationCount(AnimationManager manager)
    {
        var field = typeof(AnimationManager).GetField("_animations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = field?.GetValue(manager) as System.Collections.IList;
        return list?.Count ?? 0;
    }
}
