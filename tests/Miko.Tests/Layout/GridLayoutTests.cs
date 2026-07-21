using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Grid 布局测试（ISSUE-097）：轨道尺寸（fixed/%/fr/auto）、显式与自动放置、
/// 子项拉伸/对齐/auto margin、容器行为（gap、justify-content、align-content、
/// 盒模型、min/max、滚动尺寸）等。
/// </summary>
public class GridLayoutTests
{
    private readonly LayoutEngine _layoutEngine = new();

    /// <summary>构造样式表：类名 → 样式。</summary>
    private static List<StyleSheet> Sheets(params (string cls, Style style)[] rules)
        => new()
        {
            new()
            {
                Rules = rules.Select(r => new StyleRule
                {
                    Selector = new ClassSelector(r.cls),
                    Style = r.style
                }).ToList()
            }
        };

    private static DivElement Grid(string cls = "grid") => new() { Class = cls };

    private static DivElement Item(string cls = "it") => new() { Class = cls };

    // ---------- 映射与默认行为 ----------

    [Fact]
    public void Grid_DisplayMapsToGridLayoutType()
    {
        var container = Grid();

        var root = _layoutEngine.Layout(container,
            Sheets(("grid", new Style { Display = Display.Grid })), 800, 600);

        root.Type.ShouldBe(LayoutType.Grid);
        root.ComputedStyle.Display.ShouldBe(Display.Grid);
    }

