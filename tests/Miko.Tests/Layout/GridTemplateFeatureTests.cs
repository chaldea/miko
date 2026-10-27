using Miko.Common;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Miko.Styling.Selectors;
using Miko.Utils;
using Shouldly;

namespace Miko.Tests.Layout;

/// <summary>
/// Grid 模板特性（ISSUE-159）：<c>minmax()</c> / <c>min-content</c> / <c>max-content</c> /
/// <c>fit-content()</c> 轨道、<c>repeat()</c>（含 auto-fill / auto-fit）、命名线、
/// <c>grid-template-areas</c>，以及按 CSS 规则解析的放置（线名、span、负向隐式轨道）和
/// 轨道尺寸算法中跨轨子项、弹性轨道的内容下限。
/// </summary>
public class GridTemplateFeatureTests
{
    private readonly LayoutEngine _layoutEngine = new();

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

    private static DivElement Grid(params DivElement[] children)
    {
        var grid = new DivElement { Class = "grid" };
        foreach (var child in children) grid.AddChild(child);
        return grid;
    }

    private static DivElement Item(string cls = "it", string? text = null)
    {
        var item = new DivElement { Class = cls };
        if (text != null) item.TextContent = text;
        return item;
    }

    private static List<GridTrackSize> Tracks(params GridTrackSize[] tracks) => tracks.ToList();

    private static RectF Box(LayoutBox root, int index) => root.Children[index].BoxModel.BorderBox;

    private static float TextWidth(string text) => TextMeasurer.MeasureText(text, "Arial", 16, FontWeight.Normal).Width;

    // ---------- minmax() / min-content / max-content / fit-content() ----------

