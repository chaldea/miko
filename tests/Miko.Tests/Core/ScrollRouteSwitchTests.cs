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
/// 分栏布局中<b>切换路由</b>时内容区必须回到顶部（ISSUE-120）。
///
/// <para>现场是 <c>examples/Ionic/IonicComponents</c>：<c>.sidebar</c> + <c>.main-content</c>
/// 的左右布局，<c>@Body</c> 在 <c>.main-content</c> 里被整页替换。从滚到底的 <c>/item</c>
/// 切到 <c>/list</c> 时，新页面直接停在底部——上一页的偏移被继承了过来。</para>
///
/// <para>根因不在快照机制（ISSUE-118 的 <c>ScrollSnapshotStore</c> 只在 Back 方向回放），
/// 而在 ISSUE-092 的同树恢复：<c>IsSamePresentedContent</c> 为了支持无限滚动的「追加」
/// 被放宽成<b>子序列等价</b>，而两个文档页恰好都以 <c>h1 → p → h2 → Playground…</c> 开头，
/// 同构的通用标签让旧页整棵子树都能在新页里按序找到对应项，于是「整页替换」被误判成
/// 「同一批内容重新渲染」，旧偏移被搬了过来。</para>
/// </summary>
public class ScrollRouteSwitchTests
{
    private const float ViewportW = 900f;
    private const float ViewportH = 400f;

    /// <summary>
    /// 一次前进导航（菜单点击）。<c>Forward</c> 不回放 ISSUE-118 的快照，
    /// 因此这些用例考察的纯粹是 ISSUE-092 的同树恢复。
    /// </summary>
    private static NavigationTransitionInfo Nav(string from, string to)
        => new(null, NavigationDirection.Forward, from, to);

    /// <summary>
    /// 文档页：与真实示例同构 —— 标题、若干段落、小标题、若干演示块。
    /// 两个页面的<b>标签序列</b>高度相似，仅块数与文本不同，正是误判的土壤。
    /// </summary>
    private static DivElement BuildDocPage(string title, int paragraphs, int demos)
    {
        var page = new DivElement { Class = "page" };
        page.AddChild(new H1Element { TextContent = title });

        for (int i = 0; i < paragraphs; i++)
        {
            page.AddChild(new ParagraphElement { TextContent = $"{title} paragraph {i}" });
        }

        for (int i = 0; i < demos; i++)
        {
            page.AddChild(new H2Element { TextContent = $"{title} section {i}" });

            // Playground：一个固定高度的演示块。
            var demo = new DivElement
            {
                Class = "playground",
                Style = new Style { Height = Length.Px(200) }
            };
            demo.AddChild(new DivElement { Class = "demo-body", TextContent = $"demo {i}" });
            page.AddChild(demo);
        }

        return page;
    }

    /// <summary>MainLayout：左侧菜单 + 右侧可滚动内容区，页面放进 <c>.main-content</c>。</summary>
    private static DivElement BuildShell(Element page)
    {
        var layout = new DivElement
        {
            Class = "layout",
            Style = new Style
            {
                Display = Display.Flex,
                Width = Length.Px(ViewportW),
                Height = Length.Px(ViewportH),
            }
        };

        var sidebar = new DivElement
        {
            Class = "sidebar",
            Style = new Style
            {
                Width = Length.Px(200),
                Height = Length.Px(ViewportH),
                OverflowY = Overflow.Auto,
            }
        };
        for (int i = 0; i < 40; i++)
        {
            var item = new DivElement { Class = "nav-item", Style = new Style { Height = Length.Px(40) } };
            item.AddChild(new SpanElement { TextContent = $"Ion{i}" });
            sidebar.AddChild(item);
        }
        layout.AddChild(sidebar);

        var main = new DivElement
        {
            Class = "main-content",
            Style = new Style
            {
                Width = Length.Px(ViewportW - 200),
                Height = Length.Px(ViewportH),
                OverflowY = Overflow.Auto,
            }
        };
        main.AddChild(page);
        layout.AddChild(main);

        return layout;
    }

    [Fact]
    public void SwitchingRoute_ShouldResetContentScroll_EvenWhenPagesLookStructurallySimilar()
    {
        // ISSUE-120 主场景：/item(滚到底) → /list，新页面必须从顶部开始。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(
            BuildShell(BuildDocPage("IonItem", paragraphs: 1, demos: 6)),
            new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        // 在右侧内容区滚到底。
        engine.ScrollBy(600, 200, 0, 99999);
        var main = FindByClass(engine.GetCurrentLayout()!, "main-content").ShouldNotBeNull();
        main.ScrollTop.ShouldBeGreaterThan(0, "the content pane must be scrolled before navigating");

        // 切到另一篇文档页（整页替换）。
        engine.Initialize(
            BuildShell(BuildDocPage("IonList", paragraphs: 2, demos: 8)),
            new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH, Nav("/item", "/list"));

        FindByClass(engine.GetCurrentLayout()!, "main-content")!.ScrollTop.ShouldBe(0f,
            "opening a different page must start at the top, not inherit the previous page's offset");
    }

    [Fact]
    public void SwitchingRoute_ShouldKeepSidebarScroll()
    {
        // 与主场景互补：跨导航一直在场的侧栏（内容未变）仍必须保留偏移，
        // 否则「修好内容区」会以牺牲 ISSUE-092 的主场景为代价。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(
            BuildShell(BuildDocPage("IonItem", paragraphs: 1, demos: 6)),
            new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        // 在左侧菜单滚到底。
        engine.ScrollBy(100, 200, 0, 99999);
        var sidebar = FindByClass(engine.GetCurrentLayout()!, "sidebar").ShouldNotBeNull();
        var scrolled = sidebar.ScrollTop;
        scrolled.ShouldBeGreaterThan(0, "the sidebar must be scrolled before navigating");

        engine.Initialize(
            BuildShell(BuildDocPage("IonList", paragraphs: 2, demos: 8)),
            new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH, Nav("/item", "/list"));

        FindByClass(engine.GetCurrentLayout()!, "sidebar")!.ScrollTop.ShouldBe(scrolled,
            "the sidebar is unchanged across navigation and must keep its scroll position");
    }

    [Fact]
    public void SamePathRebuild_ShouldKeepContentScroll_WhenRowsAreAppended()
    {
        // 与主场景对照：路径没变（StateHasChanged / 加载更多）时仍走宽松的子序列判定，
        // 追加内容不得把用户弹回顶部（ion-infinite-scroll 问题 2 的语义必须保住）。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(
            BuildShell(BuildDocPage("IonItem", paragraphs: 1, demos: 6)),
            new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.ScrollBy(600, 200, 0, 99999);
        var scrolled = FindByClass(engine.GetCurrentLayout()!, "main-content")!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0);

        // 同一页追加了两个演示块后重建（路径不变）。
        engine.Initialize(
            BuildShell(BuildDocPage("IonItem", paragraphs: 1, demos: 8)),
            new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH, Nav("/item", "/item"));

        FindByClass(engine.GetCurrentLayout()!, "main-content")!.ScrollTop.ShouldBe(scrolled,
            "appending content within the same page must not reset the scroll position");
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
