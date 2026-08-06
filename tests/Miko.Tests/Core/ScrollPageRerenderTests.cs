using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Layout;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// 页面组件重渲染后的滚动保持（ion-infinite-scroll 问题 2 的真实路径）。
///
/// <para>无限滚动加载完数据后，页面组件会重渲染。<see cref="ComponentBase.StateHasChanged"/>
/// 在页面根（<c>_rootElement.Parent == null</c>）上走 <c>ReplaceElementContent</c> 分支：
/// 保留根元素本身，但<b>整批换掉它的子节点</b>。滚动容器因此是一个<b>全新的 Element 实例</b>，
/// 与旧布局树里的实例不是同一个对象。</para>
///
/// <para>这条路径既不经 <c>Initialize</c>（那是导航/热重载），也不是简单的就地追加，
/// 是本问题的真正现场。</para>
/// </summary>
public class ScrollPageRerenderTests
{
    private const float ViewportW = 600f;
    private const float ViewportH = 300f;

    /// <summary>
    /// 模拟示例页：一个可滚动列表，行数可增长，重渲染时整棵子树被重建。
    /// </summary>
    private sealed class ListPage : ComponentBase
    {
        public int Rows { get; set; } = 20;

        /// <summary>加载更多数据后重渲染，等价于示例页处理器里的 StateHasChanged。</summary>
        public void LoadMore(int count)
        {
            Rows += count;
            NotifyStateChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "inner-scroll");
            builder.AddAttribute(2, "style", new Style
            {
                Width = Length.Px(ViewportW),
                Height = Length.Px(ViewportH),
                OverflowY = Overflow.Auto,
            });

            for (var i = 0; i < Rows; i++)
            {
                builder.OpenElement(100 + i, "div");
                builder.AddAttribute(101 + i, "style", new Style { Height = Length.Px(50) });
                builder.AddContent(102 + i, $"Item {i}");
                builder.CloseElement();
            }

            // 列表末尾的哨兵，对应 ion-infinite-scroll。
            builder.OpenElement(9000, "div");
            builder.AddAttribute(9001, "class", "ion-infinite-scroll");
            builder.AddAttribute(9002, "style", new Style { Height = Length.Px(84) });
            builder.OpenElement(9003, "div");
            builder.AddContent(9004, "Loading...");
            builder.CloseElement();
            builder.CloseElement();

            builder.CloseElement();
        }
    }

    [Fact]
    public void PageRerenderAfterLoadingMore_KeepsScrollPosition()
    {
        var page = new ListPage { Rows = 20 };
        var root = page.Build();

        var engine = new MikoEngine();
        using var surface = SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));
        engine.Initialize(root, new List<StyleSheet>(), surface.Canvas, ViewportW, ViewportH);

        engine.ScrollBy(300, 150, 0, 9999);
        var scrolled = FindScroller(engine).ScrollTop;
        scrolled.ShouldBeGreaterThan(0, "the list must be scrolled before loading more");

        // 加载出 5 行新数据 → 页面重渲染。
        page.LoadMore(5);
        engine.InvalidateElement(root);
        engine.Update(surface.Canvas);

        FindScroller(engine).ScrollTop.ShouldBe(scrolled,
            "loading more rows must not bounce the page back to the top");
    }

    private static LayoutBox FindScroller(MikoEngine engine)
        => Find(engine.GetCurrentLayout()!, "inner-scroll").ShouldNotBeNull();

    private static LayoutBox? Find(LayoutBox box, string className)
    {
        if (box.Element.HasClass(className)) return box;
        foreach (var child in box.Children)
        {
            var found = Find(child, className);
            if (found != null) return found;
        }
        return null;
    }
}
