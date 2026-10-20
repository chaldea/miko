using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Routing;
using Shouldly;

namespace Miko.Tests.Routing;

/// <summary>
/// ISSUE-066 问题1：页面多个顶层元素经 Layout 渲染后，不应在 Layout 根与页面真实顶层元素
/// 之间插入多余的包裹元素。承载多根的透明 FragmentElement 留在 DOM 中作为页面稳定根，
/// 但对布局透明，因此 Layout 的根（如 .root）与页面顶层元素之间不应有不透明 div。
/// </summary>
public class RouteViewFragmentTests
{
    // 多根页面：两个顶层 div，模拟 issue 中的 <div>1</div><div>2</div>。
    private sealed class TwoRootPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement<DivElement>();
            builder.AddContent("1");
            builder.CloseElement();
            builder.OpenElement<DivElement>();
            builder.AddContent("2");
            builder.CloseElement();
        }
    }

    // 单根页面：仅一个顶层 div。
    private sealed class OneRootPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var __e1 = builder.OpenElement<DivElement>();
            __e1.Class = "page";
            builder.CloseElement();
        }
    }

    // 等价 MainLayout：<div class="root">@Body</div>
    private sealed class RootLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var __e2 = builder.OpenElement<DivElement>();
            __e2.Class = "root";
            Body?.Invoke(builder);
            builder.CloseElement();
        }
    }

    private static RouteView CreateRouteView(Router router, Type? layout)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new RouteView(router, new NavigationManager(), layout, services);
    }

    [Fact]
    public void MultiRootPage_WithLayout_HasNoExtraWrapperBetweenRootAndPageElements()
    {
        var router = new Router();
        router.MapRoute<TwoRootPage>("/");
        var routeView = CreateRouteView(router, typeof(RootLayout));

        var tree = routeView.Render("/");

        // 顶层是 Layout 的 <div class="root">。
        tree.ShouldBeOfType<DivElement>();
        tree.Class.ShouldBe("root");

        // .root 下直接是承载多根的透明 fragment，fragment 下是页面的两个真实 div——
        // .root 与页面 div 之间没有不透明 div 包裹层。
        tree.Children.Count.ShouldBe(1);
        var fragment = tree.Children[0].ShouldBeOfType<FragmentElement>();
        fragment.Children.Count.ShouldBe(2);
        fragment.Children[0].TextContent.ShouldBe("1");
        fragment.Children[1].TextContent.ShouldBe("2");

        // 整棵树里只有 Layout 的那一个 div（class=root）；页面没有引入任何额外 div 包裹。
        tree.FindByTagName("div").Count.ShouldBe(3); // root + 页面两个
    }

    [Fact]
    public void SingleRootPage_WithLayout_AttachesDirectlyWithoutFragment()
    {
        var router = new Router();
        router.MapRoute<OneRootPage>("/");
        var routeView = CreateRouteView(router, typeof(RootLayout));

        var tree = routeView.Render("/");

        tree.Class.ShouldBe("root");
        tree.Children.Count.ShouldBe(1);
        // 单根页面没有 fragment 包裹，直接是页面的 div。
        var page = tree.Children[0].ShouldBeOfType<DivElement>();
        page.Class.ShouldBe("page");
        tree.FindByTagName("fragment").ShouldBeEmpty();
    }

    /// <summary>页面经级联参数读取布局提供的值（ISSUE-153 问题 1）。</summary>
    private sealed class CascadeReaderPage : ComponentBase
    {
        internal static string? Seen;

        [CascadingParameter] public string? Theme { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Seen = Theme;
            var __e1 = builder.OpenElement<DivElement>();
            __e1.Class = "page";
            builder.CloseElement();
        }
    }

    /// <summary>等价 <c>&lt;CascadingValue Value="layout-theme"&gt;@Body&lt;/CascadingValue&gt;</c>。</summary>
    private sealed class CascadingLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var __c1 = builder.OpenComponent<CascadingValue<string>>();
            __c1.Value = "layout-theme";
            __c1.ChildContent = Body;
            builder.CloseComponent();
        }
    }

    /// <summary>手写布局：读 <see cref="LayoutComponentBase.BodyElement"/>，并在其外层级联一个值。</summary>
    private sealed class CascadingBodyElementLayout : LayoutComponentBase
    {
        public override Element Build()
        {
            using var _ = CascadingValueSource.Push("hand-written", typeof(string), null);
            return new DivElement { Class = "root", Children = { BodyElement! } };
        }
    }

    /// <summary>
    /// 页面必须在布局的渲染之内构建，才能拿到布局级联的值。RouteView 曾先建页面、后建布局，
    /// 于是 <c>IonApp</c> 级联的弹层上下文永远到不了路由页面（ISSUE-153 问题 1）。
    /// </summary>
    [Theory]
    [InlineData(typeof(CascadingLayout), "layout-theme")]
    [InlineData(typeof(CascadingBodyElementLayout), "hand-written")]
    public void Page_ReceivesValuesCascadedByTheLayout(Type layout, string expected)
    {
        CascadeReaderPage.Seen = null;
        var router = new Router();
        router.MapRoute<CascadeReaderPage>("/");

        var tree = CreateRouteView(router, layout).Render("/");

        CascadeReaderPage.Seen.ShouldBe(expected);
        tree.FindByClass("page").ShouldHaveSingleItem();
    }

    /// <summary>多根页面，可自行重渲染。</summary>
    private sealed class RerenderableTwoRootPage : ComponentBase
    {
        internal static RerenderableTwoRootPage? Last;
        private int _renders;

        public RerenderableTwoRootPage() => Last = this;

        internal void Refresh() => StateHasChanged();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            _renders++;
            var __e1 = builder.OpenElement<DivElement>();
            __e1.Class = $"page-a-{_renders}";
            builder.CloseElement();
            var __e2 = builder.OpenElement<DivElement>();
            __e2.Class = "page-b";
            builder.CloseElement();
        }
    }

    /// <summary>
    /// 等价 <c>IonApp</c>：<c>&lt;div&gt;&lt;CascadingValue&gt;@Body &lt;div class="sibling"/&gt;&lt;/CascadingValue&gt;&lt;/div&gt;</c>
    /// ——页面内容之后，同一个透明组件里还跟着一个兄弟元素（IonOverlayHost 的位置）。
    /// </summary>
    private sealed class SiblingAfterBodyLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var __e1 = builder.OpenElement<DivElement>();
            __e1.Class = "root";
            var __c1 = builder.OpenComponent<CascadingValue<string>>();
            __c1.Value = "x";
            var body = Body;
            __c1.ChildContent = content =>
            {
                body?.Invoke(content);
                var __e2 = content.OpenElement<DivElement>();
                __e2.Class = "sibling";
                content.CloseElement();
            };
            builder.CloseComponent();
            builder.CloseElement();
        }
    }

    /// <summary>
    /// ISSUE-153 问题 2：<see cref="RenderTreeBuilder"/> 遇到第二个顶层元素时复用了已有的片段根，
    /// 把多根页面自己的片段当成了承载容器，兄弟元素于是挂进页面片段，页面一重渲染就被一起换掉。
    /// </summary>
    [Fact]
    public void MultiRootPageRerender_KeepsSiblingsTheLayoutPlacedAfterIt()
    {
        var router = new Router();
        router.MapRoute<RerenderableTwoRootPage>("/");

        var tree = CreateRouteView(router, typeof(SiblingAfterBodyLayout)).Render("/");
        tree.FindByClass("sibling").ShouldHaveSingleItem();

        RerenderableTwoRootPage.Last!.Refresh();

        tree.FindByClass("page-a-2").ShouldHaveSingleItem();
        tree.FindByClass("page-a-1").ShouldBeEmpty();
        tree.FindByClass("sibling").ShouldHaveSingleItem(
            "the page's re-render must not take the layout's sibling element with it");
    }

    [Fact]
    public void MultiRootPage_NoLayout_ReturnsFragmentRoot()
    {
        var router = new Router();
        router.MapRoute<TwoRootPage>("/");
        var routeView = CreateRouteView(router, layout: null);

        var tree = routeView.Render("/");

        // 无 Layout：返回承载多根的 fragment 作为引擎根（布局阶段它充当允许的根包裹）。
        var fragment = tree.ShouldBeOfType<FragmentElement>();
        fragment.Children.Count.ShouldBe(2);
        fragment.Children[0].TextContent.ShouldBe("1");
        fragment.Children[1].TextContent.ShouldBe("2");
    }
}
