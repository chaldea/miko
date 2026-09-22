using Miko.Common;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Routing;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// Tab（根级）切换时各个 Tab 各自保留滚动位置（ISSUE-144 问题3）。
///
/// <para>现场是 <c>examples/App/Anime</c> 的 <c>MainLayout</c>：三个 <c>IonTabButton</c> 通过
/// <c>IonTabs.SelectTab</c> 以 <see cref="NavigationDirection.Root"/> 导航。首页滚到底后切到
/// 「我的」再切回首页，滚动条被重置回顶部。</para>
///
/// <para>根因不是 bug 而是<b>语义缺口</b>：Root 会清空历史栈，因此 ISSUE-118 的快照存储在
/// Root 方向上把<b>全部</b>快照都丢掉了。那对「历史栈快照」是正确的——栈上的页面确实都不再
/// 可返回；但 Tab 切换本身不是「一去不回」，而是在几个根级页面之间来回切。所以 Root 需要
/// 自己一张<b>常驻</b>表，与历史栈那张分开（见 <c>ScrollSnapshotStore</c>）。</para>
///
/// <para>历史栈语义必须保持不变，由 <see cref="ScrollSnapshotRestorationTests"/> 的
/// <c>Root_ShouldDropAllSnapshots</c> 继续守着：Root 之后再 Back 仍然不恢复。</para>
/// </summary>
public class ScrollTabSwitchTests
{
    private const float ViewportW = 600f;
    private const float ViewportH = 300f;

    private static NavigationTransitionInfo Nav(NavigationDirection direction, string from, string to)
        => new(null, direction, from, to);

    /// <summary>
    /// 带页面身份键的导航。真实应用里由 <c>MikoInteractionController</c> 从路由解析填入；
    /// 这里显式给出，用来表达「两个路径是同一个页面组件」。
    /// </summary>
    private static NavigationTransitionInfo Nav(
        NavigationDirection direction, string from, string to, string fromKey, string toKey)
        => new(null, direction, from, to, fromKey, toKey);

    /// <summary>
    /// 一个 Tab 页：可滚动容器内含 <paramref name="rows"/> 个等高行。
    /// <paramref name="itemClass"/> 让两个 Tab 的内容<b>不同</b>，以免测试靠 ISSUE-092 的
    /// 同树恢复侥幸通过——那条路径比较的是标签与树形，与本用例要验证的快照机制无关。
    /// </summary>
    private static DivElement BuildTabPage(int rows, string itemClass)
    {
        var page = new DivElement
        {
            Class = "inner-scroll",
            Style = new Style
            {
                Width = Length.Px(ViewportW),
                Height = Length.Px(ViewportH),
                OverflowY = Overflow.Auto,
            }
        };
        for (int i = 0; i < rows; i++)
        {
            page.AddChild(new DivElement
            {
                Class = itemClass,
                Style = new Style { Height = Length.Px(50) },
                TextContent = $"{itemClass} {i}"
            });
        }
        return page;
    }

    [Fact]
    public void RootSwitch_ShouldRestoreScroll_WhenReturningToTab()
    {
        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        // 首页滚到底。
        engine.Initialize(BuildTabPage(20, "home-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);
        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0);

        // Root 切到「我的」。
        engine.Initialize(BuildTabPage(8, "user-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
            Nav(NavigationDirection.Root, "/home", "/user"));
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0, "a freshly entered tab starts at the top");

        // Root 切回首页：偏移应当回来。
        engine.Initialize(BuildTabPage(20, "home-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
            Nav(NavigationDirection.Root, "/user", "/home"));

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "switching back to a tab must restore that tab's scroll position");
    }

    [Fact]
    public void RootSwitch_ShouldRestoreScroll_OnEveryReturn()
    {
        // Tab 会被反复切回，因此 Root 表不能在回放后被消费——否则只有第一次生效。
        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(BuildTabPage(20, "home-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);
        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0);

        for (int round = 0; round < 3; round++)
        {
            engine.Initialize(BuildTabPage(8, "user-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
                Nav(NavigationDirection.Root, "/home", "/user"));
            engine.Initialize(BuildTabPage(20, "home-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
                Nav(NavigationDirection.Root, "/user", "/home"));

            engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
                $"round {round}: every switch back must restore the tab's scroll position");
        }
    }

    [Fact]
    public void RootSwitch_ShouldRestoreScroll_WhenTabPageHasSeveralRouteTemplates()
    {
        // 一个页面挂多个路由模板是 Razor 的标准写法（Anime 的首页就是
        // `@page "/"` + `@page "/home"`）：应用启动落在 "/"，而 Tab 按钮导航到 "/home"。
        // 若快照按<b>路径</b>存取，这两条路径就是两个键——离开时存在 "/" 下，切回来查
        // "/home" 取不到，滚动条照旧重置（ISSUE-144 问题3 在真机上的残留现场）。
        const string homePage = "Anime.Pages.Home";
        const string userPage = "Anime.Pages.User";

        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        // 启动落在 "/"，滚到底。
        engine.Initialize(BuildTabPage(20, "home-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);
        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0);

        // 切到「我的」——离开的是 "/"。
        engine.Initialize(BuildTabPage(8, "user-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
            Nav(NavigationDirection.Root, "/", "/user", homePage, userPage));

        // 切回首页——Tab 按钮走的是 "/home"，与离开时的 "/" 不是同一个路径，但是同一个页面。
        engine.Initialize(BuildTabPage(20, "home-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
            Nav(NavigationDirection.Root, "/user", "/home", userPage, homePage));

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "a page reachable at several routes must share one scroll snapshot");
    }

    [Fact]
    public void RootSwitch_ShouldNotRestoreScroll_WhenTabWasNeverScrolled()
    {
        // 没滚动过的 Tab 不该凭空获得偏移（也不该继承上一个 Tab 的）。
        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(BuildTabPage(20, "home-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);
        engine.ScrollBy(300, 150, 0, 9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBeGreaterThan(0);

        engine.Initialize(BuildTabPage(20, "user-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
            Nav(NavigationDirection.Root, "/home", "/user"));

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0,
            "a tab that was never scrolled must not inherit another tab's offset");
    }

    [Fact]
    public void RootSwitch_ShouldForgetScroll_WhenTabReturnedToTop()
    {
        // 切回后用户自己滚回顶部，再切走再切回：应当停在顶部，不该回放那张旧快照。
        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(BuildTabPage(20, "home-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);
        engine.ScrollBy(300, 150, 0, 9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBeGreaterThan(0);

        engine.Initialize(BuildTabPage(8, "user-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
            Nav(NavigationDirection.Root, "/home", "/user"));
        engine.Initialize(BuildTabPage(20, "home-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
            Nav(NavigationDirection.Root, "/user", "/home"));

        // 滚回顶部。
        engine.ScrollBy(300, 150, 0, -9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0);

        engine.Initialize(BuildTabPage(8, "user-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
            Nav(NavigationDirection.Root, "/home", "/user"));
        engine.Initialize(BuildTabPage(20, "home-item"), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH,
            Nav(NavigationDirection.Root, "/user", "/home"));

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0,
            "a tab left at the top must not be restored to a stale offset");
    }
}
