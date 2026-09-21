using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Miko.Components;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Routing;
using Shouldly;

namespace Miko.Tests.Routing;

/// <summary>
/// 回归测试（ISSUE-142 复审）：页面反复 <c>StateHasChanged</c> 后，被换下的那些代不得存活。
///
/// <para><b>现场症状</b>：Release 构建 + dotMemory 启动 Anime 示例，来回点击
/// <c>IonSegmentButton</c>，G2 从 4 MB 涨到 94 MB，<c>Force GC</c> 放不掉。实测每次点击泄漏
/// 约 730 KB，600 次点击后托管堆 21 MB → 457 MB，堆转储里 16,688 个存活
/// <c>ComputedStyle</c> 占 99.6 MB。</para>
///
/// <para><b>根因</b>：<see cref="RouteView"/> 装配布局时把页面内容根元素交给布局两次——
/// <c>layout.BodyElement = content</c>，以及捕获同一个元素的 <c>layout.Body</c> 闭包——之后
/// 从不清理。布局组件随路由一直存活，于是这两个引用永远攥着<b>第 0 代</b>内容根；页面每次
/// 重渲染都产出全新实例并在旧实例上留下 <c>SupersededBy</c> <b>前向</b>指针，于是第 0 代
/// 之后的每一代——连同每个元素约 6 KB 的 <c>ComputedStyle</c>——全部可达且无法回收。</para>
///
/// <para>与 ISSUE-141 不同：那次是<b>整棵根树被替换</b>（路由导航/热重载）不走拆解，一次导航
/// 泄漏一棵；这次是<b>页面自己重渲染</b>，一次点击泄漏一代，频率高得多。</para>
///
/// <para><b>判定方式</b>：对每代页面根留弱引用，强制压缩回收后要求只剩当前在场的那一代。
/// 不用堆字节数判定——本套件并行运行，那个读数会混入其它用例的分配。</para>
/// </summary>
public class LayoutBodyRetentionTests
{
    /// <summary>
    /// 最近一次被 <see cref="RouteView"/> 创建的布局实例。
    /// <para>测试<b>必须</b>让它保持存活：真实应用里布局组件是组件图的一部分
    /// （堆转储里 <c>Anime.MainLayout</c> 经 <c>IonApp</c> 的 RenderFragment 可达），
    /// 而 <see cref="RouteView.Render"/> 只是把 <c>layout.Build()</c> 的结果返回、不对外暴露
    /// 布局实例。若测试放任它变成垃圾，泄漏链的持有者自己先被回收，回退修复也测不出来。</para>
    /// </summary>
    private static LayoutComponentBase? LastLayout;

    /// <summary>页面每次重渲染都产出形状不同的全新子树（与真实页面切换分段一致）。</summary>
    private sealed class CounterPage : ComponentBase
    {
        /// <summary>
        /// 最近一次被 <see cref="RouteView"/> 创建的实例。页面实例由 RouteView 内部 new，
        /// 测试拿不到它，于是让它自己登记一下——这样测试能驱动<b>真实</b>的 RouteView 装配路径，
        /// 而不是在测试里复刻那几行（复刻会让被测代码与产品代码脱节）。
        /// </summary>
        internal static CounterPage? Last;

        private int _clicks;

        public CounterPage() => Last = this;

        internal void Bump()
        {
            _clicks++;
            StateHasChanged();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var root = builder.OpenElement<DivElement>();
            root.Class = "page";
            for (int i = 0; i < 5; i++)
            {
                var row = builder.OpenElement<DivElement>();
                row.Class = $"row-{_clicks}-{i}";
                builder.AddContent("x");
                builder.CloseElement();
            }
            builder.CloseElement();
        }
    }

    /// <summary>等价 Razor MainLayout：<c>&lt;div class="root"&gt;@Body&lt;/div&gt;</c>。</summary>
    private sealed class RootLayout : LayoutComponentBase
    {
        public RootLayout() => LastLayout = this;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var host = builder.OpenElement<DivElement>();
            host.Class = "root";
            Body?.Invoke(builder);
            builder.CloseElement();
        }
    }

    /// <summary>手写布局：读 <see cref="LayoutComponentBase.BodyElement"/>（非 Razor 路径）。</summary>
    private sealed class BodyElementLayout : LayoutComponentBase
    {
        public BodyElementLayout() => LastLayout = this;

        public override Element Build()
            => new DivElement { Class = "root", Children = { BodyElement! } };
    }

    /// <summary>
    /// 跑一遍「装配布局 + 反复重渲染」，对每一代页面根留弱引用。
    ///
    /// <para>整个会话放在<b>禁止内联</b>的方法里：Release 下栈槽存活到方法结束，内联会把
    /// 某一代的 <see cref="Element"/> 临时变量留在调用方栈上，其 <c>SupersededBy</c> 前向链
    /// 会让此后每一代都可达——那样无论产品是否泄漏，测试都能"测出"泄漏。</para>
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int RunSessionAndCountRetained(Type layoutType, int clicks, List<WeakReference> generations)
    {
        var router = new Router();
        router.MapRoute("/", typeof(CounterPage));
        var services = new ServiceCollection().BuildServiceProvider();

        // 走真实的 RouteView 装配路径（被测的正是它对 BodyElement / Body 的处理）。
        var routeView = new RouteView(router, new NavigationManager(), layoutType, services);
        var rendered = routeView.Render("/");

        var page = CounterPage.Last!;
        var layout = LastLayout!;
        CounterPage.Last = null;   // 静态字段不得留住任何一代
        LastLayout = null;

        for (int i = 0; i < clicks; i++)
        {
            page.Bump();
            generations.Add(new WeakReference(CurrentPageRoot(rendered)!));
        }

        // **必须在会话仍存活时判定。** 泄漏的形态是「长寿的布局组件攥着第 0 代内容」；
        // 等 routeView / layout / rendered 整体出栈再测，泄漏链自己也成了垃圾，
        // 于是回退修复也测不出来（这一点踩过一次）。
        ForceCompactingCollect();
        int alive = generations.Count(reference => reference.IsAlive);


        GC.KeepAlive(rendered);
        GC.KeepAlive(routeView);
        GC.KeepAlive(page);
        GC.KeepAlive(layout);      // 见 LastLayout 的注释：布局在真实应用里是活的
        return alive;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Element? CurrentPageRoot(Element layoutRoot)
        => Descendants(layoutRoot).FirstOrDefault(e => e.Class == "page");

    private static IEnumerable<Element> Descendants(Element root)
    {
        yield return root;
        foreach (var child in root.Children)
            foreach (var descendant in Descendants(child))
                yield return descendant;
    }

    private static void ForceCompactingCollect()
    {
        for (int i = 0; i < 3; i++)
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    [Theory]
    [InlineData(typeof(RootLayout))]         // Razor 布局：经 @Body 片段拿内容
    [InlineData(typeof(BodyElementLayout))]  // 手写布局：读 BodyElement
    public void PageRerender_ShouldNotRetainReplacedGenerations(Type layoutType)
    {
        const int clicks = 40;
        var generations = new List<WeakReference>();

        int alive = RunSessionAndCountRetained(layoutType, clicks, generations);

        // 只有当前在场的那一代允许存活（它挂在 rendered 树上），之前每一代都必须已被回收。
        alive.ShouldBeLessThanOrEqualTo(1,
            $"{alive}/{generations.Count} 代页面根在强制压缩回收后仍存活 —— " +
            "布局又攥住了第 0 代内容（BodyElement 或 Body 闭包），" +
            "其 SupersededBy 前向链把之后每一代连同各自的 ComputedStyle 全部钉住");
    }
}
