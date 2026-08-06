using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Components;
using Miko.Core;
using Miko.Ionic.Components;
using Miko.Platform;
using Miko.Styling;
using Miko.Testing;
using Shouldly;
using SkiaSharp;

namespace Miko.Ionic.Tests.Components;

/// <summary>
/// 端到端复现 <c>issues/ion-infinite-scroll.md</c> 问题 1：真实的
/// <c>IonContent</c> + <c>IonList</c> + <c>IonInfiniteScroll</c> 组合，经 <see cref="MikoEngine.ScrollBy"/>
/// 滚到底部后必须触发 <c>OnInfinite</c>。
///
/// <para>此前 <c>@onscroll</c> 绑在 infinite-scroll 自己的 div 上，而真正滚动的是
/// <c>IonContent</c> 内部的 <c>.inner-scroll</c>；infinite-scroll 是它的<b>后代</b>，不在冒泡链上，
/// 所以处理器一次都不会被调用。这是唯一能防止该 bug 回归的测试。</para>
/// </summary>
public class IonInfiniteScrollIntegrationTests : IDisposable
{
    private const float ViewportW = 400f;
    private const float ViewportH = 600f;

    private readonly SKSurface _surface =
        SKSurface.Create(new SKImageInfo((int)ViewportW, (int)ViewportH));

    public void Dispose() => _surface.Dispose();

    /// <summary>
    /// 承载页：<c>IonContent</c> 里放 <paramref name="rows"/> 行内容（撑出滚动条），
    /// 末尾跟一个 <c>IonInfiniteScroll</c>，与 issue 的示例页结构一致。
    /// </summary>
    private sealed class InfiniteScrollHost : ComponentBase
    {
        [Parameter] public int Rows { get; set; } = 30;
        [Parameter] public string Threshold { get; set; } = "100px";
        [Parameter] public EventCallback<IonInfiniteScrollCustomEvent> OnInfinite { get; set; }


        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            // .ion-content only gets a height from the ion-page flex column (flex:1 against a
            // definite basis); rendering it bare leaves .inner-scroll (absolute, inset 0) at zero
            // height, so nothing would scroll. Mirror the real page structure.
            builder.OpenComponent<IonPage>(0);
            builder.AddComponentParameter(1, nameof(IonPage.ChildContent), (RenderFragment)(page =>
            {
                page.OpenComponent<IonContent>(0);
                page.AddComponentParameter(1, nameof(IonContent.ChildContent), (RenderFragment)(b =>
                {
                    for (var i = 0; i < Rows; i++)
                    {
                        b.OpenElement(100 + i, "div");
                        b.AddAttribute(101 + i, "style", new Style { Height = Length.Px(60) });
                        b.AddContent(102 + i, $"Item {i}");
                        b.CloseElement();
                    }

                    b.OpenComponent<IonInfiniteScroll>(9000);
                    b.AddComponentParameter(9001, nameof(IonInfiniteScroll.Threshold), Threshold);
                    b.AddComponentParameter(9002, nameof(IonInfiniteScroll.OnInfinite), OnInfinite);
                    b.AddComponentParameter(9003, nameof(IonInfiniteScroll.ChildContent), (RenderFragment)(c =>
                    {
                        c.OpenComponent<IonInfiniteScrollContent>(9100);
                        c.AddComponentParameter(9101, nameof(IonInfiniteScrollContent.LoadingText), "Loading...");
                        c.CloseComponent();
                    }));
                    b.CloseComponent();
                }));
                page.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    /// <summary>
    /// Builds the DOM for a host configuration. TestContext.Render pushes the ambient service
    /// scope, so the nested Ionic components resolve IPlatformInfo (and therefore their mode).
    /// Only the built DOM is used — the assertions run against a real MikoEngine, which is what
    /// drives scrolling.
    /// </summary>
    private Element BuildTree(InfiniteScrollHost host)
    {
        using var context = new TestContext
        {
            ViewportWidth = ViewportW,
            ViewportHeight = ViewportH,
        };
        context.Services.AddSingleton<IPlatformInfo>(new PlatformInfo(HostPlatform.Android));
        return context.Render<InfiniteScrollHost>(p =>
        {
            p.Add(nameof(InfiniteScrollHost.Rows), host.Rows);
            p.Add(nameof(InfiniteScrollHost.Threshold), host.Threshold);
            p.Add(nameof(InfiniteScrollHost.OnInfinite), host.OnInfinite);
        }).Root;
    }

    private MikoEngine BuildAndInitialize(InfiniteScrollHost host)
    {
        var root = BuildTree(host);

        var engine = new MikoEngine();
        engine.Initialize(
            root,
            new List<StyleSheet> { IonicStyleSheetFactory.CreateAllModes() },
            _surface.Canvas,
            ViewportW,
            ViewportH);

        return engine;
    }

    [Fact]
    public void ScrollingToTheBottom_InvokesOnInfinite()
    {
        var invoked = 0;
        var host = new InfiniteScrollHost
        {
            Rows = 30,
            OnInfinite = EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, _ => invoked++),
        };

        var engine = BuildAndInitialize(host);

        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 99999).ShouldBeTrue();

        invoked.ShouldBe(1, "scrolling to the bottom must invoke OnInfinite");
    }

    [Fact]
    public void ScrollingNearTheTop_DoesNotInvokeOnInfinite()
    {
        var invoked = 0;
        var host = new InfiniteScrollHost
        {
            Rows = 30,
            OnInfinite = EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, _ => invoked++),
        };

        var engine = BuildAndInitialize(host);

        // 内容 1884px、视口 600px，滚到 100px 时距底部还有 ~1184px，远超 100px 阈值。
        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 100).ShouldBeTrue();

