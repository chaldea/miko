using BenchmarkDotNet.Attributes;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Rendering;
using Miko.Styling;
using Miko.Styling.Selectors;
using SkiaSharp;

namespace Miko.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class TableBenchmarks
{
    private readonly LayoutEngine _layoutEngine = new();
    private readonly RenderEngine _renderEngine = new();

    private TableElement _table10K = null!;
    private TableElement _table100K = null!;
    private TableElement _table1M = null!;
    private List<StyleSheet> _tableStyles = null!;
    private SKSurface _surface = null!;

    [GlobalSetup]
    public void Setup()
    {
        _table10K = CreateTable(10_000, 5);
        _table100K = CreateTable(100_000, 5);
        _table1M = CreateTable(1_000_000, 5);
        _tableStyles = CreateTableStyleSheet();
        _surface = SKSurface.Create(new SKImageInfo(1200, 800));
        _renderEngine.SetCanvas(_surface.Canvas);
    }

    [GlobalCleanup]
    public void Cleanup() => _surface.Dispose();

    [Benchmark(Description = "Layout: 1万行 x 5列 (5万单元格)")]
    public LayoutBox Layout_10K_Rows()
        => _layoutEngine.Layout(_table10K, _tableStyles, 1200, 10_000 * 30);

    [Benchmark(Description = "Layout: 10万行 x 5列 (50万单元格)")]
    public LayoutBox Layout_100K_Rows()
        => _layoutEngine.Layout(_table100K, _tableStyles, 1200, 100_000 * 30);

    [Benchmark(Description = "Layout: 100万行 x 5列 (500万单元格)")]
    public LayoutBox Layout_1M_Rows()
        => _layoutEngine.Layout(_table1M, _tableStyles, 1200, 1_000_000 * 30);

    [Benchmark(Description = "Render: 1万行 x 5列")]
    public void Render_10K_Rows()
    {
        var layout = _layoutEngine.Layout(_table10K, _tableStyles, 1200, 10_000 * 30);
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(layout);
    }

    [Benchmark(Description = "Render: 10万行 x 5列")]
    public void Render_100K_Rows()
    {
        var layout = _layoutEngine.Layout(_table100K, _tableStyles, 1200, 100_000 * 30);
        _surface.Canvas.Clear(SKColors.White);
        _renderEngine.Render(layout);
    }

    private static TableElement CreateTable(int rowCount, int colCount)
    {
        var table = new TableElement { Id = "data-table", Class = "table" };
        var thead = new TheadElement();
        var headerRow = new TrElement { Class = "header-row" };
        for (int c = 0; c < colCount; c++)
            headerRow.AddChild(new ThElement { TextContent = $"Col {c}" });
        thead.AddChild(headerRow);
        table.AddChild(thead);

        var tbody = new TbodyElement();
        for (int r = 0; r < rowCount; r++)
        {
            var row = new TrElement { Class = r % 2 == 0 ? "row-even" : "row-odd" };
            for (int c = 0; c < colCount; c++)
                row.AddChild(new TdElement { TextContent = $"R{r}C{c}" });
            tbody.AddChild(row);
        }
        table.AddChild(tbody);

        return table;
    }

    private static List<StyleSheet> CreateTableStyleSheet()
    {
        return
        [
            new StyleSheet
            {
                Rules =
                [
                    new StyleRule
                    {
                        Selector = new ClassSelector("table"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(1000)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new TagSelector("thead"),
                        Style = new Style { Display = Display.Block }
                    },
                    new StyleRule
                    {
                        Selector = new TagSelector("tbody"),
                        Style = new Style { Display = Display.Block }
                    },
                    new StyleRule
                    {
                        Selector = new TagSelector("tr"),
                        Style = new Style
                        {
                            Display = Display.Flex,
                            FlexDirection = FlexDirection.Row,
                            Height = Length.Px(28)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new TagSelector("th"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(200),
                            Padding = Length.Px(4)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new TagSelector("td"),
                        Style = new Style
                        {
                            Display = Display.Block,
                            Width = Length.Px(200),
                            Padding = Length.Px(4)
                        }
                    },
                    new StyleRule
                    {
                        Selector = new ClassSelector("row-even"),
                        Style = new Style
                        {
                            BackgroundColor = Color.FromHex("f8f9fa")
                        }
                    }
                ]
            }
        ];
    }
}