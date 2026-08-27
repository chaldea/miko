using BenchmarkDotNet.Attributes;
using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Miko.Styling.Selectors;
using SkiaSharp;

namespace Miko.Benchmarks.Benchmarks;

/// <summary>
/// Measures the cost of several simultaneous CSS-like animations during a frame.
/// Each animated frame advances keyframes, invalidates layout through the mutation
/// tracker, rebuilds the layout tree, and paints the result to an off-screen surface.
/// </summary>
[MemoryDiagnoser]
public class AnimationFrameBenchmarks
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly RenderEngine _renderEngine = new();
    private readonly AnimationManager _animationManager = new();

    private SKSurface _surface = null!;
    private Element _page = null!;
    private List<StyleSheet> _styles = null!;
    private List<Element> _animatedElements = null!;
    private KeyframeAnimation _animation = null!;

    [Params(10, 50, 100)]
    public int AnimatedElementCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(800, 5000));
        _renderEngine.SetCanvas(_surface.Canvas);

        _page = new DivElement { Class = "animated-page" };
        _animatedElements = [];
        for (int i = 0; i < AnimatedElementCount; i++)
        {
            var element = new DivElement
            {
                Class = "animated-item",
                TextContent = $"Animated item {i}"
            };
            _page.AddChild(element);
            _animatedElements.Add(element);
        }

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

        _animation = new KeyframeAnimation(
            "benchmark-pulse",
            1f,
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
        };

        foreach (var element in _animatedElements)
            _animationManager.StartAnimation(element, _animation);

        _layoutEngine.InvalidateCache();
        _layoutEngine.Layout(_page, _styles, 800, 5000);
    }

    [GlobalCleanup]
    public void Cleanup() => _surface.Dispose();

    [Benchmark(Baseline = true, Description = "Static frame (layout + render)")]
    public void StaticFrame()
    {
        _layoutEngine.InvalidateCache();
        var layout = _layoutEngine.Layout(_page, _styles, 800, 5000);
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(layout);
    }

    [Benchmark(Description = "Animated frame (update + layout + render)")]
    public void AnimatedFrame()
    {
        _animationManager.Update(1f / 60f);
        _layoutEngine.InvalidateCache();
        var layout = _layoutEngine.Layout(_page, _styles, 800, 5000);
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(layout);
    }

    [Benchmark(Description = "Animation update only")]
    public void AnimationUpdateOnly() => _animationManager.Update(1f / 60f);
}
