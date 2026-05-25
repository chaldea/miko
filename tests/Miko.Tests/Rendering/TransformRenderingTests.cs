using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Rendering;

public class TransformRenderingTests : IDisposable
{
    private readonly SKBitmap _canvasBitmap;
    private readonly SKCanvas _canvas;
    private readonly RenderEngine _renderEngine;

    public TransformRenderingTests()
    {
        _canvasBitmap = new SKBitmap(200, 200);
        _canvas = new SKCanvas(_canvasBitmap);
        _canvas.Clear(SKColors.White);
        _renderEngine = new RenderEngine();
        _renderEngine.SetCanvas(_canvas);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _canvasBitmap.Dispose();
    }

    [Fact]
    public void Rotate180_ShouldFlipElement()
    {
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(100),
            Height = Length.Px(100),
            BackgroundColor = Color.Red,
            Transform = Transform.FromRotate(180),
        };

        RenderElement(root);

        // A 100x100 red box at (0,0) rotated 180 degrees around its center (50,50)
        // maps the box to (-100,-100)-(0,0) relative to center, so it stays at (0,0)-(100,100)
        // but the pixel at (10,10) in the original maps to (90,90) after rotation
        // The center pixel should still be red
        GetPixelColor(50, 50).ShouldBe(SKColors.Red);
    }

    [Fact]
    public void Scale2x_ShouldEnlargeElement()
    {
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(200),
            Height = Length.Px(200),
        };

        var child = new DivElement();
        child.Style = new Style
        {
            Width = Length.Px(40),
            Height = Length.Px(40),
            BackgroundColor = Color.Blue,
            Transform = Transform.FromScale(2f, 2f),
            TransformOrigin = TransformOrigin.TopLeft,
        };
        root.AddChild(child);

        RenderElement(root);

        // Scaled 2x from top-left: 40x40 becomes 80x80
        GetPixelColor(10, 10).ShouldBe(SKColors.Blue);
        GetPixelColor(70, 70).ShouldBe(SKColors.Blue);
    }

    [Fact]
    public void TranslateTransform_ShouldMoveElement()
    {
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(200),
            Height = Length.Px(200),
        };

        var child = new DivElement();
        child.Style = new Style
        {
            Width = Length.Px(40),
            Height = Length.Px(40),
            BackgroundColor = Color.Blue,
            Transform = Transform.FromTranslate(Length.Px(50), Length.Px(50)),
            TransformOrigin = TransformOrigin.TopLeft,
        };
        root.AddChild(child);

        RenderElement(root);

        // Element at (0,0) translated by (50,50) => drawn at (50,50)
        GetPixelColor(60, 60).ShouldBe(SKColors.Blue);
        // Original position should be empty (white)
        GetPixelColor(5, 5).ShouldBe(SKColors.White);
    }

    [Fact]
    public void NoTransform_ShouldRenderNormally()
    {
        var root = new DivElement();
        root.Style = new Style
        {
            Width = Length.Px(100),
            Height = Length.Px(100),
            BackgroundColor = Color.Red,
        };

        RenderElement(root);

        GetPixelColor(50, 50).ShouldBe(SKColors.Red);
    }

    private void RenderElement(Element root)
    {
        var engine = new MikoEngine();
        engine.Initialize(root, [], _canvas, 200, 200);
        engine.Render(_canvas);
    }

    [Fact]
    public void PseudoElement_Transition_ShouldTriggerOnTransformChange()
    {
        var root = new DivElement { Class = "panel" };
        root.Style = new Style
        {
            Width = Length.Px(100),
            Height = Length.Px(100),
        };

        var afterStyle = new Style
        {
            Width = Length.Px(20),
            Height = Length.Px(20),
            BackgroundColor = Color.Red,
            Transitions = [Transition.For(nameof(Style.Transform), 1f, TimingFunction.Linear)],
        };

        var styleSheet = new StyleSheet();
        styleSheet.PseudoElementRules.Add(new PseudoElementRule
        {
            Selector = new Miko.Styling.Selectors.ClassSelector("panel"),
            Type = PseudoElementType.After,
            Style = afterStyle
        });

        var engine = new MikoEngine();
        engine.Initialize(root, [styleSheet], _canvas, 200, 200);

        // Now mutate the style to add Transform
        afterStyle.Transform = Transform.FromRotate(-180);

        root.IsDirty = true;
        engine.Render(_canvas);

        // Transition should have been triggered
        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue();

        // After half the duration, the transform should be interpolated
        engine.AnimationManager.Update(0.5f);

        root.PseudoElementStyles.ShouldNotBeNull();
        root.PseudoElementStyles!.ContainsKey(PseudoElementType.After).ShouldBeTrue();
        var pseudoStyle = root.PseudoElementStyles[PseudoElementType.After];
        pseudoStyle.Transform.ShouldNotBeNull();
        pseudoStyle.Transform!.Functions.Count.ShouldBeGreaterThan(0);
        var rotate = pseudoStyle.Transform.Functions[0]
            .ShouldBeOfType<TransformFunction.Rotate>();
        rotate.Degrees.ShouldBe(-90f, 1f);
    }

    [Fact]
    public void PseudoElement_Transition_DirectApplier_ShouldWork()
    {
        var manager = new AnimationManager();
        var parent = new DivElement { Style = new Style() };
        var transition = Transition.For(nameof(Style.Transform), 1f, TimingFunction.Linear);
        var from = Transform.None;
        var to = Transform.FromRotate(-180f);

        manager.TrackTransformChangeWithApplier(parent, "::pseudo(After).Transform", from, to, transition,
            (e, t) =>
            {
                e.PseudoElementStyles ??= new();
                if (!e.PseudoElementStyles.TryGetValue(PseudoElementType.After, out var s))
                {
                    s = new Style();
                    e.PseudoElementStyles[PseudoElementType.After] = s;
                }
                s.Transform = t;
            });

        manager.HasActiveAnimations.ShouldBeTrue();

        manager.Update(0.5f);

        parent.PseudoElementStyles.ShouldNotBeNull();
        var style = parent.PseudoElementStyles![PseudoElementType.After];
        style.Transform.ShouldNotBeNull();
        style.Transform!.Functions.Count.ShouldBe(1);
        var r = style.Transform.Functions[0].ShouldBeOfType<TransformFunction.Rotate>();
        r.Degrees.ShouldBe(-90f, 1f);
    }

    [Fact]
    public void PseudoElement_Transition_ShouldTriggerOnReinitialize()
    {
        // Simulates Razor flow: Initialize is called with a new DOM tree on each StateHasChanged
        var styleSheet = new StyleSheet();
        styleSheet.PseudoElementRules.Add(new PseudoElementRule
        {
            Selector = new Miko.Styling.Selectors.ClassSelector("panel"),
            Type = PseudoElementType.After,
            Style = new Style
            {
                Width = Length.Px(20),
                Height = Length.Px(20),
                BackgroundColor = Color.Red,
                Transitions = [Transition.For(nameof(Style.Transform), 1f, TimingFunction.Linear)],
            }
        });
        styleSheet.PseudoElementRules.Add(new PseudoElementRule
        {
            Selector = new Miko.Styling.Selectors.ClassSelector("open"),
            Type = PseudoElementType.After,
            Style = new Style
            {
                Transform = Transform.FromRotate(-180),
            }
        });

        // First Initialize: class="panel" (no .open, so no Transform)
        var root1 = new DivElement { Class = "panel" };
        root1.Style = new Style { Width = Length.Px(100), Height = Length.Px(100) };

        var engine = new MikoEngine();
        engine.Initialize(root1, [styleSheet], _canvas, 200, 200);

        // Second Initialize: class="panel open" (now .open matches, Transform applied)
        var root2 = new DivElement { Class = "panel open" };
        root2.Style = new Style { Width = Length.Px(100), Height = Length.Px(100) };

        engine.Initialize(root2, [styleSheet], _canvas, 200, 200);

        // Transition should have been triggered
        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue();
        engine.AnimationManager.ActiveTransitionCount.ShouldBeGreaterThan(0);

        // After half the duration
        engine.AnimationManager.Update(0.5f);

        root2.PseudoElementStyles.ShouldNotBeNull();
        root2.PseudoElementStyles!.ContainsKey(PseudoElementType.After).ShouldBeTrue();
        var pseudoStyle = root2.PseudoElementStyles[PseudoElementType.After];
        pseudoStyle.Transform.ShouldNotBeNull();
        pseudoStyle.Transform!.Functions.Count.ShouldBeGreaterThan(0);
        var rotate = pseudoStyle.Transform.Functions[0]
            .ShouldBeOfType<TransformFunction.Rotate>();
        rotate.Degrees.ShouldBe(-90f, 1f);
    }

    [Fact]
    public void PseudoElement_Transition_ShouldTriggerOnClassChange()
    {
        // Simulates StateHasChanged flow: DOM is mutated in place, then Render is called
        var styleSheet = new StyleSheet();
        styleSheet.PseudoElementRules.Add(new PseudoElementRule
        {
            Selector = new Miko.Styling.Selectors.ClassSelector("panel"),
            Type = PseudoElementType.After,
            Style = new Style
            {
                Width = Length.Px(20),
                Height = Length.Px(20),
                Transitions = [Transition.For(nameof(Style.Transform), 1f, TimingFunction.Linear)],
            }
        });
        styleSheet.PseudoElementRules.Add(new PseudoElementRule
        {
            Selector = new Miko.Styling.Selectors.ClassSelector("open"),
            Type = PseudoElementType.After,
            Style = new Style
            {
                Transform = Transform.FromRotate(-180),
            }
        });

        var panel = new DivElement { Class = "panel" };
        panel.Style = new Style { Width = Length.Px(100), Height = Length.Px(100) };
        var root = new DivElement();
        root.Style = new Style { Width = Length.Px(200), Height = Length.Px(200) };
        root.AddChild(panel);

        var engine = new MikoEngine();
        engine.Initialize(root, [styleSheet], _canvas, 200, 200);

        // Simulate StateHasChanged: change class in place
        panel.Class = "panel open";
        panel.IsDirty = true;

        engine.Render(_canvas);

        // Transition should have been triggered
        engine.AnimationManager.HasActiveAnimations.ShouldBeTrue();

        engine.AnimationManager.Update(0.5f);

        panel.PseudoElementStyles.ShouldNotBeNull();
        var pseudoStyle = panel.PseudoElementStyles![PseudoElementType.After];
        pseudoStyle.Transform.ShouldNotBeNull();
        pseudoStyle.Transform!.Functions.Count.ShouldBeGreaterThan(0);
        var rotate = pseudoStyle.Transform.Functions[0]
            .ShouldBeOfType<TransformFunction.Rotate>();
        rotate.Degrees.ShouldBe(-90f, 1f);
    }

    private SKColor GetPixelColor(int x, int y) => _canvasBitmap.GetPixel(x, y);
}
