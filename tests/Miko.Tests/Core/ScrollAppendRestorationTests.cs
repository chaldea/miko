using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 向可滚动容器<b>追加</b>内容后的滚动保持（ion-infinite-scroll 问题 2）。
///
/// <para>ISSUE-092 的恢复要求新旧 DOM 子树<b>结构等价</b>（标签与子节点数量逐层相同），用来把
/// 「同一内容重新渲染」与「整页内容被替换」区分开。但无限滚动的常态正是<b>在末尾追加</b>：
/// 子节点数量变了，严格相等判定不成立，滚动偏移被丢弃，页面弹回顶部。</para>
///
/// <para>追加是「原有内容仍在原位、后面多了一截」，旧偏移依然指向同一批内容，必须保留。
/// 判定因此放宽为<b>子序列等价</b>：旧的每个子树都能在新子节点里按原有顺序找到结构等价的
/// 对应项。之所以不是更简单的「前缀等价」——真实的无限滚动列表末尾还跟着
/// <c>ion-infinite-scroll</c> 哨兵，新行是插在它<b>之前</b>的，前缀比较会在哨兵那一位错位而判负
/// （见 <see cref="InsertingBeforeATrailingSentinel_KeepsScrollPosition"/>）。</para>
///
/// <para>整页替换时旧行在新树里找不到对应项，判定不成立，「切换内容回到顶部」的既有语义保住。
/// 代价是：只要原有行仍按序在场，插入位置无从分辨，前插也会沿用旧偏移
/// （见 <see cref="InsertingContentAnywhere_KeepsOffset_WhenExistingRowsSurvive"/>）——
/// 要真正分辨需要元素身份（key）。这是两害相权取其轻：保留偏移最多让位置偏移几行，
/// 而重置会把用户直接弹回顶部，后者正是本次要修的症状。</para>
/// </summary>
public class ScrollAppendRestorationTests
{
    private const float ViewportW = 600f;
    private const float ViewportH = 300f;

    /// <summary>可滚动列表页：<paramref name="rows"/> 个 50px 行，可选在前面插入 <paramref name="prepend"/> 行。</summary>
    private static DivElement BuildList(int rows, int prepend = 0)
    {
        var scroller = new DivElement
        {
            Class = "inner-scroll",
            Style = new Style
            {
                Width = Length.Px(ViewportW),
                Height = Length.Px(ViewportH),
                OverflowY = Overflow.Auto,
            }
        };

        for (int i = 0; i < prepend; i++)
        {
            scroller.AddChild(new DivElement
            {
                Class = "prepended",
                Style = new Style { Height = Length.Px(50) },
                TextContent = $"New {i}"
            });
        }

        for (int i = 0; i < rows; i++)
        {
            scroller.AddChild(new DivElement
            {
                Class = "item",
                Style = new Style { Height = Length.Px(50) },
                TextContent = $"Item {i}"
            });
        }

        return scroller;
    }

    [Fact]
    public void AppendingRows_ShouldKeepScrollPosition()
    {
        // 这正是无限滚动的场景：滚到底 → 加载出新数据追加到末尾 → 视图不应弹回顶部。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(BuildList(20), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0, "the list must be scrolled before appending");

        // 追加 5 行后重建（示例页 StateHasChanged 的效果）。
        engine.Initialize(BuildList(25), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "appending rows must not reset the scroll position");
    }

