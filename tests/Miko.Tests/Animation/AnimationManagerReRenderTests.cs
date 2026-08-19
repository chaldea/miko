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

    /// <summary>
    /// ISSUE-127：组件回调触发重渲染会用<b>全新</b>元素实例替换旧子树。动画进度以元素引用为键，
    /// 若不沿 <c>SupersededBy</c> 迁移，同一个逻辑元素每次重渲染都被判为「未运行」而重启回 0
    /// ——现场表现是任意子组件回调都把页面上所有动画打回起点。
    /// </summary>
    [Fact]
    public void AnimationProgress_ShouldSurviveReRender_IntoNewElementInstance()
    {
        var root = new DivElement { Id = "root" };
        var bar = new DivElement { Id = "bar", Style = MakeAnimatedStyle("slide") };
        root.AddChild(bar);

        var engine = CreateEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { new StyleSheet() }, canvas, 800, 600);
        engine.Render(canvas);
        GetActiveAnimationCount(engine.AnimationManager).ShouldBe(1);

        // 推进半个周期
        engine.AnimationManager.Update(0.5f);
        GetElapsedTime(engine.AnimationManager, 0).ShouldBe(0.5f, 0.001f);

        // 模拟 ComponentBase.StateHasChanged：新实例整体替换旧子树，
        // 并由 TransferRuntimeState 留下 SupersededBy 转发指针。
        var rebuilt = new DivElement { Id = "bar", Style = MakeAnimatedStyle("slide") };
        SetSupersededBy(bar, rebuilt);
        root.RemoveChild(bar);
        root.AddChild(rebuilt);

        Element.BumpMutationVersion();
        engine.Render(canvas);

        // 仍是同一条动画，进度未被重置，且已改挂到在场的新实例上。
        GetActiveAnimationCount(engine.AnimationManager).ShouldBe(1);
        GetElapsedTime(engine.AnimationManager, 0).ShouldBe(0.5f, 0.001f);
        GetAnimationElement(engine.AnimationManager, 0).ShouldBeSameAs(rebuilt);
    }

    [Fact]
    public void AnimationCurrentValue_ShouldBeAppliedToReplacement_OnFirstRenderAfterReRender()
    {
        var root = new DivElement { Id = "root" };
        var bar = new DivElement { Id = "bar", Style = MakeAnimatedStyle("slide") };
        root.AddChild(bar);

        var engine = CreateEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { new StyleSheet() }, canvas, 800, 600);
        engine.AnimationManager.Update(0.5f);
        var currentMargin = bar.Style!.MarginLeft.ValueOrNull();
        currentMargin.ShouldNotBeNull();

        var rebuilt = new DivElement { Id = "bar", Style = MakeAnimatedStyle("slide") };
        SetSupersededBy(bar, rebuilt);
        root.RemoveChild(bar);
        root.AddChild(rebuilt);

        Element.BumpMutationVersion();
        engine.Render(canvas);

        // The first rendered replacement frame must keep the current visual value instead of
        // briefly drawing the animation's base/start state until the next Update call.
        var rebuiltLayout = GetLayoutBox(rebuilt);
        rebuiltLayout.ComputedStyle.MarginLeft.Value.ShouldBe(currentMargin.Value.Value, 0.001f);
    }

    [Fact]
    public void ImperativeAnimation_ShouldSurviveRender_WhenNotDeclaredInStyle()
    {
        var root = new DivElement { Id = "root" };
        var spinner = new DivElement
        {
            Id = "spinner",
            Style = new Style { Width = Length.Px(30), Height = Length.Px(30) }
        };
        root.AddChild(spinner);

        var engine = CreateEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { new StyleSheet() }, canvas, 800, 600);
        engine.Render(canvas);

        var animation = new KeyframeAnimation("imperative-spin", 2f,
            new Keyframe(0f, new Style { Opacity = 0f }),
            new Keyframe(1f, new Style { Opacity = 1f }))
        {
            Infinite = true,
            TimingFunction = TimingFunction.Linear,
        };
        engine.StartAnimation(spinner, animation);

        // Imperative animations are not present in Style.Animations, but must still be owned by
        // the animation manager until explicitly stopped or completed.
        engine.Render(canvas);

        GetActiveAnimationCount(engine.AnimationManager).ShouldBe(1);
    }

    [Fact]
    public void AnimationDefinition_ShouldUpdate_WhenSameNamedAnimationChangesAfterReRender()
    {
        var root = new DivElement { Id = "root" };
        var spinner = new DivElement
        {
            Id = "spinner",
            Style = MakeAnimatedStyle("spin", duration: 2f, playState: AnimationPlayState.Running)
        };
        root.AddChild(spinner);

        var engine = CreateEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { new StyleSheet() }, canvas, 800, 600);

        var rebuilt = new DivElement
        {
            Id = "spinner",
            Style = MakeAnimatedStyle("spin", duration: 4f, playState: AnimationPlayState.Paused)
        };
        SetSupersededBy(spinner, rebuilt);
        root.RemoveChild(spinner);
        root.AddChild(rebuilt);

        Element.BumpMutationVersion();
        engine.Render(canvas);

        var activeDefinition = GetAnimationDefinition(engine.AnimationManager, 0);
        activeDefinition.Duration.ShouldBe(4f);
        activeDefinition.PlayState.ShouldBe(AnimationPlayState.Paused);
    }

    [Fact]
    public void DeclarativeAnimation_ShouldStop_WhenRemovedFromStyleDuringReRender()
    {
        var root = new DivElement { Id = "root" };
        var skeleton = new DivElement { Id = "skeleton", Style = MakeAnimatedStyle("shimmer") };
        root.AddChild(skeleton);

        var engine = CreateEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { new StyleSheet() }, canvas, 800, 600);
        GetActiveAnimationCount(engine.AnimationManager).ShouldBe(1);

        var rebuilt = new DivElement
        {
            Id = "skeleton",
            Style = new Style { Width = Length.Px(30), Height = Length.Px(30) }
        };
        SetSupersededBy(skeleton, rebuilt);
        root.RemoveChild(skeleton);
        root.AddChild(rebuilt);

        Element.BumpMutationVersion();
        engine.Render(canvas);

        GetActiveAnimationCount(engine.AnimationManager).ShouldBe(0);
        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();
    }

    [Fact]
    public void RemovingDeclarativeAnimation_ShouldRestoreReplacementStyleValue()
    {
        var root = new DivElement { Id = "root" };
        var animated = new DivElement { Id = "animated", Style = MakeAnimatedStyle("shimmer") };
        root.AddChild(animated);

        var engine = CreateEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { new StyleSheet() }, canvas, 800, 600);
        engine.AnimationManager.Update(0.5f);

        var rebuilt = new DivElement
        {
            Id = "animated",
            Style = new Style
            {
                Width = Length.Px(30),
                Height = Length.Px(30),
                MarginLeft = Length.Px(7)
            }
        };
        SetSupersededBy(animated, rebuilt);
        root.RemoveChild(animated);
        root.AddChild(rebuilt);

        Element.BumpMutationVersion();
        engine.Render(canvas);

        // Removing the animation must not leave the last animated inline value (42.5px) overriding
        // the replacement component's own declared margin.
        GetLayoutBox(rebuilt).ComputedStyle.MarginLeft.Value.ShouldBe(7f, 0.001f);
    }

    [Fact]
    public void ActiveTransition_ShouldNotBeDuplicated_WhenElementIsReRendered()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject
        {
            [".box"] = new CssObject
            {
                Width = Length.Px(100),
                Height = Length.Px(50),
                Opacity = 1f,
                Transitions = new List<Transition>
                {
                    Transition.For(x => x.Opacity).Duration(1f).Linear()
                }
            },
            [".box.faded"] = new CssObject { Opacity = 0f }
        });

        var root = new DivElement
        {
            Id = "root",
            Style = new Style { Width = Length.Px(500), Height = Length.Px(500) }
        };
        var box = new DivElement { Id = "box", Class = "box" };
        root.AddChild(box);

        var engine = CreateEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);
        box.Class = "box faded";
        engine.Render(canvas);
        engine.AnimationManager.ActiveTransitionCount.ShouldBe(1);
        engine.AnimationManager.Update(0.25f);

        var rebuilt = new DivElement { Id = "box", Class = "box faded" };
        TransferLayoutBox(box, rebuilt);
        SetSupersededBy(box, rebuilt);
        root.RemoveChild(box);
        root.AddChild(rebuilt);

        Element.BumpMutationVersion();
        engine.Render(canvas);

        engine.AnimationManager.ActiveTransitionCount.ShouldBe(1);
    }

    [Fact]
    public void ActiveTransition_ShouldReapplyCurrentValue_OnReplacementFirstFrame()
    {
        var styleSheet = new StyleSheet();
        styleSheet.Add(new CssObject
        {
            [".box"] = new CssObject
            {
                Width = Length.Px(100),
                Height = Length.Px(50),
                Opacity = 1f,
                Transitions = new List<Transition>
                {
                    Transition.For(x => x.Opacity).Duration(1f).Linear()
                }
            },
            [".box.faded"] = new CssObject { Opacity = 0f }
        });

        var root = new DivElement
        {
            Id = "root",
            Style = new Style { Width = Length.Px(500), Height = Length.Px(500) }
        };
        var box = new DivElement { Id = "box", Class = "box" };
        root.AddChild(box);

        var engine = CreateEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { styleSheet }, canvas, 800, 600);
        box.Class = "box faded";
        engine.Render(canvas);
        engine.AnimationManager.Update(0.25f);
        engine.Render(canvas); // Commit the current 0.75 value to the old layout tree.

        var rebuilt = new DivElement { Id = "box", Class = "box faded" };
        TransferLayoutBox(box, rebuilt);
        SetSupersededBy(box, rebuilt);
        root.RemoveChild(box);
        root.AddChild(rebuilt);

        Element.BumpMutationVersion();
        engine.Render(canvas);

        // The migrated transition must keep its current interpolation value, rather than letting
        // the replacement's first layout jump directly to the target opacity of 0.
        GetLayoutBox(rebuilt).ComputedStyle.Opacity.ShouldBe(0.75f, 0.001f);
    }

    /// <summary>
    /// ISSUE-127 伴生缺陷：此前没有任何「元素已脱离 DOM」的清理，被移除元素的动画条目会永久
    /// 留存，继续每帧写入孤儿树并递增变更版本号——既泄漏，又让引擎永远无法进入空闲态。
    /// </summary>
    [Fact]
    public void Animations_OnElementsRemovedFromDom_ShouldBePruned()
    {
        var root = new DivElement { Id = "root" };
        var spinner = new DivElement { Id = "spinner", Style = MakeAnimatedStyle("spin") };
        root.AddChild(spinner);

        var engine = CreateEngine();
        using var surface = SKSurface.Create(new SKImageInfo(800, 600));
        var canvas = surface.Canvas;

        engine.Initialize(root, new List<StyleSheet> { new StyleSheet() }, canvas, 800, 600);
        engine.Render(canvas);
        GetActiveAnimationCount(engine.AnimationManager).ShouldBe(1);

        // 元素真正离开 DOM（没有 SupersededBy 转发）——条目应被回收。
        root.RemoveChild(spinner);
        Element.BumpMutationVersion();
        engine.Render(canvas);

        GetActiveAnimationCount(engine.AnimationManager).ShouldBe(0);
        engine.AnimationManager.HasActiveAnimations.ShouldBeFalse();
    }

    private static Style MakeAnimatedStyle(
        string name,
        float duration = 2f,
        AnimationPlayState playState = AnimationPlayState.Running) => new()
    {
        Width = Length.Px(30),
        Height = Length.Px(30),
        Animations = new List<KeyframeAnimation>
        {
            new KeyframeAnimation
            {
                Name = name,
                Duration = duration,
                Infinite = true,
                TimingFunction = TimingFunction.Linear,
                PlayState = playState,
                Keyframes = new List<Keyframe>
                {
                    new Keyframe(0f, new Style { MarginLeft = Length.Px(0) }),
                    new Keyframe(1f, new Style { MarginLeft = Length.Px(170) })
                }
            }
        }
    };

    // SupersededBy 是 internal（Miko.Core），测试程序集经反射写入，模拟
    // ComponentBase.TransferRuntimeState 在重渲染时留下的转发指针。
    private static void SetSupersededBy(Element oldElement, Element newElement)
    {
        var prop = typeof(Element).GetProperty("SupersededBy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prop.ShouldNotBeNull();
        prop!.SetValue(oldElement, newElement);
    }

    private static void TransferLayoutBox(Element oldElement, Element newElement)
    {
        var prop = typeof(Element).GetProperty("LayoutBox",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prop.ShouldNotBeNull();

        var oldLayout = GetLayoutBox(oldElement);
        prop.SetValue(newElement, new LayoutBox
        {
            Element = newElement,
            ComputedStyle = oldLayout.ComputedStyle,
            Children = oldLayout.Children
        });
    }

    private static LayoutBox GetLayoutBox(Element element)
    {
        var prop = typeof(Element).GetProperty("LayoutBox",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        prop.ShouldNotBeNull();

        var layout = (LayoutBox?)prop!.GetValue(element);
        layout.ShouldNotBeNull();
        return layout!;
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

    // ActiveAnimation 是 internal，按名字反射取其字段值。
    private static object GetActiveAnimation(AnimationManager manager, int index)
    {
        var field = typeof(AnimationManager).GetField("_animations",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = (System.Collections.IList)field!.GetValue(manager)!;
        return list[index]!;
    }

    private static float GetElapsedTime(AnimationManager manager, int index)
    {
        var anim = GetActiveAnimation(manager, index);
        return (float)anim.GetType().GetProperty("ElapsedTime")!.GetValue(anim)!;
    }

    private static Element GetAnimationElement(AnimationManager manager, int index)
    {
        var anim = GetActiveAnimation(manager, index);
        return (Element)anim.GetType().GetProperty("Element")!.GetValue(anim)!;
    }

    private static KeyframeAnimation GetAnimationDefinition(AnimationManager manager, int index)
    {
        var anim = GetActiveAnimation(manager, index);
        return (KeyframeAnimation)anim.GetType().GetProperty("Definition")!.GetValue(anim)!;
    }
}