    [Fact]
    public void MinMax_FixedMinimum_WinsOverTheFrShare()
    {
        // minmax(200px, 1fr) × 2 放进 300px：fr 份额 150 < 下限 200，两列都停在 200（网格溢出）。
        var root = _layoutEngine.Layout(Grid(Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(GridTrackSize.MinMax(200, GridTrackSize.Fr(1)), GridTrackSize.MinMax(200, GridTrackSize.Fr(1))),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 0).Width.ShouldBe(200);
        Box(root, 1).X.ShouldBe(200);
        Box(root, 1).Width.ShouldBe(200);
        root.ScrollableContentWidth.ShouldBe(400);
    }

    [Fact]
    public void MinMax_FrMaximum_SharesTheSpaceAboveTheMinimum()
    {
        var root = _layoutEngine.Layout(Grid(Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(400),
                GridTemplateColumns = Tracks(GridTrackSize.MinMax(100, GridTrackSize.Fr(1)), GridTrackSize.MinMax(100, GridTrackSize.Fr(3))),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 0).Width.ShouldBe(100);
        Box(root, 1).X.ShouldBe(100);
        Box(root, 1).Width.ShouldBe(300);
    }

    [Fact]
    public void MinMax_AutoMinimum_IsClampedByAFixedMaximum()
    {
        // minmax(auto, 100px)：150px 宽的子项把 auto 下限抬到 150，但上限固定时自动最小尺寸
        // 不超过 100（CSS §6.6），轨道停在 100，子项溢出。
        var root = _layoutEngine.Layout(Grid(Item("wide"), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(400),
                JustifyContent = JustifyContent.Center,
                GridTemplateColumns = Tracks(GridTrackSize.MinMax(GridTrackSize.Auto, 100), GridTrackSize.Px(50)),
            }),
            ("wide", new Style { Width = Length.Px(150), Height = Length.Px(10) }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        // 轨道 100 + 50 = 150，居中：起点 (400 − 150) / 2 = 125。
        Box(root, 0).X.ShouldBe(125);
        Box(root, 1).X.ShouldBe(225);
    }

    [Fact]
    public void MinContentTrack_FitsTheLongestWord()
    {
        var root = _layoutEngine.Layout(Grid(Item("text", "aaaa bbbbbbbb cc"), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(600),
                GridTemplateColumns = Tracks(GridTrackSize.MinContent, GridTrackSize.Fr(1)),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 0).Width.ShouldBe(TextWidth("bbbbbbbb"), 0.5f);
        Box(root, 1).X.ShouldBe(TextWidth("bbbbbbbb"), 0.5f);
    }

    [Fact]
    public void MaxContentTrack_FitsTheUnwrappedText()
    {
        var root = _layoutEngine.Layout(Grid(Item("text", "aaaa bbbbbbbb cc"), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(600),
                GridTemplateColumns = Tracks(GridTrackSize.MaxContent, GridTrackSize.Fr(1)),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 0).Width.ShouldBe(TextWidth("aaaa bbbbbbbb cc"), 1f);
    }

    [Fact]
    public void FitContent_GrowsWithContentUpToItsLimit()
    {
        const string longText = "aaaa bbbbbbbb cc dddd eeee ffff gggg";
        var root = _layoutEngine.Layout(Grid(Item("short", "aaaa"), Item("long", longText)), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(600),
                JustifyContent = JustifyContent.Center,
                GridTemplateColumns = Tracks(GridTrackSize.FitContent(Length.Px(100)), GridTrackSize.FitContent(Length.Px(100))),
            })), 800, 600);

        // 内容不足上限：按内容；超过上限：停在上限（内容在其中换行）。
        TextWidth(longText).ShouldBeGreaterThan(100);
        Box(root, 0).Width.ShouldBe(TextWidth("aaaa"), 0.5f);
        Box(root, 1).Width.ShouldBe(100, 0.01f);
    }

    [Fact]
    public void FrTrack_DoesNotShrinkBelowTheMinContentOfItsItems()
    {
        // 1fr = minmax(auto, 1fr)：150px 宽的子项让第一列不小于 150，剩余的 50 归第二列（与浏览器一致）。
        var root = _layoutEngine.Layout(Grid(Item("wide"), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(200),
                GridTemplateColumns = Tracks(GridTrackSize.Fr(1), GridTrackSize.Fr(1)),
            }),
            ("wide", new Style { Width = Length.Px(150), Height = Length.Px(10) }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 1).X.ShouldBe(150);
        Box(root, 1).Width.ShouldBe(50);
    }

    [Fact]
    public void MinMaxZeroFr_IgnoresTheContentOfItsItems()
    {
        var root = _layoutEngine.Layout(Grid(Item("wide"), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(200),
                GridTemplateColumns = Tracks(GridTrackSize.MinMax(0, GridTrackSize.Fr(1)), GridTrackSize.MinMax(0, GridTrackSize.Fr(1))),
            }),
            ("wide", new Style { Width = Length.Px(150), Height = Length.Px(10) }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 1).X.ShouldBe(100);
        Box(root, 1).Width.ShouldBe(100);
    }

    [Fact]
    public void SpanningItem_GrowsTheAutoTrackItCrosses()
    {
        // b 跨两条 auto 列、宽 300；a 只占第一列、宽 50。第二列没有单轨子项，由 b 的需求撑到 250
        // （ISSUE-159 之前跨轨子项不参与轨道尺寸，第二列为 0、网格只有 50 宽）。
        var root = _layoutEngine.Layout(Grid(Item("a"), Item("b")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(800),
                JustifyContent = JustifyContent.Center,
                GridTemplateColumns = Tracks(GridTrackSize.Auto, GridTrackSize.Auto),
            }),
            ("a", new Style { Width = Length.Px(50), Height = Length.Px(10) }),
            ("b", new Style { GridColumn = new GridPlacement(1, GridLine.Span(2)), Width = Length.Px(300), Height = Length.Px(10) })), 800, 600);

        // 网格 50 + 250 = 300，居中于 800：起点 250。
        root.ScrollableContentWidth.ShouldBe(300);
        Box(root, 0).X.ShouldBe(250);
        Box(root, 1).X.ShouldBe(250);
        Box(root, 1).Y.ShouldBe(10);
    }