    [Fact]
    public void Grid_NoTemplate_SingleColumnOfAutoRows()
    {
        // 无模板：单列 auto 行流，子项拉伸填满容器宽度，逐行堆叠。
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));
        container.AddChild(Item("c"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style { Display = Display.Grid }),
            ("a", new Style { Height = Length.Px(20) }),
            ("b", new Style { Height = Length.Px(30) }),
            ("c", new Style { Height = Length.Px(40) })), 800, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[0].BoxModel.Content.Y.ShouldBe(0);
        root.Children[0].BoxModel.Content.Width.ShouldBe(800);
        root.Children[1].BoxModel.Content.Y.ShouldBe(20);
        root.Children[2].BoxModel.Content.Y.ShouldBe(50);
        root.BoxModel.Content.Height.ShouldBe(90);
    }

    [Fact]
    public void Grid_BlockLevel_FillsAvailableWidth()
    {
        var container = Grid();
        container.AddChild(Item("a"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style { Display = Display.Grid }),
            ("a", new Style { Height = Length.Px(20) })), 800, 600);

        root.BoxModel.Content.Width.ShouldBe(800);
    }

    [Fact]
    public void Grid_EmptyContainer_ZeroContentHeight()
    {
        var container = Grid();

        var root = _layoutEngine.Layout(container,
            Sheets(("grid", new Style { Display = Display.Grid })), 800, 600);

        root.BoxModel.Content.Width.ShouldBe(800);
        root.BoxModel.Content.Height.ShouldBe(0);
    }

    // ---------- 轨道尺寸 ----------

    [Fact]
    public void GridTemplateColumns_FixedPx_PositionsItems()
    {
        var container = Grid();
        container.AddChild(Item());
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100), GridTrackSize.Px(200) }
            }),
            ("it", new Style { Height = Length.Px(50) })), 800, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[0].BoxModel.Content.Width.ShouldBe(100);
        root.Children[1].BoxModel.Content.X.ShouldBe(100);
        root.Children[1].BoxModel.Content.Width.ShouldBe(200);
        // 单个 auto 行：取最高子项。
        root.BoxModel.Content.Height.ShouldBe(50);
    }

    [Fact]
    public void GridTemplateColumns_Percent_ResolvesAgainstContentWidth()
    {
        var container = Grid();
        container.AddChild(Item());
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(400),
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Percent(25), GridTrackSize.Percent(75) }
            }),
            ("it", new Style { Height = Length.Px(50) })), 800, 600);

        root.Children[0].BoxModel.Content.Width.ShouldBe(100);
        root.Children[1].BoxModel.Content.X.ShouldBe(100);
        root.Children[1].BoxModel.Content.Width.ShouldBe(300);
    }

    [Fact]
    public void GridTemplateColumns_PercentAgainstIndefiniteWidth_DegradesToAuto()
    {
        // grid 作为 flex 行的 auto 宽度子项（收缩包裹 → 宽度不确定）：
        // 百分比轨道退化为内容尺寸（与 ISSUE-077 的退化哲学一致）。
        var container = new DivElement { Class = "row" };
        var grid = Grid();
        grid.AddChild(Item("a"));
        grid.AddChild(Item("b"));
        container.AddChild(grid);

        var root = _layoutEngine.Layout(container, Sheets(
            ("row", new Style { Display = Display.Flex }),
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Percent(50), GridTrackSize.Px(100) }
            }),
            ("a", new Style { Width = Length.Px(40), Height = Length.Px(20) }),
            ("b", new Style { Height = Length.Px(20) })), 800, 600);

        var box = root.Children[0];
        // 轨道 0 退化为 auto（内容 40），轨道 1 固定 100 → grid 宽 140。
        box.BoxModel.Content.Width.ShouldBe(140);
        box.Children[0].BoxModel.Content.Width.ShouldBe(40);
        box.Children[1].BoxModel.Content.X.ShouldBe(40);
        box.Children[1].BoxModel.Content.Width.ShouldBe(100);
    }

    [Fact]
    public void GridTemplateColumns_Fr_DistributesLeftover()
    {
        var container = Grid();
        for (int i = 0; i < 3; i++) container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(600),
                GridTemplateColumns = new List<GridTrackSize>
                {
                    GridTrackSize.Px(100), GridTrackSize.Fr(1), GridTrackSize.Fr(1)
                }
            }),
            ("it", new Style { Height = Length.Px(50) })), 800, 600);

        root.Children[0].BoxModel.Content.Width.ShouldBe(100);
        // (600 - 100) / 2 = 250
        root.Children[1].BoxModel.Content.X.ShouldBe(100);
        root.Children[1].BoxModel.Content.Width.ShouldBe(250);
        root.Children[2].BoxModel.Content.X.ShouldBe(350);
        root.Children[2].BoxModel.Content.Width.ShouldBe(250);
    }

    [Fact]
    public void GridTemplateColumns_Fr_CombinedWithGap()
    {
        var container = Grid();
        for (int i = 0; i < 3; i++) container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(600),
                ColumnGap = Length.Px(10),
                GridTemplateColumns = new List<GridTrackSize>
                {
                    GridTrackSize.Px(100), GridTrackSize.Fr(1), GridTrackSize.Fr(1)
                }
            }),
            ("it", new Style { Height = Length.Px(50) })), 800, 600);

        // gap 先于 fr 扣除：(600 - 100 - 2×10) / 2 = 240。
        root.Children[1].BoxModel.Content.X.ShouldBe(110);
        root.Children[1].BoxModel.Content.Width.ShouldBe(240);
        root.Children[2].BoxModel.Content.X.ShouldBe(360);
        root.Children[2].BoxModel.Content.Width.ShouldBe(240);
    }

    [Fact]
    public void GridTemplateColumns_AutoTrack_SizedByWidestItem()
    {
        // 模板 [auto, 100px]，4 个子项两行两列：auto 列取该列最宽内容。
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));
        container.AddChild(Item("c"));
        container.AddChild(Item("d"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                // 宽度恰好等于轨道总和（90+100），避免默认模式拉伸 auto 列改变断言基准。
                Width = Length.Px(190),
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Auto, GridTrackSize.Px(100) }
            }),
            ("a", new Style { Width = Length.Px(60), Height = Length.Px(20) }),
            ("b", new Style { Width = Length.Px(10), Height = Length.Px(20) }),
            ("c", new Style { Width = Length.Px(90), Height = Length.Px(20) }),
            ("d", new Style { Width = Length.Px(10), Height = Length.Px(20) })), 800, 600);

        // auto 列宽 = max(60, 90) = 90；第二列从 90 开始。
        root.Children[1].BoxModel.Content.X.ShouldBe(90);
        root.Children[3].BoxModel.Content.X.ShouldBe(90);
        // 容器高度 = 两行 × 20。
        root.BoxModel.Content.Height.ShouldBe(40);
    }

    [Fact]
    public void Grid_AutoRow_HeightFromTallestItem()
    {
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100), GridTrackSize.Px(100) }
            }),
            ("a", new Style { Height = Length.Px(30) }),
            ("b", new Style { Height = Length.Px(70) })), 800, 600);

        root.BoxModel.Content.Height.ShouldBe(70);
    }

    [Fact]
    public void Grid_GridAutoRows_AppliesToImplicitRows()
    {
        // 无行模板：隐式行取 grid-auto-rows 的固定尺寸（40），两行 → 容器高 80。
        var container = Grid();
        for (int i = 0; i < 3; i++) container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100), GridTrackSize.Px(100) },
                GridAutoRows = GridTrackSize.Px(40)
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        root.BoxModel.Content.Height.ShouldBe(80);
        // 第三个子项在第二行（Y=40）；行轨道固定 40，子项高度 auto 时拉伸填满。
        root.Children[2].BoxModel.Content.Y.ShouldBe(40);
    }

    [Fact]
    public void Grid_GridAutoColumns_AppliesToImplicitColumns()
    {
        // 显式放置越界（第 2 列）产生隐式列，尺寸取 grid-auto-columns（70）。
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(50) },
                GridAutoColumns = GridTrackSize.Px(70)
            }),
            ("a", new Style { GridColumnStart = 2, Height = Length.Px(20) }),
            ("b", new Style { Height = Length.Px(20) })), 800, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(50);
        root.Children[0].BoxModel.Content.Width.ShouldBe(70);
        root.Children[1].BoxModel.Content.X.ShouldBe(0);
        root.Children[1].BoxModel.Content.Width.ShouldBe(50);
    }

    // ---------- 放置 ----------

    [Fact]
    public void Grid_AutoPlacement_FillsRowThenWraps()
    {
        // 3 列 + gap 10，5 个子项：第 4 个换到第二行。
        var container = Grid();
        for (int i = 0; i < 5; i++) container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Gap = Length.Px(10),
                GridTemplateColumns = new List<GridTrackSize>
                {
                    GridTrackSize.Px(100), GridTrackSize.Px(100), GridTrackSize.Px(100)
                }
            }),
            ("it", new Style { Height = Length.Px(20) })), 800, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[0].BoxModel.Content.Y.ShouldBe(0);
        root.Children[2].BoxModel.Content.X.ShouldBe(220);
        root.Children[2].BoxModel.Content.Y.ShouldBe(0);
        root.Children[3].BoxModel.Content.X.ShouldBe(0);
        root.Children[3].BoxModel.Content.Y.ShouldBe(30);
        root.Children[4].BoxModel.Content.X.ShouldBe(110);
        root.Children[4].BoxModel.Content.Y.ShouldBe(30);
        // 容器高度 = 2 行 × 20 + 行 gap 10 = 50。
        root.BoxModel.Content.Height.ShouldBe(50);
    }

    [Fact]
    public void Grid_ExplicitColumnPlacement_GridColumnStartEnd()
    {
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize>
                {
                    GridTrackSize.Px(100), GridTrackSize.Px(100), GridTrackSize.Px(100)
                }
            }),
            ("a", new Style { GridColumnStart = 2, GridColumnEnd = 4, Height = Length.Px(20) }),
            ("b", new Style { Height = Length.Px(20) })), 800, 600);

        // a 占第 2..3 列（线 2/4）；b 自动放置到剩余的第一个空格（行 0 列 0）。
        root.Children[0].BoxModel.Content.X.ShouldBe(100);
        root.Children[0].BoxModel.Content.Width.ShouldBe(200);
        root.Children[1].BoxModel.Content.X.ShouldBe(0);
        root.Children[1].BoxModel.Content.Width.ShouldBe(100);
    }

    [Fact]
    public void Grid_Span_SpansTracksPlusInternalGap()
    {
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                ColumnGap = Length.Px(10),
                GridTemplateColumns = new List<GridTrackSize>
                {
                    GridTrackSize.Px(100), GridTrackSize.Px(100), GridTrackSize.Px(100)
                }
            }),
            ("a", new Style { GridColumnStart = 1, GridColumnEnd = 3, Height = Length.Px(20) }),
            ("b", new Style { Height = Length.Px(20) })), 800, 600);

        // 跨两列的 area 包含内部 gap：100 + 10 + 100 = 210。
        root.Children[0].BoxModel.Content.Width.ShouldBe(210);
        root.Children[1].BoxModel.Content.X.ShouldBe(220);
    }

    [Fact]
    public void Grid_NegativeEndLine_ResolvesAgainstExplicitTrackCount()
    {
        var container = Grid();
        container.AddChild(Item("a"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize>
                {
                    GridTrackSize.Px(100), GridTrackSize.Px(100), GridTrackSize.Px(100)
                }
            }),
            ("a", new Style { GridColumnStart = 1, GridColumnEnd = -1, Height = Length.Px(20) })), 800, 600);

        // 1 / -1：横跨全部 3 个显式列。
        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[0].BoxModel.Content.Width.ShouldBe(300);
    }

    [Fact]
    public void Grid_ExplicitRowPlacement()
    {
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));
        container.AddChild(Item("c"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100), GridTrackSize.Px(100) }
            }),
            ("a", new Style { GridRowStart = 2, GridColumnStart = 1, Height = Length.Px(30) }),
            ("b", new Style { Height = Length.Px(20) }),
            ("c", new Style { Height = Length.Px(20) })), 800, 600);

        // b、c 自动放置到第 1 行两列；a 在第 2 行第 1 列（Y = 第 1 行高 20）。
        root.Children[1].BoxModel.Content.X.ShouldBe(0);
        root.Children[1].BoxModel.Content.Y.ShouldBe(0);
        root.Children[2].BoxModel.Content.X.ShouldBe(100);
        root.Children[2].BoxModel.Content.Y.ShouldBe(0);
        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[0].BoxModel.Content.Y.ShouldBe(20);
        root.BoxModel.Content.Height.ShouldBe(50);
    }

    [Fact]
    public void Grid_ExplicitPlacementBeyondTemplate_CreatesImplicitTracks()
    {
        // 显式放置到第 3 列（模板仅 1 列）：隐式列按需创建，默认 auto（内容尺寸）。
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                // 宽度恰好等于轨道总和（50+0+40），避免默认模式拉伸 auto 列改变断言基准。
                Width = Length.Px(90),
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(50) }
            }),
            ("a", new Style { GridColumnStart = 3, Width = Length.Px(40), Height = Length.Px(30) }),
            ("b", new Style { Height = Length.Px(20) })), 800, 600);

        // 列轨道：[50, auto(空→0), auto(内容 40)] → a 的 X = 50 + 0 = 50。
        root.Children[0].BoxModel.Content.X.ShouldBe(50);
        root.Children[0].BoxModel.Content.Width.ShouldBe(40);
        root.Children[1].BoxModel.Content.X.ShouldBe(0);
        root.Children[1].BoxModel.Content.Width.ShouldBe(50);
    }

    [Fact]
    public void Grid_AutoFlowSpan_ExceedsExplicitColumns_WidensGrid()
    {
        // 自动放置子项的跨度（3）超过显式列数（2）：按规范加宽网格而非钳制跨度。
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                // 宽度恰好等于两条显式轨道之和，使隐式 auto 列不被默认模式拉伸（保持 0），
                // 从而断言 span-3 的子项宽 = 100 + 100 + 0 = 200（网格被加宽、span 未钳制）。
                Width = Length.Px(200),
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100), GridTrackSize.Px(100) }
            }),
            ("a", new Style { GridColumnEnd = 4, Height = Length.Px(20) }),
            ("b", new Style { Height = Length.Px(20) })), 800, 600);

        // a 跨 3 列（第 3 列为隐式 auto，无 span-1 子项贡献 → 0）：宽 = 100 + 100 + 0 = 200。
        root.Children[0].BoxModel.Content.Width.ShouldBe(200);
        // b 换到第二行。
        root.Children[1].BoxModel.Content.X.ShouldBe(0);
        root.Children[1].BoxModel.Content.Y.ShouldBe(20);
    }

    [Fact]
    public void Grid_AutoPlacement_SkipsOccupiedCells()
    {
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));
        container.AddChild(Item("c"));
        container.AddChild(Item("d"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100), GridTrackSize.Px(100) }
            }),
            ("a", new Style { GridRowStart = 1, GridColumnStart = 2, Height = Length.Px(20) }),
            ("b", new Style { Height = Length.Px(20) }),
            ("c", new Style { Height = Length.Px(20) }),
            ("d", new Style { Height = Length.Px(20) })), 800, 600);

        // a 显式占 (行0, 列1)；b 占 (0,0)；c 跳过被占的 (0,1) 换行到 (1,0)；d 占 (1,1)。
        root.Children[1].BoxModel.Content.X.ShouldBe(0);
        root.Children[1].BoxModel.Content.Y.ShouldBe(0);
        root.Children[0].BoxModel.Content.X.ShouldBe(100);
        root.Children[0].BoxModel.Content.Y.ShouldBe(0);
        root.Children[2].BoxModel.Content.X.ShouldBe(0);
        root.Children[2].BoxModel.Content.Y.ShouldBe(20);
        root.Children[3].BoxModel.Content.X.ShouldBe(100);
        root.Children[3].BoxModel.Content.Y.ShouldBe(20);
    }

    [Fact]
    public void Grid_Order_ReordersAutoPlacement()
    {
        var container = Grid();
        container.AddChild(Item("a"));
        container.AddChild(Item("b"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100), GridTrackSize.Px(100) }
            }),
            ("a", new Style { Height = Length.Px(20), Order = 2 }),
            ("b", new Style { Height = Length.Px(20), Order = 1 })), 800, 600);

        // order 1（b）先放置到 (0,0)，order 2（a）到 (0,1)。
        root.Children[1].BoxModel.Content.X.ShouldBe(0);
        root.Children[0].BoxModel.Content.X.ShouldBe(100);
    }

    // ---------- 子项尺寸与对齐 ----------

    [Fact]
    public void GridItem_DefaultStretch_FillsGridArea()
    {
        var container = Grid();
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(150) },
                GridTemplateRows = new List<GridTrackSize> { GridTrackSize.Px(80) }
            }),
            ("it", new Style())), 800, 600);

        // 宽度/高度均 auto：默认 stretch 填满 area（margin-box == area）。
        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[0].BoxModel.Content.Y.ShouldBe(0);
        root.Children[0].BoxModel.Content.Width.ShouldBe(150);
        root.Children[0].BoxModel.Content.Height.ShouldBe(80);
    }

    [Fact]
    public void GridItem_ExplicitSize_AlignsStartWithinArea()
    {
        var container = Grid();
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(150) },
                GridTemplateRows = new List<GridTrackSize> { GridTrackSize.Px(80) }
            }),
            ("it", new Style { Width = Length.Px(60), Height = Length.Px(30) })), 800, 600);

        // 显式尺寸不拉伸，起点对齐。
        root.Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[0].BoxModel.Content.Y.ShouldBe(0);
        root.Children[0].BoxModel.Content.Width.ShouldBe(60);
        root.Children[0].BoxModel.Content.Height.ShouldBe(30);
    }

    [Fact]
    public void GridItem_AlignSelfCenter_OffsetsWithinArea()
    {
        var container = Grid();
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100) },
                GridTemplateRows = new List<GridTrackSize> { GridTrackSize.Px(80) }
            }),
            ("it", new Style { Height = Length.Px(30), AlignSelf = AlignSelf.Center })), 800, 600);

        root.Children[0].BoxModel.Content.Y.ShouldBe(25);
        root.Children[0].BoxModel.Content.Height.ShouldBe(30);
    }

    [Fact]
    public void GridItem_ContainerAlignItemsFlexEnd_OffsetsWithinArea()
    {
        var container = Grid();
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                AlignItems = AlignItems.FlexEnd,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100) },
                GridTemplateRows = new List<GridTrackSize> { GridTrackSize.Px(80) }
            }),
            ("it", new Style { Height = Length.Px(30) })), 800, 600);

        root.Children[0].BoxModel.Content.Y.ShouldBe(50);
        root.Children[0].BoxModel.Content.Height.ShouldBe(30);
    }

    [Fact]
    public void GridItem_AutoMargins_CenterWithinArea()
    {
        var container = Grid();
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100) },
                GridTemplateRows = new List<GridTrackSize> { GridTrackSize.Px(80) }
            }),
            ("it", new Style
            {
                Width = Length.Px(40),
                Height = Length.Px(30),
                MarginTop = Length.Auto,
                MarginRight = Length.Auto,
                MarginBottom = Length.Auto,
                MarginLeft = Length.Auto
            })), 800, 600);

        // auto margin 均分剩余空间：水平 (100-40)/2=30，垂直 (80-30)/2=25。
        root.Children[0].BoxModel.Content.X.ShouldBe(30);
        root.Children[0].BoxModel.Content.Y.ShouldBe(25);
    }

    [Fact]
    public void GridItem_TextNode_IsGridItemWithoutStretch()
    {
        // 文本节点作为普通 grid 项参与放置，但不拉伸（保持文本自然宽度）。
        var container = new DivElement { Class = "grid", TextContent = "text" };

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(200) }
            })), 800, 600);

        root.Children.Count.ShouldBe(1);
        var textBox = root.Children[0];
        textBox.Type.ShouldBe(LayoutType.Text);
        textBox.BoxModel.Content.X.ShouldBe(0);
        float textWidth = Miko.Utils.TextMeasurer.MeasureTextWidth(
            "text", textBox.ComputedStyle.FontFamily, 16f, textBox.ComputedStyle.FontWeight);
        textBox.BoxModel.Content.Width.ShouldBe(textWidth, 0.5f);
        textBox.BoxModel.Content.Width.ShouldBeLessThan(200);
    }

    [Fact]
    public void GridItem_PercentWidth_ResolvesAgainstArea()
    {
        var container = Grid();
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(200) }
            }),
            ("it", new Style { Width = Length.Percent(50), Height = Length.Px(20) })), 800, 600);

        root.Children[0].BoxModel.Content.Width.ShouldBe(100);
    }

    // ---------- 容器行为 ----------

    [Fact]
    public void Grid_AutoHeight_SumsRowsPlusGaps()
    {
        var container = Grid();
        for (int i = 0; i < 4; i++) container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                RowGap = Length.Px(10),
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100), GridTrackSize.Px(100) }
            }),
            ("it", new Style { Height = Length.Px(20) })), 800, 600);

        // 两行 20 + 行 gap 10 = 50。
        root.BoxModel.Content.Height.ShouldBe(50);
    }

    [Fact]
    public void Grid_JustifyContent_DistributesColumnTracks()
    {
        var container = new DivElement();
        var center = Grid("ga");
        var end = Grid("gb");
        var between = Grid("gc");
        foreach (var g in new[] { center, end, between })
        {
            g.AddChild(Item());
            g.AddChild(Item());
            container.AddChild(g);
        }

        var template = new List<GridTrackSize> { GridTrackSize.Px(100), GridTrackSize.Px(100) };
        var root = _layoutEngine.Layout(container, Sheets(
            ("ga", new Style { Display = Display.Grid, Width = Length.Px(400), JustifyContent = JustifyContent.Center, GridTemplateColumns = template }),
            ("gb", new Style { Display = Display.Grid, Width = Length.Px(400), JustifyContent = JustifyContent.FlexEnd, GridTemplateColumns = template }),
            ("gc", new Style { Display = Display.Grid, Width = Length.Px(400), JustifyContent = JustifyContent.SpaceBetween, GridTemplateColumns = template }),
            ("it", new Style { Height = Length.Px(20) })), 800, 600);

        // Center：剩余 200，偏移 100 → X: 100, 200。
        root.Children[0].Children[0].BoxModel.Content.X.ShouldBe(100);
        root.Children[0].Children[1].BoxModel.Content.X.ShouldBe(200);
        // FlexEnd：偏移 200 → X: 200, 300。
        root.Children[1].Children[0].BoxModel.Content.X.ShouldBe(200);
        root.Children[1].Children[1].BoxModel.Content.X.ShouldBe(300);
        // SpaceBetween：轨道间距 200 → X: 0, 300。
        root.Children[2].Children[0].BoxModel.Content.X.ShouldBe(0);
        root.Children[2].Children[1].BoxModel.Content.X.ShouldBe(300);
    }

    [Fact]
    public void Grid_AlignContent_DistributesRowTracksInDefiniteHeight()
    {
        var container = new DivElement();
        var center = Grid("ga");
        center.AddChild(Item());
        var end = Grid("gb");
        end.AddChild(Item());
        container.AddChild(center);
        container.AddChild(end);

        var columns = new List<GridTrackSize> { GridTrackSize.Px(100) };
        var root = _layoutEngine.Layout(container, Sheets(
            ("ga", new Style
            {
                Display = Display.Grid, Width = Length.Px(200), Height = Length.Px(200),
                AlignContent = AlignContent.Center, GridTemplateColumns = columns
            }),
            ("gb", new Style
            {
                Display = Display.Grid, Width = Length.Px(200), Height = Length.Px(200),
                AlignContent = AlignContent.FlexEnd, GridTemplateColumns = columns
            }),
            ("it", new Style { Height = Length.Px(20) })), 800, 600);

        // 行高 20，剩余 180：Center → Y = 90；FlexEnd → Y = 200(自身容器 Y) + 180 = 380。
        root.Children[0].Children[0].BoxModel.Content.Y.ShouldBe(90);
        root.Children[1].Children[0].BoxModel.Content.Y.ShouldBe(200 + 180);
    }

    [Fact]
    public void Grid_AlignContent_Stretch_GrowsRowTracks()
    {
        var container = Grid();
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(200),
                Height = Length.Px(200),
                AlignContent = AlignContent.Stretch,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100) }
            }),
            ("it", new Style())), 800, 600);

        // auto 行（空内容高 0）被 stretch 均分剩余空间抬高到 200；子项 stretch 填满。
        root.Children[0].BoxModel.Content.Height.ShouldBe(200);
    }

    [Fact]
    public void Grid_PaddingBorder_OffsetGridArea()
    {
        var container = Grid();
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Padding = new Padding(Length.Px(10)),
                BorderWidth = Length.Px(5),
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100) }
            }),
            ("it", new Style { Height = Length.Px(20) })), 800, 600);

        root.Children[0].BoxModel.Content.X.ShouldBe(15);
        root.Children[0].BoxModel.Content.Y.ShouldBe(15);
    }

    [Fact]
    public void Grid_BoxSizing_BorderBox()
    {
        var container = Grid();
        container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                BoxSizing = BoxSizing.BorderBox,
                Width = Length.Px(200),
                Padding = new Padding(Length.Px(10)),
                BorderWidth = Length.Px(5),
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Fr(1) }
            }),
            ("it", new Style { Height = Length.Px(20) })), 800, 600);

        // border-box：内容宽 = 200 - 2×10 - 2×5 = 170；fr 轨道 = 170。
        root.BoxModel.Content.Width.ShouldBe(170);
        root.Children[0].BoxModel.Content.Width.ShouldBe(170);
    }

    [Fact]
    public void Grid_MinMaxWidthHeight_ClampContainer()
    {
        var container = new DivElement();
        var withMin = Grid("a");
        var withMax = Grid("b");
        var withMinH = Grid("c");
        container.AddChild(withMin);
        container.AddChild(withMax);
        container.AddChild(withMinH);

        var root = _layoutEngine.Layout(container, Sheets(
            ("a", new Style { Display = Display.Grid, Width = Length.Px(200), MinWidth = Length.Px(300) }),
            ("b", new Style { Display = Display.Grid, Width = Length.Px(200), MaxWidth = Length.Px(100) }),
            ("c", new Style { Display = Display.Grid, Height = Length.Px(50), MinHeight = Length.Px(120) })), 800, 600);

        root.Children[0].BoxModel.Content.Width.ShouldBe(300);
        root.Children[1].BoxModel.Content.Width.ShouldBe(100);
        root.Children[2].BoxModel.Content.Height.ShouldBe(120);
    }

    [Fact]
    public void Grid_OutOfFlowChild_NotPlaced()
    {
        // absolute 子项不占格、不参与轨道尺寸；仍单独布局获得尺寸。
        var container = Grid();
        container.AddChild(Item("abs"));
        container.AddChild(Item("it"));

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100), GridTrackSize.Px(100) }
            }),
            ("abs", new Style { Position = Position.Absolute, Width = Length.Px(30), Height = Length.Px(30) }),
            ("it", new Style { Height = Length.Px(20) })), 800, 600);

        // 普通子项占据第一格（absolute 兄弟未占位）。
        root.Children[1].BoxModel.Content.X.ShouldBe(0);
        root.Children[1].BoxModel.Content.Y.ShouldBe(0);
        // absolute 子项仍被布局出尺寸。
        root.Children[0].BoxModel.Content.Width.ShouldBe(30);
        root.Children[0].BoxModel.Content.Height.ShouldBe(30);
        // 容器高度只反映在流子项。
        root.BoxModel.Content.Height.ShouldBe(20);
    }

    [Fact]
    public void Grid_ScrollableContent_ReflectsGridExtent()
    {
        var container = Grid();
        for (int i = 0; i < 3; i++) container.AddChild(Item());

        var root = _layoutEngine.Layout(container, Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(100),
                Height = Length.Px(50),
                OverflowY = Overflow.Auto,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(100) }
            }),
            ("it", new Style { Height = Length.Px(30) })), 800, 600);

        root.BoxModel.Content.Height.ShouldBe(50);
        // 网格总高 3×30 = 90 超出容器 → 滚动尺寸反映网格范围。
        root.ScrollableContentHeight.ShouldBe(90);
        root.HasVerticalScrollbar.ShouldBeTrue();
    }

    [Fact]
    public void Grid_NestedGrid_LayoutsIndependently()
    {
        var outer = Grid("outer");
        var inner = Grid("inner");
        inner.AddChild(Item("cell"));
        inner.AddChild(Item("cell"));
        outer.AddChild(inner);
        outer.AddChild(Item("sibling"));

        var root = _layoutEngine.Layout(outer, Sheets(
            ("outer", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(200), GridTrackSize.Px(100) }
            }),
            ("inner", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = new List<GridTrackSize> { GridTrackSize.Px(50), GridTrackSize.Px(50) }
            }),
            ("cell", new Style { Height = Length.Px(20) }),
            ("sibling", new Style { Height = Length.Px(20) })), 800, 600);

        var innerBox = root.Children[0];
        innerBox.Children[0].BoxModel.Content.X.ShouldBe(0);
        innerBox.Children[1].BoxModel.Content.X.ShouldBe(50);
        root.Children[1].BoxModel.Content.X.ShouldBe(200);
    }

    [Fact]
    public void Grid_RowGapColumnGap_AppliedBetweenTracksOnly()
    {
        // 收缩包裹场景（flex 行内的 auto 宽度 grid）：n 个轨道间只有 n-1 个 gap。
        var container = new DivElement { Class = "row" };
        var grid = Grid();
        for (int i = 0; i < 3; i++) grid.AddChild(Item());
        container.AddChild(grid);

        var root = _layoutEngine.Layout(container, Sheets(
            ("row", new Style { Display = Display.Flex }),
            ("grid", new Style
            {
                Display = Display.Grid,
                ColumnGap = Length.Px(10),
                GridTemplateColumns = new List<GridTrackSize>
                {
                    GridTrackSize.Px(100), GridTrackSize.Px(100), GridTrackSize.Px(100)
                }
            }),
            ("it", new Style { Height = Length.Px(20) })), 800, 600);

        // 3×100 + 2×10 = 320。
        root.Children[0].BoxModel.Content.Width.ShouldBe(320);
    }
}
