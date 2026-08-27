using BenchmarkDotNet.Attributes;
using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Miko.Styling.Selectors;
using SkiaSharp;

namespace Miko.Benchmarks.Benchmarks;

/// <summary>
/// Isolates the per-property cost of an animated frame through <see cref="MikoEngine.Tick"/>.
/// The ISSUE-130 report attributed the animated-frame blowup to layout invalidation, but that
/// benchmark forced <c>InvalidateCache()</c> on both arms, so layout cost was constant and could
/// not have been the delta. These arms separate the candidates: <c>Transform</c> and
/// <c>BackgroundColor</c> are paint-only, <c>MarginLeft</c> is a layout input, and
/// <c>Opacity</c> is paint-only but forces the renderer into <c>SaveLayerAlpha</c>.
/// </summary>
[MemoryDiagnoser]
public class AnimationPropertyCostBenchmarks
{
    public enum AnimatedProp { Transform, Opacity, BackgroundColor, MarginLeft }

    private MikoEngine _engine = null!;
    private SKSurface _surface = null!;
    private Element _page = null!;
    private List<StyleSheet> _styles = null!;

    [Params(50)]
    public int AnimatedElementCount { get; set; }

    [Params(AnimatedProp.Transform, AnimatedProp.Opacity, AnimatedProp.BackgroundColor, AnimatedProp.MarginLeft)]
    public AnimatedProp Property { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(800, 5000));
        _styles =
        [
            new StyleSheet
            {
                Rules =
                [
                    new StyleRule
                    {
                        Selector = new ClassSelector("animated-page"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(800) }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("animated-item"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(760),
                            Height = Length.Px(32),
                            MarginBottom = Length.Px(8),
                            BackgroundColor = Color.FromRgb(45, 125, 190)
                        }
                    }
                ]
            }
        ];

        var animation = BuildAnimation();
        _page = new DivElement { Class = "animated-page" };
        for (int i = 0; i < AnimatedElementCount; i++)
        {
            _page.AddChild(new DivElement
            {
                Class = "animated-item",
                TextContent = $"Animated item {i}",
                Style = new Style { Animations = new List<KeyframeAnimation> { animation } }
            });
        }

        _engine = new MikoEngineBuilder().Build();
        _engine.Initialize(_page, _styles, _surface.Canvas, 800, 5000);
        _engine.Render(_surface.Canvas);
    }

    [GlobalCleanup]
    public void Cleanup() => _surface.Dispose();

    private KeyframeAnimation BuildAnimation()
    {
        (Style from, Style to) = Property switch
        {
            AnimatedProp.Transform => (
                new Style { Transform = Transform.FromTranslate(Length.Px(0), Length.Px(0)) },
                new Style { Transform = Transform.FromTranslate(Length.Px(18), Length.Px(0)) }),
            AnimatedProp.Opacity => (
                new Style { Opacity = 0.35f },
                new Style { Opacity = 1f }),
            AnimatedProp.BackgroundColor => (
                new Style { BackgroundColor = Color.FromRgb(45, 125, 190) },
                new Style { BackgroundColor = Color.FromRgb(190, 80, 45) }),
            _ => (
                new Style { MarginLeft = Length.Px(0) },
                new Style { MarginLeft = Length.Px(18) })
        };

        return new KeyframeAnimation($"pulse-{Property}", 1f, new Keyframe(0f, from), new Keyframe(1f, to))
        {
            Infinite = true,
            TimingFunction = TimingFunction.EaseInOut
        };
    }

    [Benchmark(Description = "Engine animated frame (Tick)")]
    public void AnimatedTick() => _engine.Tick(1f / 60f, _surface.Canvas);
}