    [Fact]
    public void FrTracks_InAnIndefiniteWidth_SizeByTheirFlexFactors()
    {
        // 收缩包裹的 grid（flex 行里的项目）：fr 取「每个子项的 max-content / 其轨道系数」的最大值，
        // 1fr 2fr 各放 60px 的子项 → fr = 60 → 轨道 60 / 120，网格宽 180。
        var grid = Grid(Item("a"), Item("b"));
        var row = new DivElement { Class = "row" };
        row.AddChild(grid);

        var layout = _layoutEngine.Layout(row, Sheets(
            ("row", new Style { Display = Display.Flex }),
            ("grid", new Style
            {
                Display = Display.Grid,
                GridTemplateColumns = Tracks(GridTrackSize.Fr(1), GridTrackSize.Fr(2)),
            }),
            ("a", new Style { Width = Length.Px(60), Height = Length.Px(10) }),
            ("b", new Style { Width = Length.Px(60), Height = Length.Px(10) })), 800, 600);

        var gridBox = layout.Children[0];
        gridBox.BoxModel.Content.Width.ShouldBe(180);
        gridBox.Children[1].BoxModel.BorderBox.X.ShouldBe(60);
    }

    [Fact]
    public void AutoRows_WithMinMax_KeepTheirMinimumAndGrowWithContent()
    {
        var root = _layoutEngine.Layout(Grid(Item("short"), Item("tall"), Item("short")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(100),
                AlignContent = AlignContent.FlexEnd,
                GridAutoRows = GridTrackSize.MinMax(50, GridTrackSize.Auto),
            }),
            ("short", new Style { Height = Length.Px(20) }),
            ("tall", new Style { Height = Length.Px(80) })), 800, 600);

        Box(root, 1).Y.ShouldBe(50);
        Box(root, 2).Y.ShouldBe(130);
        root.BoxModel.Content.Height.ShouldBe(180);
    }

    [Fact]
    public void FrRows_InAnAutoHeightGrid_ExpandToFillMinHeight()
    {
        // 高度不确定：fr 行先按内容（各 20），网格被 min-height 撑到 200 后按确定空间重新分配。
        var root = _layoutEngine.Layout(Grid(Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(100),
                MinHeight = Length.Px(200),
                GridTemplateRows = Tracks(GridTrackSize.Fr(1), GridTrackSize.Fr(1)),
                AlignItems = AlignItems.FlexStart,
            }),
            ("it", new Style { Height = Length.Px(20) })), 800, 600);

        root.BoxModel.Content.Height.ShouldBe(200);
        Box(root, 1).Y.ShouldBe(100);
    }

    [Fact]
    public void MinMax_ResolvesViewportUnitsInside()
    {
        // minmax(25vw, 1fr) 中的 vw 在样式阶段折算（视口 800 → 200px）。
        var root = _layoutEngine.Layout(Grid(Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(GridTrackSize.MinMax(Length.Vw(25), GridTrackSize.Fr(1)), GridTrackSize.Fr(1)),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 0).Width.ShouldBe(200);
        Box(root, 1).Width.ShouldBe(100);
    }

    // ---------- repeat() ----------

    [Fact]
    public void Repeat_WithACount_ExpandsTheTracks()
    {
        var root = _layoutEngine.Layout(Grid(Item(), Item(), Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(3, GridTrackSize.Px(100))),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 1).X.ShouldBe(100);
        Box(root, 2).X.ShouldBe(200);
        // 第 4 个换行。
        Box(root, 3).X.ShouldBe(0);
        Box(root, 3).Y.ShouldBe(10);
    }

    [Fact]
    public void Repeat_MixedWithOtherTracks_KeepsTheirOrder()
    {
        // 50px repeat(2, 1fr) 50px 放进 300px：50 / 100 / 100 / 50。
        var root = _layoutEngine.Layout(Grid(Item(), Item(), Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(GridTrackSize.Px(50), GridTrackSize.Repeat(2, GridTrackSize.Fr(1)), GridTrackSize.Px(50)),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 1).X.ShouldBe(50);
        Box(root, 2).X.ShouldBe(150);
        Box(root, 3).X.ShouldBe(250);
        Box(root, 3).Width.ShouldBe(50);
    }

    [Fact]
    public void RepeatAutoFill_PlacesAsManyTracksAsFit()
    {
        // 500px、gap 10、100px 轨道：4 × 100 + 3 × 10 = 430 ≤ 500，第 5 条放不下 → 4 列。
        var root = _layoutEngine.Layout(Grid(Item(), Item(), Item(), Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(500),
                Gap = Length.Px(10),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(GridAutoRepeat.AutoFill, GridTrackSize.Px(100))),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 3).X.ShouldBe(330);
        Box(root, 4).X.ShouldBe(0);
        Box(root, 4).Y.ShouldBe(20);
    }

    [Fact]
    public void RepeatAutoFill_WithMinMaxFr_StretchesTheTracksItFits()
    {
        // repeat(auto-fill, minmax(120px, 1fr)) 放进 500px：按下限放下 4 条，再由 1fr 均分 → 各 125。
        var root = _layoutEngine.Layout(Grid(Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(500),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(GridAutoRepeat.AutoFill, GridTrackSize.MinMax(120, GridTrackSize.Fr(1)))),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 0).Width.ShouldBe(125);
        Box(root, 1).X.ShouldBe(125);
    }

    [Fact]
    public void RepeatAutoFill_KeepsEmptyTracks()
    {
        // 4 条 100px 轨道（含 gap 共 430）只放 2 个子项：空轨道保留，居中时网格仍按 430 计。
        var root = _layoutEngine.Layout(Grid(Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(500),
                Gap = Length.Px(10),
                JustifyContent = JustifyContent.Center,
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(GridAutoRepeat.AutoFill, GridTrackSize.Px(100))),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(35);
        Box(root, 1).X.ShouldBe(145);
    }

    [Fact]
    public void RepeatAutoFit_CollapsesEmptyTracksAndTheirGaps()
    {
        // 同上但 auto-fit：两条空轨道折叠为 0、两侧 gap 一并折叠，网格只剩 210，居中起点 145。
        var root = _layoutEngine.Layout(Grid(Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(500),
                Gap = Length.Px(10),
                JustifyContent = JustifyContent.Center,
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(GridAutoRepeat.AutoFit, GridTrackSize.Px(100))),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(145);
        Box(root, 1).X.ShouldBe(255);
        root.ScrollableContentWidth.ShouldBe(210);
    }

    [Fact]
    public void RepeatAutoFit_WithFr_LetsTheOccupiedTracksTakeTheSpace()
    {
        // repeat(auto-fit, minmax(100px, 1fr))：500px 放得下 5 条，只有 2 个子项 → 其余折叠，两条各 250。
        var root = _layoutEngine.Layout(Grid(Item(), Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(500),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(GridAutoRepeat.AutoFit, GridTrackSize.MinMax(100, GridTrackSize.Fr(1)))),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        Box(root, 0).Width.ShouldBe(250);
        Box(root, 1).X.ShouldBe(250);
        Box(root, 1).Width.ShouldBe(250);
    }

    [Fact]
    public void RepeatAutoFill_InAnAutoHeight_UsesTheMinHeight()
    {
        // 行方向高度不确定：取填满 min-height 120 的最少次数 → 3 条 50px 行，网格高 150。
        var root = _layoutEngine.Layout(Grid(Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(100),
                MinHeight = Length.Px(120),
                GridTemplateRows = Tracks(GridTrackSize.Repeat(GridAutoRepeat.AutoFill, GridTrackSize.Px(50))),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        root.BoxModel.Content.Height.ShouldBe(150);
    }

    [Fact]
    public void RepeatAutoFill_InAnAutoHeight_UsesTheMaxHeight()
    {
        // 只有 max-height 120：取放得下的最多次数 → 2 条 50px 行，网格高 100。
        var root = _layoutEngine.Layout(Grid(Item()), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(100),
                MaxHeight = Length.Px(120),
                GridTemplateRows = Tracks(GridTrackSize.Repeat(GridAutoRepeat.AutoFill, GridTrackSize.Px(50))),
            }),
            ("it", new Style { Height = Length.Px(10) })), 800, 600);

        root.BoxModel.Content.Height.ShouldBe(100);
    }

    // ---------- 命名线 ----------

    [Fact]
    public void NamedLines_PlaceItemsBetweenThem()
    {
        var root = _layoutEngine.Layout(Grid(Item("content"), Item("full")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(
                    GridTrackSize.LineNames("full-start"), GridTrackSize.Px(50),
                    GridTrackSize.LineNames("content-start"), GridTrackSize.Px(200),
                    GridTrackSize.LineNames("content-end"), GridTrackSize.Px(50),
                    GridTrackSize.LineNames("full-end")),
            }),
            ("content", new Style { GridColumn = new GridPlacement("content-start", "content-end"), Height = Length.Px(10) }),
            ("full", new Style { GridColumn = new GridPlacement("full-start", "full-end"), Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(50);
        Box(root, 0).Width.ShouldBe(200);
        Box(root, 1).X.ShouldBe(0);
        Box(root, 1).Width.ShouldBe(300);
        Box(root, 1).Y.ShouldBe(10);
    }

    [Fact]
    public void NamedLine_WithAnIndex_PicksTheNthLineOfThatName()
    {
        // repeat(3, [col] 100px)：三条名为 col 的线（1、2、3）；第 2 条 → x 100，倒数第 1 条 → x 200。
        var root = _layoutEngine.Layout(Grid(Item("second"), Item("last")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(3, GridTrackSize.LineNames("col"), GridTrackSize.Px(100))),
            }),
            ("second", new Style { GridColumnStart = GridLine.Named("col", 2), GridRowStart = GridLine.Line(1), Height = Length.Px(10) }),
            ("last", new Style { GridColumnStart = GridLine.Named("col", -1), GridRowStart = GridLine.Line(1), Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(100);
        Box(root, 1).X.ShouldBe(200);
    }

    [Fact]
    public void Repeat_MergesTheLineNamesWhereRepetitionsMeet()
    {
        // repeat(2, [a] 50px [b]) = [a] 50px [b a] 50px [b]：第 2 条线既叫 b 也叫 a。
        var root = _layoutEngine.Layout(Grid(Item("b"), Item("a2")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(100),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(2,
                    GridTrackSize.LineNames("a"), GridTrackSize.Px(50), GridTrackSize.LineNames("b"))),
            }),
            ("b", new Style { GridColumnStart = GridLine.Named("b"), GridRowStart = GridLine.Line(1), Height = Length.Px(10) }),
            ("a2", new Style { GridColumnStart = GridLine.Named("a", 2), GridRowStart = GridLine.Line(2), Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(50);
        Box(root, 1).X.ShouldBe(50);
    }

    [Fact]
    public void NamedLine_ThatDoesNotExist_ResolvesIntoTheImplicitGrid()
    {
        // 名字不够数时显式网格之后的隐式线都视为带该名字：第一条隐式线是线 4（两列网格的线为 1..3）。
        var root = _layoutEngine.Layout(Grid(Item("lost")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(200),
                GridTemplateColumns = Tracks(GridTrackSize.Px(100), GridTrackSize.Px(100)),
            }),
            ("lost", new Style { GridColumnStart = GridLine.Named("nope"), Width = Length.Px(40), Height = Length.Px(10) })), 800, 600);

        // 线 3→4 之间的隐式列为空（0 宽），子项落在线 4 起始的隐式列里。
        Box(root, 0).X.ShouldBe(200);
    }

    [Fact]
    public void SpanToANamedLine_StopsAtTheNthLineWithThatName()
    {
        // [a] 50px 50px [b] 50px [b]
        var root = _layoutEngine.Layout(Grid(Item("one"), Item("two")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(150),
                GridTemplateColumns = Tracks(
                    GridTrackSize.LineNames("a"), GridTrackSize.Px(50), GridTrackSize.Px(50),
                    GridTrackSize.LineNames("b"), GridTrackSize.Px(50), GridTrackSize.LineNames("b")),
            }),
            ("one", new Style { GridColumn = new GridPlacement("a", GridLine.Span("b")), Height = Length.Px(10) }),
            ("two", new Style { GridColumn = new GridPlacement("a", GridLine.Span("b", 2)), Height = Length.Px(10) })), 800, 600);

        Box(root, 0).Width.ShouldBe(100);
        Box(root, 1).Width.ShouldBe(150);
    }

    // ---------- grid-template-areas ----------

    [Fact]
    public void TemplateAreas_PlaceItemsByAreaName()
    {
        // DOM 顺序与区域顺序无关。
        var root = _layoutEngine.Layout(Grid(Item("footer"), Item("main"), Item("header"), Item("nav")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(400),
                GridTemplateAreas = new GridTemplateAreas(
                    "header header",
                    "nav    main",
                    "footer footer"),
                GridTemplateColumns = Tracks(GridTrackSize.Px(100), GridTrackSize.Fr(1)),
                GridTemplateRows = Tracks(GridTrackSize.Px(50), GridTrackSize.Px(200), GridTrackSize.Px(30)),
            }),
            ("header", new Style { GridArea = "header" }),
            ("nav", new Style { GridArea = "nav" }),
            ("main", new Style { GridArea = "main" }),
            ("footer", new Style { GridArea = "footer" })), 800, 600);

        var footer = Box(root, 0);
        var main = Box(root, 1);
        var header = Box(root, 2);
        var nav = Box(root, 3);

        header.ShouldBe(new RectF(0, 0, 400, 50));
        nav.ShouldBe(new RectF(0, 50, 100, 200));
        main.ShouldBe(new RectF(100, 50, 300, 200));
        footer.ShouldBe(new RectF(0, 250, 400, 30));
    }

    [Fact]
    public void TemplateAreas_ProvideImplicitStartAndEndLineNames()
    {
        var root = _layoutEngine.Layout(Grid(Item("it")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateAreas = new GridTemplateAreas("a b b"),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(3, GridTrackSize.Px(100))),
            }),
            ("it", new Style
            {
                GridColumnStart = GridLine.Named("b-start"),
                GridColumnEnd = GridLine.Named("b-end"),
                Height = Length.Px(10),
            })), 800, 600);

        Box(root, 0).X.ShouldBe(100);
        Box(root, 0).Width.ShouldBe(200);
    }

    [Fact]
    public void TemplateAreas_WiderThanTheTemplate_AddExplicitAutoSizedTracks()
    {
        // 区域有 3 列而模板只有 1 列：另两列仍属显式网格，按 grid-auto-columns 定尺寸。
        var root = _layoutEngine.Layout(Grid(Item("it")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(240),
                GridTemplateAreas = new GridTemplateAreas("a b c"),
                GridTemplateColumns = Tracks(GridTrackSize.Px(100)),
                GridAutoColumns = GridTrackSize.Px(70),
            }),
            ("it", new Style { GridArea = "c", Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(170);
        Box(root, 0).Width.ShouldBe(70);
    }

    [Fact]
    public void ExplicitStartAndEndLineNames_FormAnImplicitArea()
    {
        // [side-start] 100px [side-end main-start] 1fr [main-end]：GridArea = "main" 解析到这对线。
        var root = _layoutEngine.Layout(Grid(Item("it")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(
                    GridTrackSize.LineNames("side-start"), GridTrackSize.Px(100),
                    GridTrackSize.LineNames("side-end", "main-start"), GridTrackSize.Fr(1),
                    GridTrackSize.LineNames("main-end")),
            }),
            ("it", new Style { GridArea = "main", Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(100);
        Box(root, 0).Width.ShouldBe(200);
    }

    [Fact]
    public void TemplateAreas_RejectMalformedDefinitions()
    {
        Should.Throw<ArgumentException>(() => new GridTemplateAreas("a a", "a b", "b b"))
            .Message.ShouldContain("rectangle");
        Should.Throw<ArgumentException>(() => new GridTemplateAreas("a b", "c"))
            .Message.ShouldContain("same number of columns");
        Should.Throw<ArgumentException>(() => new GridTemplateAreas("a #"));
        Should.Throw<ArgumentException>(() => new GridTemplateAreas());
    }

    [Fact]
    public void TemplateAreas_TreatDotsAsEmptyCells()
    {
        var areas = new GridTemplateAreas("a ...", ". b");

        areas.RowCount.ShouldBe(2);
        areas.ColumnCount.ShouldBe(2);
        areas.AreaNames.OrderBy(n => n).ShouldBe(new[] { "a", "b" });
        areas.TryGetArea("b", out int rs, out int cs, out int re, out int ce).ShouldBeTrue();
        (rs, cs, re, ce).ShouldBe((1, 1, 2, 2));
        areas.ToString().ShouldBe("\"a ...\" \". b\"");
    }

    // ---------- 放置规则 ----------

    [Fact]
    public void EndLineAlone_IsADefinitePlacement()
    {
        // grid-column-end: 3（起始 auto）= 线 2 / 线 3，即第 2 列——不是「自动放置 + 跨度」。
        var root = _layoutEngine.Layout(Grid(Item("it")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(3, GridTrackSize.Px(100))),
            }),
            ("it", new Style { GridColumnEnd = GridLine.Line(3), Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(100);
        Box(root, 0).Width.ShouldBe(100);
    }

    [Fact]
    public void NegativeLineBeforeTheExplicitGrid_CreatesLeadingImplicitTracks()
    {
        // 两列显式网格的线为 1..3；-4 → 线 0，在显式网格之前产生一条隐式列（grid-auto-columns 50），
        // 显式列整体右移。
        var root = _layoutEngine.Layout(Grid(Item("before"), Item("first")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(250),
                GridTemplateColumns = Tracks(GridTrackSize.Px(100), GridTrackSize.Px(100)),
                GridAutoColumns = GridTrackSize.Px(50),
            }),
            ("before", new Style { GridColumnStart = GridLine.Line(-4), GridRowStart = GridLine.Line(1), Height = Length.Px(10) }),
            ("first", new Style { GridColumnStart = GridLine.Line(1), GridRowStart = GridLine.Line(1), Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(0);
        Box(root, 0).Width.ShouldBe(50);
        Box(root, 1).X.ShouldBe(50);
        Box(root, 1).Width.ShouldBe(100);
    }

    [Fact]
    public void SpanOnTheStartSide_CountsBackFromADefiniteEnd()
    {
        var root = _layoutEngine.Layout(Grid(Item("it")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(3, GridTrackSize.Px(100))),
            }),
            ("it", new Style { GridColumnStart = GridLine.Span(2), GridColumnEnd = GridLine.Line(4), Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(100);
        Box(root, 0).Width.ShouldBe(200);
    }

    [Fact]
    public void ConflictingLines_AreSwappedOrWidened()
    {
        var root = _layoutEngine.Layout(Grid(Item("swapped"), Item("same"), Item("spans")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(3, GridTrackSize.Px(100))),
            }),
            // 3 / 1 → 1 / 3。
            ("swapped", new Style { GridColumn = new GridPlacement(3, 1), GridRowStart = GridLine.Line(1), Height = Length.Px(10) }),
            // 2 / 2 → 2 / 3。
            ("same", new Style { GridColumn = new GridPlacement(2, 2), GridRowStart = GridLine.Line(2), Height = Length.Px(10) }),
            // span 2 / span 3 → 结束侧的 span 被忽略：自动放置、跨 2 列。
            ("spans", new Style { GridColumn = new GridPlacement(GridLine.Span(2), GridLine.Span(3)), GridRowStart = GridLine.Line(3), Height = Length.Px(10) })), 800, 600);

        Box(root, 0).X.ShouldBe(0);
        Box(root, 0).Width.ShouldBe(200);
        Box(root, 1).X.ShouldBe(100);
        Box(root, 1).Width.ShouldBe(100);
        Box(root, 2).Width.ShouldBe(200);
    }

    [Fact]
    public void AutoPlacement_WithADefiniteColumn_MovesTheCursorDownInsteadOfBackfilling()
    {
        // sparse：a 自动放到 (0,0)、b 自动放到 (0,1)；c 指定第 1 列——游标已在第 1 列之后，换到下一行。
        var root = _layoutEngine.Layout(Grid(Item("a"), Item("b"), Item("c")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(3, GridTrackSize.Px(100))),
            }),
            ("a", new Style { Height = Length.Px(10) }),
            ("b", new Style { Height = Length.Px(10) }),
            ("c", new Style { GridColumnStart = GridLine.Line(1), Height = Length.Px(10) })), 800, 600);

        Box(root, 1).X.ShouldBe(100);
        Box(root, 2).X.ShouldBe(0);
        Box(root, 2).Y.ShouldBe(10);
    }

    [Fact]
    public void GridRowAndColumnShorthands_SetBothLines()
    {
        var root = _layoutEngine.Layout(Grid(Item("it")), Sheets(
            ("grid", new Style
            {
                Display = Display.Grid,
                Width = Length.Px(300),
                GridTemplateColumns = Tracks(GridTrackSize.Repeat(3, GridTrackSize.Px(100))),
                GridTemplateRows = Tracks(GridTrackSize.Repeat(3, GridTrackSize.Px(20))),
            }),
            ("it", new Style { GridColumn = new GridPlacement(2, GridLine.Span(2)), GridRow = 3 })), 800, 600);

        Box(root, 0).ShouldBe(new RectF(100, 40, 200, 20));
    }

    // ---------- 值类型 ----------

    [Fact]
    public void TrackSizes_FormatAsCss()
    {
        GridTrackSize.MinMax(120, GridTrackSize.Fr(1)).ToString().ShouldBe("minmax(120px, 1fr)");
        GridTrackSize.Repeat(GridAutoRepeat.AutoFill, GridTrackSize.MinMax(GridTrackSize.MinContent, 200))
            .ToString().ShouldBe("repeat(auto-fill, minmax(min-content, 200px))");
        GridTrackSize.Repeat(2, GridTrackSize.LineNames("a", "b"), GridTrackSize.Auto).ToString().ShouldBe("repeat(2, [a b] auto)");
        GridTrackSize.FitContent(Length.Px(300)).ToString().ShouldBe("fit-content(300px)");
    }

    [Fact]
    public void GridLines_FormatAsCss()
    {
        GridLine.Auto.ToString().ShouldBe("auto");
        GridLine.Line(-1).ToString().ShouldBe("-1");
        GridLine.Named("header").ToString().ShouldBe("header");
        GridLine.Named("col", 2).ToString().ShouldBe("2 col");
        GridLine.Span(2).ToString().ShouldBe("span 2");
        GridLine.Span("col").ToString().ShouldBe("span col");
        ((GridLine)0).ShouldBe(GridLine.Auto);
        ((GridLine)"x").ShouldBe(GridLine.Named("x"));
        GridLine.Named("x").ShouldNotBe(GridLine.Named("x", 1));
    }

    [Fact]
    public void InvalidTrackDefinitions_Throw()
    {
        Should.Throw<ArgumentException>(() => GridTrackSize.MinMax(GridTrackSize.Fr(1), 100));
        Should.Throw<ArgumentException>(() => GridTrackSize.Repeat(0, GridTrackSize.Px(10)));
        Should.Throw<ArgumentException>(() => GridTrackSize.Repeat(2, GridTrackSize.Repeat(2, GridTrackSize.Px(10))));
        Should.Throw<ArgumentException>(() => GridTrackSize.Repeat(2, GridTrackSize.LineNames("a")));
        // auto-fill / auto-fit 的轨道必须至少一端是固定长度。
        Should.Throw<ArgumentException>(() => GridTrackSize.Repeat(GridAutoRepeat.AutoFill, GridTrackSize.Fr(1)));
        Should.Throw<ArgumentException>(() => GridTrackSize.LineNames("span"));
        Should.Throw<ArgumentException>(() => GridLine.Line(0));
        Should.Throw<ArgumentException>(() => GridLine.Span(0));
        Should.Throw<ArgumentException>(() => GridLine.Named("two words"));
    }
}
