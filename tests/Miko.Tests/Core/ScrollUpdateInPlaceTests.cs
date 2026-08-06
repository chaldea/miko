using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 就地修改 DOM 后经 <see cref="MikoEngine.Update"/> 重排时的滚动保持
/// （ion-infinite-scroll 问题 2 的真实路径）。
///
/// <para>组件的 <c>StateHasChanged</c> 不会置 <c>_needsRebuild</c>——那只由导航与热重载触发。
/// 它直接<b>就地改写</b>现有 DOM，再由下一帧 <see cref="MikoEngine.Update"/> 重新布局。因此
/// 无限滚动加载新数据走的是这条路径，而非 <see cref="MikoEngine.Initialize"/>。</para>
///
/// <para>这条路径上 <c>oldLayout</c> 与新布局树指向的往往是<b>同一批 Element 实例</b>
/// （只是多挂了几个子节点），据此判断「内容是否被替换」的依据与 Initialize 路径不同。</para>
/// </summary>
public class ScrollUpdateInPlaceTests
{
    private const float ViewportW = 600f;
    private const float ViewportH = 300f;

    private static DivElement NewRow(int i) => new()
    {
        Class = "item",
        Style = new Style { Height = Length.Px(50) },
        TextContent = $"Item {i}"
    };

    private static DivElement BuildScroller(int rows)
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
        for (int i = 0; i < rows; i++) scroller.AddChild(NewRow(i));
        return scroller;
    }

    [Fact]
    public void AppendingRowsInPlace_ThenUpdate_KeepsScrollPosition()
    {
        // 无限滚动的真实路径：处理器就地把新行加进列表，下一帧 Update 重排。
        var scroller = BuildScroller(20);
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));
        engine.Initialize(scroller, new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0);

        // 就地追加 5 行并失效（组件 StateHasChanged 的效果）。
        for (int i = 20; i < 25; i++) scroller.AddChild(NewRow(i));
        engine.InvalidateElement(scroller);

        engine.Update(surface.Canvas);

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "appending rows in place must not reset the scroll position");
    }

    [Fact]
    public void AppendingBeforeTrailingSentinelInPlace_ThenUpdate_KeepsScrollPosition()
    {
        // 与真实结构一致：列表末尾跟着 ion-infinite-scroll 哨兵，新行插在它之前。
        var scroller = BuildScroller(20);
        var sentinel = new DivElement
        {
            Class = "ion-infinite-scroll",
            Style = new Style { Height = Length.Px(84) }
        };
        var inner = new DivElement();
        inner.AddChild(new SpanElement { TextContent = "Loading..." });
        sentinel.AddChild(inner);
        scroller.AddChild(sentinel);

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));
        engine.Initialize(scroller, new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;
        scrolled.ShouldBeGreaterThan(0);

        // 在哨兵之前插入 5 行。
        for (int i = 20; i < 25; i++) scroller.Children.Insert(scroller.Children.Count - 1, NewRow(i));
        engine.InvalidateElement(scroller);

        engine.Update(surface.Canvas);

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled,
            "rows inserted before the sentinel must not reset the scroll position");
    }

    [Fact]
    public void MutatingRowTextInPlace_ThenUpdate_KeepsScrollPosition()
    {
        // 既有行为回归：只改文本不动结构时，滚动同样必须保持。
        var scroller = BuildScroller(20);
        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));
        engine.Initialize(scroller, new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = engine.GetCurrentLayout()!.ScrollTop;

        scroller.Children[0].TextContent = "Changed";
        engine.InvalidateElement(scroller);
        engine.Update(surface.Canvas);

        engine.GetCurrentLayout()!.ScrollTop.ShouldBe(scrolled);
    }
}
