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
            builder.OpenElement(0, "div");
            builder.AddContent(1, "1");
            builder.CloseElement();
            builder.OpenElement(2, "div");
            builder.AddContent(3, "2");
            builder.CloseElement();
        }
    }

    // 单根页面：仅一个顶层 div。
    private sealed class OneRootPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "page");
            builder.CloseElement();
        }
    }

    // 等价 MainLayout：<div class="root">@Body</div>
    private sealed class RootLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "root");
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
