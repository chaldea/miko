using Miko.Common;
using Miko.Components;
using Miko.Components.Virtualization;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Components.Virtualization;

/// <summary>
/// ISSUE-151: <see cref="Virtualize{TItem}"/> driven by a real engine — scroll input through
/// <see cref="MikoEngine.ScrollBy"/>, a frame rendered after each step, the way a host drives it.
/// </summary>
public class VirtualizeTests : IDisposable
{
    private const int W = 400;
    private const int H = 300;

    private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(W, H));

    public void Dispose() => _surface.Dispose();

    /// <summary>A page: a 300px scroller holding a header, the virtualized list and a footer.</summary>
    private sealed class ListPage : ComponentBase
    {
        public List<Row> Rows { get; set; } = new();
        public Action<Virtualize<Row>>? Configure { get; set; }
        public Func<Row, RenderFragment>? Template { get; set; }
        public float HeaderHeight { get; set; }
        public int Renders { get; private set; }

        public void Rerender() => NotifyStateChanged();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Renders++;
            var scroller = builder.OpenElement<DivElement>();
            scroller.Class = "scroller";
            scroller.Style = new Style { Height = Length.Px(H), OverflowY = Overflow.Auto };

            if (HeaderHeight > 0)
            {
                var header = builder.OpenElement<DivElement>();
                header.Class = "header";
                header.Style = new Style { Height = Length.Px(HeaderHeight) };
                builder.CloseElement();
            }

            var list = builder.OpenComponent<Virtualize<Row>>();
            list.Items = Rows;
            list.ChildContent = Template is { } template ? row => template(row) : RowTemplate;
            Configure?.Invoke(list);
            builder.CloseComponent();

            builder.CloseElement();
        }

        private static RenderFragment RowTemplate(Row row) => builder =>
        {
            var element = builder.OpenElement<DivElement>();
            element.Id = $"row-{row.Id}";
            element.Class = "row";
            element.Style = new Style { Height = Length.Px(row.Height) };
            builder.AddContent(row.Text);
            builder.CloseElement();
        };
    }

    private sealed record Row(int Id, float Height, string Text);

    private static List<Row> MakeRows(int count, Func<int, float>? height = null, int firstId = 0)
        => Enumerable.Range(firstId, count).Select(i => new Row(i, height?.Invoke(i) ?? 40f, $"Row {i}")).ToList();

    private (MikoEngine Engine, ListPage Page, Element Root) Start(ListPage page)
    {
        var root = page.Build();
        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(root, new List<StyleSheet>(), _surface.Canvas, W, H);
        return (engine, page, root);
    }

    private void Frame(MikoEngine engine) => engine.Render(_surface.Canvas);

    private void Scroll(MikoEngine engine, float dy)
    {
        engine.ScrollBy(W / 2f, H / 2f, 0, dy);
        Frame(engine);
    }

    private static Element Scroller(Element root) => root.HasClass("scroller") ? root : root.FindByClass("scroller")[0];

    private static LayoutBox ScrollerBox(Element root) => Scroller(root).LayoutBox!;

    private static List<Element> RenderedRows(Element root) => root.FindByClass("row");

    private static Element? RowById(Element root, int id) => root.FindById($"row-{id}");

    /// <summary>Where a row is on screen: its layout position minus the scroller's offset.</summary>
    private static float ScreenTop(Element root, Element row)
        => row.LayoutBox!.BoxModel.BorderBox.Top - ScrollerBox(root).ScrollTop;

    /// <summary>
    /// The part of the scroller the list occupies must be covered by rendered rows: the first rendered
    /// row starts at or above the top of the screen (or is the first item), the last ends at or below the
    /// bottom (or is the last item) — no blank strip where rows should be.
    /// </summary>
    private static void AssertNoGap(Element root, int itemCount, string when)
    {
        var rows = RenderedRows(root);
        rows.ShouldNotBeEmpty(when);
        var box = ScrollerBox(root);
        float viewTop = box.ScrollTop;
        float viewBottom = viewTop + box.BoxModel.PaddingBox.Height;

        var first = rows[0];
        var last = rows[^1];
        if (first.Id != "row-0")
            first.LayoutBox!.BoxModel.BorderBox.Top.ShouldBeLessThanOrEqualTo(viewTop + 0.5f, $"blank above the rows ({when})");
        if (last.Id != $"row-{itemCount - 1}")
            last.LayoutBox!.BoxModel.BorderBox.Bottom.ShouldBeGreaterThanOrEqualTo(viewBottom - 0.5f, $"blank below the rows ({when})");
    }

    // ================================================================ window

    [Fact]
    public void Should_RenderOnlyTheVisibleRowsAndTheOverscan()
    {
        var (_, _, root) = Start(new ListPage { Rows = MakeRows(1000) });

        // 300px / 40px = 8 visible rows (the 8th partly), plus 3 below.
        var rows = RenderedRows(root);
        rows.Count.ShouldBeInRange(8, 12);
        rows[0].Id.ShouldBe("row-0");
        ScrollerBox(root).ScrollableContentHeight.ShouldBe(1000 * 40f, 0.5f,
            "the spacers stand in for the rows that are not rendered");
    }

    [Fact]
    public void Should_KeepTheScreenCovered_WhileScrollingThroughTheWholeList()
    {
        var (engine, _, root) = Start(new ListPage { Rows = MakeRows(2000) });

        for (int step = 0; step < 120; step++)
        {
            Scroll(engine, 37);
            AssertNoGap(root, 2000, $"down, step {step}");
            RenderedRows(root).Count.ShouldBeLessThan(20);
        }

        // A fling: big steps.
        for (int step = 0; step < 20; step++)
        {
            Scroll(engine, 900);
            AssertNoGap(root, 2000, $"fling down, step {step}");
        }

        for (int step = 0; step < 60; step++)
        {
            Scroll(engine, -61);
            AssertNoGap(root, 2000, $"up, step {step}");
        }
    }

    [Fact]
    public void Should_NotTouchTheTree_WhenScrollingWithinTheOverscan()
    {
        var (engine, _, root) = Start(new ListPage { Rows = MakeRows(1000) });
        Scroll(engine, 2000);
        var version = engine.Mutations.Version;
        var rows = RenderedRows(root);

        Scroll(engine, 10);

        engine.Mutations.Version.ShouldBe(version, "a scroll that stays inside the rendered rows only repaints");
        RenderedRows(root).ShouldBe(rows);
    }

    [Fact]
    public void Should_KeepTheElementsOfRowsThatStayInTheWindow()
    {
        var (engine, _, root) = Start(new ListPage { Rows = MakeRows(1000) });
        Scroll(engine, 2000);
        var window = RenderedRows(root);
        var kept = window[^2];
        var version = engine.Mutations.Version;

        // Far enough to move the window (past the 3 rows of overscan), not far enough to scroll the
        // second-to-last rendered row out of it.
        Scroll(engine, 160);

        engine.Mutations.Version.ShouldNotBe(version, "the window must have moved for this to prove anything");
        RowById(root, int.Parse(kept.Id!["row-".Length..])).ShouldBeSameAs(kept,
            "a row that stayed in the window is not rebuilt");
    }

    [Fact]
    public void Should_RunTheTemplateAgain_WhenTheParentRerenders()
    {
        var page = new ListPage { Rows = MakeRows(1000) };
        var (engine, _, root) = Start(page);
        var before = RowById(root, 3).ShouldNotBeNull();

        page.Rows[3] = page.Rows[3] with { Text = "Changed" };
        page.Rerender();
        Frame(engine);

        var after = RowById(root, 3).ShouldNotBeNull();
        after.ShouldNotBeSameAs(before);
        after.TextContent.ShouldBe("Changed");
    }

    [Fact]
    public void Should_RenderTheEmptyContent_WhenThereAreNoItems()
    {
        var (_, _, root) = Start(new ListPage
        {
            Rows = new List<Row>(),
            Configure = list => list.EmptyContent = b =>
            {
                var e = b.OpenElement<DivElement>();
                e.Class = "empty";
                b.CloseElement();
            },
        });

        root.FindByClass("empty").ShouldHaveSingleItem();
        RenderedRows(root).ShouldBeEmpty();
    }

    [Fact]
    public void Should_LetTheHostIdle_OnceSettled()
    {
        var (engine, _, _) = Start(new ListPage { Rows = MakeRows(1000) });
        Frame(engine);
        engine.HasPendingVisualWork.ShouldBeFalse();

        Scroll(engine, 1234);
        engine.HasPendingVisualWork.ShouldBeFalse("a scroll settles within its frame");
    }

    [Fact]
    public void Should_CapTheWindowAtMaxItemCount()
    {
        var (_, _, root) = Start(new ListPage
        {
            Rows = MakeRows(1000, _ => 1f),
            Configure = list => list.MaxItemCount = 25,
        });

        RenderedRows(root).Count.ShouldBe(25);
    }

    [Fact]
    public void Should_AccountForContentAboveTheList()
    {
        var (engine, _, root) = Start(new ListPage { Rows = MakeRows(1000), HeaderHeight = 250 });

        // Only 50px of the list are on screen.
        RenderedRows(root).Count.ShouldBeInRange(2, 6);

        Scroll(engine, 250);
        AssertNoGap(root, 1000, "header scrolled away");
        RenderedRows(root)[0].Id.ShouldBe("row-0");
    }

    // ================================================================ heights

    [Fact]
    public void Should_KeepWhatIsOnScreenStill_WhenScrollingUpIntoRowsOfAnotherHeight()
    {
        // Rows 0–499 are 30px, rows from 500 on are 100px; the estimate starts at 50px. Jumping into the
        // middle and scrolling back up renders rows whose real height differs from every estimate.
        var (engine, _, root) = Start(new ListPage
        {
            Rows = MakeRows(1000, i => i < 500 ? 30f : 100f),
            Configure = list => list.ItemSize = 50f,
        });

        Scroll(engine, 40000);
        AssertNoGap(root, 1000, "after the jump");

        for (int step = 0; step < 150; step++)
        {
            var box = ScrollerBox(root);
            if (box.ScrollTop < 600) break;

            var anchor = RenderedRows(root).First(r => r.LayoutBox!.BoxModel.BorderBox.Bottom > box.ScrollTop);
            int id = int.Parse(anchor.Id!["row-".Length..]);
            float before = ScreenTop(root, anchor);

            Scroll(engine, -20);

            AssertNoGap(root, 1000, $"up, step {step}");
            if (RowById(root, id) is { } same)
                ScreenTop(root, same).ShouldBe(before + 20f, 0.5f, $"row {id} must move with the finger only (step {step})");
        }
    }

    [Fact]
    public void Should_MeasureRowsOfDifferentHeights()
    {
        var (engine, _, root) = Start(new ListPage { Rows = MakeRows(100, i => 20f + (i % 5) * 20f) });

        // Scroll to the bottom in steps so every row is rendered and measured once.
        for (int i = 0; i < 100; i++) Scroll(engine, 200);

        var box = ScrollerBox(root);
        float exact = Enumerable.Range(0, 100).Sum(i => 20f + (i % 5) * 20f);
        box.ScrollableContentHeight.ShouldBe(exact, 1f, "every row measured: the scroll height is exact");
        RowById(root, 99).ShouldNotBeNull();
        (box.ScrollTop + box.BoxModel.PaddingBox.Height).ShouldBe(exact, 1f, "scrolled to the very bottom");
    }

    // ================================================================ keys

    [Fact]
    public void Should_KeepWhatIsOnScreenStill_WhenRowsAreInsertedAbove_WithItemKey()
    {
        var page = new ListPage
        {
            Rows = MakeRows(1000),
            Configure = list => list.ItemKey = row => row.Id,
        };
        var (engine, _, root) = Start(page);
        Scroll(engine, 8015);
        var anchor = RenderedRows(root).First(r => r.LayoutBox!.BoxModel.BorderBox.Bottom > ScrollerBox(root).ScrollTop);
        int id = int.Parse(anchor.Id!["row-".Length..]);
        float before = ScreenTop(root, anchor);

        page.Rows.InsertRange(0, MakeRows(10, firstId: 5000));
        page.Rerender();
        Frame(engine);

        var same = RowById(root, id).ShouldNotBeNull("the row on screen is still rendered");
        ScreenTop(root, same).ShouldBe(before, 0.5f, "inserting rows above must not move what is on screen");
        AssertNoGap(root, 1010, "after the insert");
    }

    [Fact]
    public void Should_FollowRowsAppendedRemovedAndInsertedBelow_WithItemKey()
    {
        var page = new ListPage
        {
            Rows = MakeRows(1000, i => 20f + (i % 3) * 20f),
            Configure = list => list.ItemKey = row => row.Id,
        };
        var (engine, _, root) = Start(page);
        Scroll(engine, 9000);
        var anchor = RenderedRows(root).First(r => r.LayoutBox!.BoxModel.BorderBox.Bottom > ScrollerBox(root).ScrollTop);
        int id = int.Parse(anchor.Id!["row-".Length..]);
        float before = ScreenTop(root, anchor);

        void Check(string when, int total)
        {
            ScreenTop(root, RowById(root, id).ShouldNotBeNull(when)).ShouldBe(before, 0.5f, when);
            AssertNoGap(root, total, when);
        }

        page.Rows.AddRange(MakeRows(50, firstId: 1000));      // appended (the tail path)
        page.Rerender();
        Frame(engine);
        Check("after appending", 1050);

        page.Rows.RemoveRange(1020, 30);                       // removed from the end
        page.Rerender();
        Frame(engine);
        Check("after removing from the end", 1020);

        page.Rows.Insert(900, new Row(5000, 40f, "Inserted")); // inserted below the screen
        page.Rerender();
        Frame(engine);
        Check("after inserting below", 1021);

        Scroll(engine, 100_000);
        RenderedRows(root)[^1].Id.ShouldBe("row-1019", "the last row is the last item");
    }

    [Fact]
    public void Should_KeepTheListScrolled_WhenTheParentRerendersAndARowChangesShape()
    {
        // The engine carries a scroll offset to a re-rendered scroller only while it judges the content
        // unchanged, comparing the old and new element trees. A page re-render replaces the scroller and
        // every row; one visible row now holds a <p> instead of a <span>, and with equally many rows the
        // old ones are no longer an ordered subsequence of the new. The list is still the same list.
        var page = new NestedPage { Rows = MakeRows(1000) };
        var root = page.Build();
        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(root, new List<StyleSheet>(), _surface.Canvas, W, H);

        // The panel is at y = 50 … 250.
        engine.ScrollBy(W / 2f, 150, 0, 2000).ShouldBeTrue();
        Frame(engine);
        var panel = root.FindByClass("panel")[0].LayoutBox!;
        panel.ScrollTop.ShouldBe(2000f, 0.5f);

        page.ParagraphRows.Add(52);
        page.Rerender();
        Frame(engine);

        root.FindByClass("panel")[0].LayoutBox!.ScrollTop.ShouldBe(2000f, 0.5f,
            "a row changing shape must not send the list back to the top");
        root.FindById("row-52").ShouldNotBeNull().Children[0].ShouldBeOfType<ParagraphElement>(
            "the row must have changed shape for this to prove anything");
    }

    private sealed class NestedPage : ComponentBase
    {
        public List<Row> Rows { get; set; } = new();
        public HashSet<int> ParagraphRows { get; } = new();

        public void Rerender() => NotifyStateChanged();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var outer = builder.OpenElement<DivElement>();
            outer.Class = "page";
            outer.Style = new Style { Height = Length.Px(H), OverflowY = Overflow.Auto };

            var top = builder.OpenElement<DivElement>();
            top.Style = new Style { Height = Length.Px(50) };
            builder.CloseElement();

            var panel = builder.OpenElement<DivElement>();
            panel.Class = "panel";
            panel.Style = new Style { Height = Length.Px(200), OverflowY = Overflow.Auto };
            var list = builder.OpenComponent<Virtualize<Row>>();
            list.Items = Rows;
            list.ChildContent = row => b =>
            {
                var element = b.OpenElement<DivElement>();
                element.Id = $"row-{row.Id}";
                element.Class = "row";
                element.Style = new Style { Height = Length.Px(40) };
                if (ParagraphRows.Contains(row.Id)) b.OpenElement<ParagraphElement>(); else b.OpenElement<SpanElement>();
                b.AddContent(row.Text);
                b.CloseElement();
                b.CloseElement();
            };
            builder.CloseComponent();
            builder.CloseElement();

            var bottom = builder.OpenElement<DivElement>();
            bottom.Style = new Style { Height = Length.Px(1000) };
            builder.CloseElement();

            builder.CloseElement();
        }
    }

    // ================================================================ placement

    [Fact]
    public void Should_NotRender_WhileUnderADisplayNoneAncestor()
    {
        var page = new ListPage { Rows = MakeRows(1000) };
        var wrapper = new DivElement { Style = new Style { Display = Display.None } };
        var root = new DivElement();
        root.AddChild(wrapper);
        wrapper.AddChild(page.Build());
        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(root, new List<StyleSheet>(), _surface.Canvas, W, H);

        RenderedRows(root).ShouldBeEmpty("a hidden list renders nothing");

        wrapper.Style = new Style { Display = Display.Block };
        Frame(engine);
        RenderedRows(root).Count.ShouldBeInRange(8, 12);
    }

    [Fact]
    public void Should_WorkAsTheEngineRoot()
    {
        var list = new Virtualize<Row> { Items = MakeRows(500) };
        list.ChildContent = row => builder =>
        {
            var e = builder.OpenElement<DivElement>();
            e.Class = "row";
            e.Id = $"row-{row.Id}";
            e.Style = new Style { Height = Length.Px(40) };
            builder.CloseElement();
        };
        var root = list.Build();
        var engine = new MikoEngineBuilder().Build();
        engine.Initialize(root, new List<StyleSheet>(), _surface.Canvas, W, H);

        // No scroll container: the viewport is all there is to see.
        var rows = root.FindByClass("row");
        rows.Count.ShouldBeInRange(8, 12);
        rows[0].Id.ShouldBe("row-0");
    }

    // ================================================================ items provider

    private sealed class Provider
    {
        public List<(ItemsProviderRequest Request, TaskCompletionSource<ItemsProviderResult<Row>> Result)> Calls { get; } = new();
        public int Total { get; set; } = 10_000;

        public ValueTask<ItemsProviderResult<Row>> Load(ItemsProviderRequest request)
        {
            var tcs = new TaskCompletionSource<ItemsProviderResult<Row>>();
            Calls.Add((request, tcs));
            return new ValueTask<ItemsProviderResult<Row>>(tcs.Task);
        }

        public void Complete(int call)
        {
            var (request, tcs) = Calls[call];
            int count = Math.Max(0, Math.Min(request.Count, Total - request.StartIndex));
            tcs.SetResult(new ItemsProviderResult<Row>(MakeRows(count, firstId: request.StartIndex), Total));
        }
    }

    [Fact]
    public void Should_LoadTheVisibleRangeFromTheProvider_AndShowPlaceholdersMeanwhile()
    {
        var provider = new Provider();
        var page = new ListPage
        {
            Configure = list =>
            {
                list.Items = null;
                list.ItemsProvider = provider.Load;
                list.ItemSize = 40f;
                list.Placeholder = context => b =>
                {
                    var e = b.OpenElement<DivElement>();
                    e.Class = "placeholder";
                    e.Style = new Style { Height = Length.Px(40) };
                    b.CloseElement();
                };
            },
        };
        var (engine, _, root) = Start(page);

        var first = provider.Calls.ShouldHaveSingleItem();
        first.Request.StartIndex.ShouldBe(0);
        first.Request.Count.ShouldBeGreaterThanOrEqualTo(8);

        provider.Complete(0);
        Frame(engine);
        RenderedRows(root).Count.ShouldBeInRange(8, 12);
        ScrollerBox(root).ScrollableContentHeight.ShouldBe(10_000 * 40f, 1f);

        Scroll(engine, 200_000);
        var second = provider.Calls[^1];
        second.Request.StartIndex.ShouldBeInRange(4990, 5000);
        root.FindByClass("placeholder").Count.ShouldBeGreaterThanOrEqualTo(8, "loading: placeholders fill the screen");
        RenderedRows(root).ShouldBeEmpty();

        provider.Complete(provider.Calls.Count - 1);
        Frame(engine);
        root.FindByClass("placeholder").ShouldBeEmpty();
        AssertNoGap(root, 10_000, "loaded");
    }

    [Fact]
    public void Should_CancelARequest_ThatALaterOneSupersedes()
    {
        var provider = new Provider();
        var (engine, _, _) = Start(new ListPage
        {
            Configure = list =>
            {
                list.Items = null;
                list.ItemsProvider = provider.Load;
                list.ItemSize = 40f;
            },
        });
        provider.Complete(0);
        Frame(engine);

        Scroll(engine, 100_000);
        var superseded = provider.Calls[^1];
        Scroll(engine, 100_000);

        superseded.Request.CancellationToken.IsCancellationRequested.ShouldBeTrue();
        provider.Calls[^1].Request.CancellationToken.IsCancellationRequested.ShouldBeFalse();
    }
}
