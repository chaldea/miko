using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Routing;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 跨「页面被完全销毁再重建」的返回滚动恢复（ISSUE-118）。
///
/// <para>ISSUE-092 的恢复只在<b>上一帧布局树</b>与新布局树之间搬运偏移，因此只覆盖跨重建仍然
/// 存在的稳定容器（分栏布局里的侧栏）。Ionic 默认模板（<c>IonApp</c> + <c>@Body</c>）中被滚动的
/// 是页面自身：push 到下一页时旧页面树连同偏移一起被丢弃，返回时上一帧布局树已经是详情页，
/// 没有可搬运的来源——必须按历史栈路径持久化快照。</para>
///
/// <para>这些用例直接用 <c>Initialize(..., NavigationTransitionInfo)</c> 驱动导航，
/// 无需真实路由与宿主。</para>
/// </summary>
public class ScrollSnapshotRestorationTests
{
    private static NavigationTransitionInfo Nav(NavigationDirection direction, string from, string to)
        => new(null, direction, from, to);

    /// <summary>长列表页：可滚动容器内含 <paramref name="rows"/> 个等高行。</summary>
    private static DivElement BuildListPage(int rows)
    {
        var page = new DivElement
        {
            Class = "inner-scroll",
            Style = new Style
            {
                Width = Length.Px(600),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            }
        };
        for (int i = 0; i < rows; i++)
        {
            page.AddChild(new DivElement
            {
                Class = "ion-item",
                Style = new Style { Height = Length.Px(50) },
                TextContent = $"Item {i}"
            });
        }
        return page;
    }

    /// <summary>短详情页：内容不足一屏，本身不滚动。</summary>
    private static DivElement BuildDetailPage()
    {
        return new DivElement
        {
            Class = "inner-scroll",
            Style = new Style
            {
                Width = Length.Px(600),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            },
            Children =
            {
                new DivElement
                {
                    Class = "ion-accordion",
                    Style = new Style { Height = Length.Px(80) },
                    TextContent = "Accordion"
                }
            }
        };
    }

    [Fact]
    public void Back_ShouldRestoreScrollOfPreviousPage_AfterFullPageReplacement()
    {
        // ISSUE-118 主场景：AppHome 滚到底 → push 详情页 → back 回 AppHome，列表应停在原位。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(600, 300));

        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300);

