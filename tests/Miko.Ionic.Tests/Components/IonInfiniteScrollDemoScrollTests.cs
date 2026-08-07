using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Ionic.Components;
using Miko.Layout;
using Miko.Platform;
using Miko.Styling;
using Miko.Testing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// 加载更多数据后滚动位置不得回到顶部（<c>issues/ion-infinite-scroll.md</c> 问题 2），
/// 按示例页 <c>examples/Ionic/IonicDemo/Pages/InfiniteScrollPage.razor</c> 的<b>真实结构</b>验证。
///
/// <para>结构本身就是本 bug 的关键：</para>
/// <code>
/// .inner-scroll          ← 可滚动容器，子节点<b>恒为 2 个</b>
///   ├─ ion-list          ← 行数变化发生在这里（下一层）
///   │    └─ ion-item*
///   └─ ion-infinite-scroll
/// </code>
/// <para>滚动容器自己的子节点数从不改变，因此「只在容器这一层放宽、内部退回严格结构等价」的
/// 判定会在 <c>ion-list</c> 的子节点数上判负，偏移被丢弃、页面弹回顶部。用扁平的 div 列表
/// 复现不出来——必须保留这层嵌套。</para>
/// </summary>
public class IonInfiniteScrollDemoScrollTests : IDisposable
{
    private const float ViewportW = 390f;
    private const float ViewportH = 844f;

    private readonly SKSurface _surface =
        SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

    public void Dispose() => _surface.Dispose();

    /// <summary>示例页的结构：IonPage > IonContent > (IonList > IonItem*) + IonInfiniteScroll。</summary>
    private sealed class DemoPage : ComponentBase
    {
        [Parameter] public int Rows { get; set; } = 15;

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<IonPage>(0);
            b.AddComponentParameter(1, nameof(IonPage.ChildContent), (RenderFragment)(page =>
            {
                page.OpenComponent<IonContent>(10);
                page.AddComponentParameter(11, nameof(IonContent.Fullscreen), true);
                page.AddComponentParameter(12, nameof(IonContent.ChildContent), (RenderFragment)(c =>
                {
                    c.OpenComponent<IonList>(20);
                    c.AddComponentParameter(21, nameof(IonList.ChildContent), (RenderFragment)(l =>
                    {
                        for (var i = 0; i < Rows; i++)
                        {
                            l.OpenComponent<IonItem>(1000 + i * 10);
                            l.AddComponentParameter(1001 + i * 10, nameof(IonItem.ChildContent), (RenderFragment)(item =>
                            {
                                item.OpenComponent<IonLabel>(1);
                                item.AddComponentParameter(2, nameof(IonLabel.ChildContent), (RenderFragment)(lab =>
                                {
                                    lab.OpenElement(1, "h2");
                                    lab.AddContent(2, "Name");
                                    lab.CloseElement();
                                    lab.OpenElement(3, "p");
                                    lab.AddContent(4, "Created");
                                    lab.CloseElement();
                                }));
                                item.CloseComponent();
                            }));
                            l.CloseComponent();
                        }
                    }));
                    c.CloseComponent();

                    c.OpenComponent<IonInfiniteScroll>(9000);
                    c.AddComponentParameter(9001, nameof(IonInfiniteScroll.Threshold), "100px");
                    c.AddComponentParameter(9002, nameof(IonInfiniteScroll.ChildContent), (RenderFragment)(s =>
                    {
                        s.OpenComponent<IonInfiniteScrollContent>(1);
                        s.AddComponentParameter(2, nameof(IonInfiniteScrollContent.LoadingSpinner), "bubbles");
                        s.AddComponentParameter(3, nameof(IonInfiniteScrollContent.LoadingText), "Loading more data...");
                        s.CloseComponent();
                    }));
                    c.CloseComponent();
                }));
                page.CloseComponent();
            }));
            b.CloseComponent();
        }
    }

    private Element BuildPage(int rows)
    {
        using var context = new TestContext { ViewportWidth = ViewportW, ViewportHeight = ViewportH };
        context.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(HostPlatform.Android));
        return context.Render<DemoPage>(p => p.Add(nameof(DemoPage.Rows), rows)).Root;
    }

    private void Initialize(MikoEngine engine, Element root)
        => engine.Initialize(
            root,
            new List<StyleSheet> { IonicStyleSheetFactory.CreateAllModes() },
            _surface.Canvas,
            ViewportW,
            ViewportH);

    [Fact]
    public void LoadingMoreRows_MustNotResetScrollToTop()
    {
        var engine = new MikoEngine();
        Initialize(engine, BuildPage(15));

        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 99999);
        var scrolled = Scroller(engine).ScrollTop;
        scrolled.ShouldBeGreaterThan(0, "the list must be scrolled to the bottom first");

        // 加载出 5 行新数据后页面重建。
        Initialize(engine, BuildPage(20));

        Scroller(engine).ScrollTop.ShouldBe(scrolled,
            "loading more rows must not bounce the page back to the top");
    }

    [Fact]
    public void LoadingMoreRowsRepeatedly_KeepsScrollPosition()
    {
        // 连续加载多批，模拟用户一路往下刷。
        var engine = new MikoEngine();
        Initialize(engine, BuildPage(15));

        float previous = 0;
        for (var rows = 20; rows <= 35; rows += 5)
        {
            engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 99999);
            var scrolled = Scroller(engine).ScrollTop;
            scrolled.ShouldBeGreaterThan(previous, "each batch adds more scrollable content");

            Initialize(engine, BuildPage(rows));

            Scroller(engine).ScrollTop.ShouldBe(scrolled, $"batch of {rows} rows must keep the offset");
            previous = scrolled;
        }
    }

    [Fact]
    public void ReplacingThePageEntirely_StillResetsScroll()
    {
        // 既有语义回归：换成结构完全不同的页面时，滚动仍应回到顶部。
        var engine = new MikoEngine();
        Initialize(engine, BuildPage(15));

        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 99999);
        Scroller(engine).ScrollTop.ShouldBeGreaterThan(0);

        using var context = new TestContext { ViewportWidth = ViewportW, ViewportHeight = ViewportH };
        context.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(HostPlatform.Android));
        var other = context.Render<OtherPage>().Root;

        Initialize(engine, other);

        var scroller = FindByClass(engine.GetCurrentLayout()!, "inner-scroll");
        (scroller?.ScrollTop ?? 0f).ShouldBe(0f,
            "navigating to a structurally different page must reset the scroll");
    }

    /// <summary>结构与 <see cref="DemoPage"/> 完全不同的另一页。</summary>
    private sealed class OtherPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<IonPage>(0);
            b.AddComponentParameter(1, nameof(IonPage.ChildContent), (RenderFragment)(page =>
            {
                page.OpenComponent<IonContent>(10);
                page.AddComponentParameter(11, nameof(IonContent.ChildContent), (RenderFragment)(c =>
                {
                    c.OpenElement(20, "p");
                    c.AddContent(21, "A completely different page.");
                    c.CloseElement();
                }));
                page.CloseComponent();
            }));
            b.CloseComponent();
        }
    }

    private static LayoutBox Scroller(MikoEngine engine)
        => FindByClass(engine.GetCurrentLayout()!, "inner-scroll").ShouldNotBeNull();

    private static LayoutBox? FindByClass(LayoutBox box, string className)
    {
        if (box.Element.HasClass(className)) return box;
        foreach (var child in box.Children)
        {
            var found = FindByClass(child, className);
            if (found != null) return found;
        }
        return null;
    }
}
