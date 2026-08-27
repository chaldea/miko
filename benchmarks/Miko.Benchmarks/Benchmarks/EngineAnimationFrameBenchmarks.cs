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
/// Drives animation frames through <see cref="MikoEngine.Tick"/>, i.e. the same entry point the
/// platform hosts use. Unlike <see cref="AnimationFrameBenchmarks"/> — which calls
/// LayoutEngine/RenderEngine directly and forces <c>InvalidateCache()</c> on every arm — this
/// benchmark lets the engine decide whether a frame needs layout. That distinction is the whole
/// point of the ISSUE-131 paint-only path, so only this shape can measure it.
/// </summary>
[MemoryDiagnoser]
public class EngineAnimationFrameBenchmarks
{
    private MikoEngine _engine = null!;
    private SKSurface _surface = null!;
    private Element _page = null!;
    private List<StyleSheet> _styles = null!;

    [Params(10, 50, 100)]
    public int AnimatedElementCount { get; set; }

    /// <summary>
    /// When true the animated properties are transform/opacity (paint-only); when false the
    /// animation drives <c>margin-left</c>, which is a genuine layout input.
    /// </summary>
    [Params(true, false)]
    public bool PaintOnly { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(800, 5000));
        _styles = BuildStyles();
        _page = BuildPage();
        _engine = new MikoEngineBuilder().Build();
        _engine.Initialize(_page, _styles, _surface.Canvas, 800, 5000);
        _engine.Render(_surface.Canvas);
    }

    [GlobalCleanup]
    public void Cleanup() => _surface.Dispose();

    private KeyframeAnimation BuildAnimation() => PaintOnly
        ? new KeyframeAnimation("paint-pulse", 1f,
            new Keyframe(0f, new Style
            {
                Opacity = 0.35f,
                Transform = Transform.FromTranslate(Length.Px(0), Length.Px(0))
            }),
            new Keyframe(1f, new Style
            {
                Opacity = 1f,
                Transform = Transform.FromTranslate(Length.Px(18), Length.Px(0))
            }))
        {
            Infinite = true,
            TimingFunction = TimingFunction.EaseInOut
        }
        : new KeyframeAnimation("layout-pulse", 1f,
            new Keyframe(0f, new Style { MarginLeft = Length.Px(0) }),
            new Keyframe(1f, new Style { MarginLeft = Length.Px(18) }))
        {
            Infinite = true,
            TimingFunction = TimingFunction.EaseInOut
        };

    private Element BuildPage()
    {
        var page = new DivElement { Class = "animated-page" };
        var animation = BuildAnimation();
        for (int i = 0; i < AnimatedElementCount; i++)
        {
            page.AddChild(new DivElement
            {
                Class = "animated-item",
                TextContent = $"Animated item {i}",
                Style = new Style { Animations = new List<KeyframeAnimation> { animation } }
            });
        }
        return page;
    }

    private static List<StyleSheet> BuildStyles() =>
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

    /// <summary>One 60 Hz animated frame exactly as a host would submit it.</summary>
    [Benchmark(Description = "Engine animated frame (Tick)")]
    public void AnimatedTick() => _engine.Tick(1f / 60f, _surface.Canvas);
}