        // 滚到底。
        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0, "list page should be scrolled");

        // push 进入短详情页：新页面必须从顶部开始。
        engine.Initialize(BuildDetailPage(), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Forward, "/", "/accordion"));
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0, "a pushed page must start at the top");

        // back 返回列表页（页面被重新构建，是一棵全新的树）。
        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Back, "/accordion", "/"));

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "returning to the previous page must restore its scroll position");
    }

    [Fact]
    public void Forward_ShouldNotRestoreScroll_WhenEnteringPage()
    {
        // 压栈进入是一次新的页面访问：即便该路径此前滚动过，也应从顶部开始。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(600, 300));

        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300);
        engine.ScrollBy(300, 150, 0, 9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBeGreaterThan(0);

        // 离开 "/"（拍快照），再以 Forward 方向重新进入 "/"。
        engine.Initialize(BuildDetailPage(), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Forward, "/", "/accordion"));
        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Forward, "/accordion", "/"));

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0,
            "a forward navigation must not restore a previous visit's scroll position");
    }

    [Fact]
    public void Root_ShouldDropAllSnapshots()
    {
        // Root（Tab 切换）清空历史栈，栈上页面都不再可返回，其快照随之作废。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(600, 300));

        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300);
        engine.ScrollBy(300, 150, 0, 9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBeGreaterThan(0);

        // Root 切换到另一个 Tab，随后「返回」到 "/"。
        engine.Initialize(BuildDetailPage(), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Root, "/", "/tabs/games"));
        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Back, "/tabs/games", "/"));

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0,
            "a root switch clears the history stack, so its snapshots must be dropped");
    }

    [Fact]
    public void Back_ShouldClampRestoredScroll_WhenReturnedPageIsShorter()
    {
        // 返回页比离开时更短（如图片尚未加载完）：偏移必须夹取到合法上限，不能越界。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(600, 300));

        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300);
        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBe(20 * 50 - 300);

        engine.Initialize(BuildDetailPage(), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Forward, "/", "/accordion"));

        // 返回时列表只剩 10 行（内容高 500，视口 300 → 上限 200）。
        engine.Initialize(BuildListPage(10), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Back, "/accordion", "/"));

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(10 * 50 - 300,
            "restored scroll must be clamped to the returned page's scrollable range");
    }

    [Fact]
    public void Back_ShouldSkipEntry_WhenStructurePathNoLongerMatches()
    {
        // 返回页的结构与离开时不同（可滚动盒子所在的索引路径上标签名已变）：
        // 宁可从顶部开始，也不要把偏移写到不相干的盒子上。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(600, 300));

        // 离开时，可滚动盒子是根下第 0 个子节点（<div>）。
        var outer = new DivElement { Style = new Style { Width = Length.Px(600), Height = Length.Px(300) } };
        outer.AddChild(BuildListPage(20));
        engine.Initialize(outer, new List<StyleSheet>(), surface.Canvas, 600, 300);
        engine.ScrollBy(300, 150, 0, 9999);
        FindByClass(engine.GetCurrentLayout()!, "inner-scroll")!.ScrollTop.ShouldBeGreaterThan(0);

        engine.Initialize(BuildDetailPage(), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Forward, "/", "/accordion"));

        // 返回时同一位置换成了 <nav>（标签名不符）。
        var newOuter = new DivElement { Style = new Style { Width = Length.Px(600), Height = Length.Px(300) } };
        var replaced = new NavElement
        {
            Class = "inner-scroll",
            Style = new Style
            {
                Width = Length.Px(600),
                Height = Length.Px(300),
                OverflowY = Overflow.Auto,
            }
        };
        for (int i = 0; i < 20; i++)
            replaced.AddChild(new DivElement { Style = new Style { Height = Length.Px(50) } });
        newOuter.AddChild(replaced);

        engine.Initialize(newOuter, new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Back, "/accordion", "/"));

        FindByClass(engine.GetCurrentLayout()!, "inner-scroll")!.ScrollTop.ShouldBe(0,
            "a snapshot entry whose structural path no longer matches must be skipped");
    }

    [Fact]
    public void Back_ShouldRestoreNestedScrollable()
    {
        // 可滚动容器不在根上：索引路径必须逐层对位。
        static DivElement BuildNested()
        {
            var header = new DivElement { Style = new Style { Height = Length.Px(50) }, TextContent = "Header" };
            var body = new DivElement { Style = new Style { Width = Length.Px(600), Height = Length.Px(250) } };
            body.AddChild(BuildListPage(20));

            var page = new DivElement { Style = new Style { Width = Length.Px(600), Height = Length.Px(300) } };
            page.AddChild(header);
            page.AddChild(body);
            return page;
        }

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(600, 300));

        engine.Initialize(BuildNested(), new List<StyleSheet>(), surface.Canvas, 600, 300);
        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = FindByClass(engine.GetCurrentLayout()!, "inner-scroll")!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0, "nested scrollable should be scrolled");

        engine.Initialize(BuildDetailPage(), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Forward, "/", "/accordion"));
        engine.Initialize(BuildNested(), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Back, "/accordion", "/"));

        FindByClass(engine.GetCurrentLayout()!, "inner-scroll")!.ScrollTop.ShouldBe(scrolled,
            "nested scrollable's scroll position should be restored by structural index path");
    }

    [Fact]
    public void Back_ShouldConsumeSnapshot_SoLaterVisitStartsAtTop()
    {
        // 返回即出栈：该历史条目的快照被消费掉，避免快照无限增长，也避免之后
        // 再次「返回」到同一路径时套用一份早已过期的偏移。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(600, 300));

        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300);
        engine.ScrollBy(300, 150, 0, 9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBeGreaterThan(0);

        engine.Initialize(BuildDetailPage(), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Forward, "/", "/accordion"));
        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Back, "/accordion", "/"));
        engine.GetCurrentLayout()!.ScrollTop.ShouldBeGreaterThan(0, "first back restores");

        // 这次离开时列表已在顶部（上一步恢复后未再滚动会保留偏移，故显式滚回顶部）。
        engine.ScrollBy(300, 150, 0, -9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0);

        engine.Initialize(BuildDetailPage(), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Forward, "/", "/accordion"));
        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300,
            Nav(NavigationDirection.Back, "/accordion", "/"));

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0,
            "leaving a page at the top must not resurrect a stale snapshot");
    }

    [Fact]
    public void HotReloadRebuild_ShouldNotTouchSnapshots()
    {
        // 非导航重建（热重载：navigation 为 null）不涉及历史栈，既不拍快照也不回放，
        // 同树内的滚动保持仍由 ISSUE-092 的机制负责。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo(600, 300));

        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300);
        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;

        // 热重载重建同一棵树：ISSUE-092 的结构等价恢复应保留偏移。
        engine.Initialize(BuildListPage(20), new List<StyleSheet>(), surface.Canvas, 600, 300);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "a non-navigation rebuild of the same structure keeps scroll (ISSUE-092)");
    }

    private static LayoutBox? FindByClass(LayoutBox root, string className)
    {
        if (root.Element.HasClass(className)) return root;
        foreach (var child in root.Children)
        {
            var found = FindByClass(child, className);
            if (found != null) return found;
        }
        return null;
    }
}
