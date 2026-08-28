using BenchmarkDotNet.Attributes;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using SkiaSharp;

namespace Miko.Benchmarks.Benchmarks;

/// <summary>
/// Measures component-driven layout rather than a hand-built DOM. The page is
/// composed through RenderTreeBuilder/OpenComponent, and the state-change case
/// calls the same StateHasChanged path used by component event handlers.
/// </summary>
[MemoryDiagnoser]
public class ComponentFrameBenchmarks
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly RenderEngine _renderEngine = new();
    private SKSurface _surface = null!;
    private ComponentBenchmarkPage _page = null!;
    private Element _root = null!;
    private List<StyleSheet> _styles = null!;

    [Params(20, 100, 300)]
    public int ComponentCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _surface = SKSurface.Create(new SKImageInfo(800, 5000));
        _renderEngine.SetCanvas(_surface.Canvas);
        _styles =
        [
            new StyleSheet
            {
                Rules =
                [
                    new StyleRule
                    {
                        Selector = new Miko.Styling.Selectors.ClassSelector("component-page"),
                        Style = new Style { Display = Display.Block, Width = Length.Px(800) }
                    },
                    new StyleRule
                    {
                        Selector = new Miko.Styling.Selectors.ClassSelector("component-item"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(760),
                            Height = Length.Px(28),
                            MarginBottom = Length.Px(6),
                            Padding = Length.Px(4)
                        }
                    }
                ]
            }
        ];

        _page = new ComponentBenchmarkPage { ItemCount = ComponentCount };
        _root = _page.Build();
        _layoutEngine.InvalidateCache();
        _layoutEngine.Layout(_root, _styles, 800, 5000);
    }

    [GlobalCleanup]
    public void Cleanup() => _surface.Dispose();

    [Benchmark(Baseline = true, Description = "Component initial Build + layout + render")]
    public void InitialComponentFrame()
    {
        var page = new ComponentBenchmarkPage { ItemCount = ComponentCount };
        var root = page.Build();
        _layoutEngine.InvalidateCache();
        var layout = _layoutEngine.Layout(root, _styles, 800, 5000);
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(layout);
    }

    [Benchmark(Description = "Component StateHasChanged + layout + render")]
    public void StateChange_Relayout()
    {
        _page.IncrementAndRender();
        _root = _page.RootElement;
        _layoutEngine.InvalidateCache();
        var layout = _layoutEngine.Layout(_root, _styles, 800, 5000);
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(layout);
    }

    [Benchmark(Description = "Component state-only change (cached layout + render)")]
    public void StateChange_NoVisualWork()
    {
        _page.MutateStateWithoutRender();
        var layout = _layoutEngine.Layout(_root, _styles, 800, 5000);
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(layout);
    }

    private sealed class ComponentBenchmarkPage : ComponentBase
    {
        public int ItemCount { get; set; }
        public Element RootElement { get; private set; } = null!;
        private int _renderedState;
        private int _internalState;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "component-page");
            for (int i = 0; i < ItemCount; i++)
            {
                builder.OpenComponent<ComponentBenchmarkItem>(10 + i * 3);
                builder.AddAttribute(11 + i * 3, nameof(ComponentBenchmarkItem.Index), i);
                builder.AddAttribute(12 + i * 3, nameof(ComponentBenchmarkItem.State), _renderedState);
                builder.CloseComponent();
            }
            builder.CloseElement();
        }

        public override Element Build()
        {
            RootElement = base.Build();
            return RootElement;
        }

        public void IncrementAndRender()
        {
            _renderedState++;
            StateHasChanged();
            RootElement = RootElement;
        }

        public void MutateStateWithoutRender() => _internalState++;
    }

    private sealed class ComponentBenchmarkItem : ComponentBase
    {
        public int Index { get; set; }
        public int State { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "component-item");
            builder.AddContent(2, $"Component item {Index}, state {State}");
            builder.CloseElement();
        }
    }
}