    [Fact]
    public void AppendingRows_ShouldKeepScrollPosition_ForNestedScrollContainer()
    {
        // 真实结构里可滚动容器不是根（ion-page > ion-content > .inner-scroll）。
        static DivElement BuildPage(int rows)
        {
            var page = new DivElement
            {
                Class = "ion-page",
                Style = new Style { Width = Length.Px(ViewportW), Height = Length.Px(ViewportH) }
            };
            page.AddChild(BuildList(rows));
            return page;
        }

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(BuildPage(20), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.ScrollBy(300, 150, 0, 9999);
        var scroller = FindByClass(engine.GetCurrentLayout()!, "inner-scroll").ShouldNotBeNull();
        var scrolled = scroller.ScrollTop;
        scrolled.ShouldBeGreaterThan(0);

        engine.Initialize(BuildPage(25), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        FindByClass(engine.GetCurrentLayout()!, "inner-scroll")!.ScrollTop.ShouldBe(scrolled,
            "appending rows must not reset a nested scroll container");
    }

    [Fact]
    public void InsertingBeforeATrailingSentinel_KeepsScrollPosition()
    {
        // 无限滚动的真实结构：列表末尾跟着 ion-infinite-scroll 哨兵，新行插在它之前。
        // 哨兵的子树形状与普通行不同（多套了两层），纯前缀比较会在哨兵那一位错位而重置滚动。
        static DivElement BuildWithSentinel(int rows)
        {
            var scroller = BuildList(rows);

            // 哨兵：<div><div><span/></div></div>，与普通行 <div>text</div> 形状不同。
            var sentinel = new DivElement
            {
                Class = "ion-infinite-scroll",
                Style = new Style { Height = Length.Px(84) }
            };
            var inner = new DivElement();
            inner.AddChild(new SpanElement { TextContent = "Loading..." });
            sentinel.AddChild(inner);
            scroller.AddChild(sentinel);

            return scroller;
        }

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(BuildWithSentinel(20), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);
        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0);

        engine.Initialize(BuildWithSentinel(25), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "rows inserted before the trailing sentinel must not reset the scroll position");
    }

    [Fact]
    public void RemovingRows_ShouldKeepScrollPosition_ClampedToNewRange()
    {
        // 变短也属于「旧树与新树共享前缀」：保留偏移，由夹取逻辑收进新范围即可。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(BuildList(40), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);
        engine.ScrollBy(300, 150, 0, 9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(1700f);

        engine.Initialize(BuildList(20), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        // 20 行 * 50 - 300 视口 = 700 上限。
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(700f,
            "a shortened list must clamp the offset instead of resetting it");
    }

    [Fact]
    public void PrependingRows_KeepsOffset_BecauseRowsAreStructurallyIdentical()
    {
        // 记录既有局限：同构的行（都是 <div>）无法从结构上区分「前插 5 行」与「后追加 5 行」，
        // 只有元素身份（key）才能分辨，而这里没有。因此前插也会沿用旧偏移，视觉上等价于
        // 「停在原像素位置」而非「停在原内容上」。
        //
        // 这是两害相权取其轻：保留偏移最多让位置偏移几行，而重置会把用户直接弹回顶部——
        // 后者正是本次要修的症状。ion-infinite-scroll 默认 position="bottom"（追加），前插
        // （position="top"）本就是少数场景。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(BuildList(20), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);
        engine.ScrollBy(300, 150, 0, 9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(700f);

        engine.Initialize(BuildList(20, prepend: 5), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(700f,
            "the offset is kept; identical rows cannot be told apart structurally");
    }

    [Fact]
    public void InsertingContentAnywhere_KeepsOffset_WhenExistingRowsSurvive()
    {
        // 子序列判定的直接后果：只要原有行还按原顺序在场，插入位置（首/中/尾）都保留偏移。
        // 无限滚动依赖的正是这一点——列表末尾跟着 ion-infinite-scroll 哨兵，新行插在它<b>之前</b>，
        // 属于"中间插入"而非"末尾追加"。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(BuildList(20), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);
        engine.ScrollBy(300, 150, 0, 9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(700f);

        // 首部插入一个形状不同的 banner，原有 20 行原样保留在后面。
        var withBanner = BuildList(0);
        var banner = new DivElement { Style = new Style { Height = Length.Px(50) } };
        banner.AddChild(new SpanElement { TextContent = "Banner" });
        withBanner.AddChild(banner);
        for (int i = 0; i < 20; i++)
        {
            withBanner.AddChild(new DivElement
            {
                Class = "item",
                Style = new Style { Height = Length.Px(50) },
                TextContent = $"Item {i}"
            });
        }

        engine.Initialize(withBanner, new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(700f,
            "existing rows are still present in order, so the offset is kept");
    }

    [Fact]
    public void ReplacingContentEntirely_ShouldResetScrollPosition()
    {
        // ISSUE-092 的既有语义必须保住：整页内容被替换时滚动条回到顶部。
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

        engine.Initialize(BuildList(20), new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);
        engine.ScrollBy(300, 150, 0, 9999);
        engine.GetCurrentLayout()!.ScrollTop.ShouldBeGreaterThan(0);

        // 同一槽位换成形状完全不同的内容。
        var replaced = new DivElement
        {
            Class = "inner-scroll",
            Style = new Style
            {
                Width = Length.Px(ViewportW),
                Height = Length.Px(ViewportH),
                OverflowY = Overflow.Auto,
            }
        };
        for (int i = 0; i < 30; i++)
        {
            var wrapper = new DivElement { Style = new Style { Height = Length.Px(50) } };
            wrapper.AddChild(new SpanElement { TextContent = $"Other {i}" });
            replaced.AddChild(wrapper);
        }

        engine.Initialize(replaced, new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(0f,
            "replacing the content must still reset the scroll position");
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