        invoked.ShouldBe(0, "scrolling near the top must not trigger the infinite scroll");
    }

    [Fact]
    public void ScrollingToTheBottom_StampsLoadingClass_AndCompleteClearsIt()
    {
        IonInfiniteScrollCustomEvent? captured = null;
        var host = new InfiniteScrollHost
        {
            Rows = 30,
            OnInfinite = EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, e => captured = e),
        };

        var engine = BuildAndInitialize(host);
        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 99999);

        captured.ShouldNotBeNull();

        // StateHasChanged rebuilds the subtree, so the element must be re-queried after each
        // state change rather than held across one.
        FindByClass(engine, "ion-infinite-scroll").ShouldHaveSingleItem()
            .ShouldHaveClass("infinite-scroll-loading");

        captured!.Complete();

        FindByClass(engine, "ion-infinite-scroll").ShouldHaveSingleItem()
            .ShouldNotHaveClass("infinite-scroll-loading");
    }

    [Fact]
    public void OnInfinite_FiresOnceUntilCompleted_ThenAgainAfterMoreContent()
    {
        var invoked = 0;
        IonInfiniteScrollCustomEvent? captured = null;
        var host = new InfiniteScrollHost
        {
            Rows = 30,
            OnInfinite = EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, e =>
            {
                invoked++;
                captured = e;
            }),
        };

        var engine = BuildAndInitialize(host);

        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 99999);
        invoked.ShouldBe(1);

        // 仍在阈值带内继续滚动：didFire 去重，不应重复触发。
        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 50);
        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 50);
        invoked.ShouldBe(1, "didFire must suppress repeat emissions until Complete()");

        // Complete() 之后重新武装。Complete() 通过 StateHasChanged 重建了子树，引擎要跑一帧
        // 才会接管新的布局树——真实宿主每帧都会做这件事。
        captured!.Complete();
        engine.Update(_surface.Canvas);

        // 此时已在底部，继续向下滚动不会产生位移（ScrollBy 返回 false，不派发事件），
        // 因此先滚回上方再滚到底，模拟「加载出新内容后继续下拉」。
        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, -900).ShouldBeTrue();
        invoked.ShouldBe(1, "scrolling back up must not emit");

        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 99999).ShouldBeTrue();
        invoked.ShouldBe(2, "Complete() must re-arm the infinite scroll");
    }

    [Fact]
    public void LoadingMoreRows_KeepsTheScrollPosition()
    {
        // issue 问题 2：滚到底触发加载后，新数据渲染出来时页面弹回了顶部。
        // 处理器追加数据后页面会重新渲染，滚动偏移必须保住。
        IonInfiniteScrollCustomEvent? captured = null;
        var host = new InfiniteScrollHost
        {
            Rows = 30,
            OnInfinite = EventCallback.Factory.Create<IonInfiniteScrollCustomEvent>(this, e => captured = e),
        };

        var engine = BuildAndInitialize(host);
        engine.ScrollBy(ViewportW / 2, ViewportH / 2, 0, 99999);

        var scrolled = FindScroller(engine).ScrollTop;
        scrolled.ShouldBeGreaterThan(0);
        captured.ShouldNotBeNull();

        // 加载出 5 行新数据后重新渲染整页（示例页 StateHasChanged 的效果）。
        captured!.Complete();
        var grown = BuildTree(new InfiniteScrollHost { Rows = 35, OnInfinite = host.OnInfinite });
        engine.Initialize(
            grown,
            new List<StyleSheet> { IonicStyleSheetFactory.CreateAllModes() },
            _surface.Canvas,
            ViewportW,
            ViewportH);

        FindScroller(engine).ScrollTop.ShouldBe(scrolled,
            "loading more rows must not bounce the page back to the top");
    }

    private static Miko.Layout.LayoutBox FindScroller(MikoEngine engine)
        => FindLayout(engine.GetCurrentLayout()!, "inner-scroll").ShouldNotBeNull();

    private static Miko.Layout.LayoutBox? FindLayout(Miko.Layout.LayoutBox box, string className)
    {
        if (box.Element.HasClass(className)) return box;
        foreach (var child in box.Children)
        {
            var found = FindLayout(child, className);
            if (found != null) return found;
        }
        return null;
    }

    private static List<Element> FindByClass(MikoEngine engine, string className)
    {
        var result = new List<Element>();
        Walk(engine.GetCurrentLayout()!.Element, className, result);
        return result;
    }

    private static void Walk(Element element, string className, List<Element> result)
    {
        if (element.HasClass(className)) result.Add(element);
        foreach (var child in element.Children) Walk(child, className, result);
    }
}
