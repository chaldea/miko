using Miko.Hosting;
using Miko.Animation;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Routing;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// ISSUE-141 的回归测试：<b>整棵根树被替换</b>时，旧树里每个组件的清理回调都必须执行。
///
/// <para>组件自己重渲染（<c>ComponentBase.StateHasChanged</c>）会拆掉被换下的子树，但路由导航与
/// 热重载走的是另一条路——<c>MikoInteractionController.Rebuild</c> 从头 <c>BuildRoot()</c> 造一棵
/// 全新的树交给 <see cref="MikoEngine.Initialize"/>，旧树整棵出局。这条路径上原先无人拆解，于是
/// 旧树里组件的 <c>OnDispose</c> 一次都不执行。</para>
///
/// <para>后果不是"少清理一点"而是彻底的内存泄漏：只要有一个组件在 <c>OnParametersSet</c> 里订阅了
/// 应用级服务（<c>Miko.Ionic</c> 的 <c>IonOverlayHost</c> → <c>IonOverlayRegistry.Changed</c> 就是），
/// 这个订阅就让整棵旧树——DOM、布局盒、以及每个元素约 9&#160;KB 的 <c>ComputedStyle</c>——从 DI 单例
/// 持续可达。实测 Anime 示例每导航一次多留一个订阅者，每轮五个路由约 6.4&#160;MB 在强制 gen2
/// 压缩回收后依然存活。</para>
/// </summary>
public class RootTreeDisposalTests
{
    private static List<StyleSheet> Sheets()
    {
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Width = Length.Px(100), Height = Length.Px(50) },
        });
        return new List<StyleSheet> { sheet };
    }

    /// <summary>
    /// 测试用转场（核心库不内置具体效果，效果由组件库实现——见 ISSUE-108）：
    /// 只需一个正时长，让引擎把旧树保留为 leaving 图层。
    /// </summary>
    private sealed class FadeTransition : NavigationTransition
    {
        public override float Duration => 0.3f;
        public override TimingFunction TimingFunction => TimingFunction.Linear;
        public override void Apply(NavigationTransitionContext context, float progress)
            => context.EnteringOpacity = progress;
    }

    /// <summary>造一棵挂了清理回调的树，回调命中时往 <paramref name="disposed"/> 里记名字。</summary>
    private static Element TreeWithDisposeCallbacks(string name, List<string> disposed)
    {
        var root = new DivElement { Class = name };
        var child = new DivElement { Class = $"{name}-child" };
        var grandChild = new DivElement { Class = $"{name}-grandchild" };

        child.AddChild(grandChild);
        root.AddChild(child);

        // DisposeCallback 是 internal 的，测试程序集经 InternalsVisibleTo 可见；
        // 这正是编译器为组件子树落下的那个回调槽。
        root.DisposeCallback = () => disposed.Add(name);
        child.DisposeCallback = () => disposed.Add($"{name}-child");
        grandChild.DisposeCallback = () => disposed.Add($"{name}-grandchild");
        return root;
    }

    [Fact]
    public void Initialize_ReplacingRootTree_ShouldDisposeWholeOldTree()
    {
        var disposed = new List<string>();
        var engine = new MikoEngineBuilder().Build();
        var sheets = Sheets();
        using var surface = SKSurface.Create(new SKImageInfo(200, 200));

        engine.Initialize(TreeWithDisposeCallbacks("old", disposed), sheets, surface.Canvas, 200, 200);
        engine.Render(surface.Canvas);
        disposed.ShouldBeEmpty("树还在场，不该被拆解。");

        // 路由导航/热重载：一棵全新的树整体替换旧树。
        engine.Initialize(new DivElement { Class = "new" }, sheets, surface.Canvas, 200, 200);

        // 整棵旧树——根、子、孙——都必须被拆解，否则其中任意一个订阅都会钉住整棵树。
        disposed.ShouldBe(new[] { "old", "old-child", "old-grandchild" }, ignoreOrder: true);
    }

    [Fact]
    public void Initialize_WithNavigationTransition_ShouldDeferDisposalUntilTransitionEnds()
    {
        var disposed = new List<string>();
        var engine = new MikoEngineBuilder().Build();
        var sheets = Sheets();
        using var surface = SKSurface.Create(new SKImageInfo(200, 200));

        engine.Initialize(TreeWithDisposeCallbacks("old", disposed), sheets, surface.Canvas, 200, 200);
        engine.Render(surface.Canvas);

        // 带转场的导航：旧树还要作为 leaving 图层继续绘制（ISSUE-108），此时拆解会让
        // 组件在画面仍显示它的时候退订。
        var transition = new FadeTransition();
        engine.Initialize(new DivElement { Class = "new" }, sheets, surface.Canvas, 200, 200,
            new NavigationTransitionInfo(transition, NavigationDirection.Forward, "/old", "/new"));

        engine.IsNavigationTransitionActive.ShouldBeTrue();
        disposed.ShouldBeEmpty("leaving 层还在绘制，不能提前拆解。");

        // 推进到转场结束：leaving 层退场，这时才该拆解。
        engine.AdvanceNavigationTransition(0.5f);

        engine.IsNavigationTransitionActive.ShouldBeFalse();
        disposed.ShouldBe(new[] { "old", "old-child", "old-grandchild" }, ignoreOrder: true);
    }

    [Fact]
    public void Initialize_InterruptingTransition_ShouldDisposeLeavingTreeOnce()
    {
        var disposed = new List<string>();
        var engine = new MikoEngineBuilder().Build();
        var sheets = Sheets();
        using var surface = SKSurface.Create(new SKImageInfo(200, 200));

        engine.Initialize(TreeWithDisposeCallbacks("first", disposed), sheets, surface.Canvas, 200, 200);
        engine.Render(surface.Canvas);

        var transition = new FadeTransition();
        engine.Initialize(TreeWithDisposeCallbacks("second", disposed), sheets, surface.Canvas, 200, 200,
            new NavigationTransitionInfo(transition, NavigationDirection.Forward, "/first", "/second"));
        disposed.ShouldBeEmpty();

        // 转场途中再次导航：旧的 leaving 层（first）被丢弃，此刻就该拆解；
        // 而刚被顶下去的 second 成为新的 leaving 层，还不该拆。
        engine.Initialize(new DivElement { Class = "third" }, sheets, surface.Canvas, 200, 200,
            new NavigationTransitionInfo(transition, NavigationDirection.Forward, "/second", "/third"));

        disposed.ShouldBe(new[] { "first", "first-child", "first-grandchild" }, ignoreOrder: true);

        // 收尾后 second 也拆解，且 first 不会被拆第二次（重复退订/重复释放）。
        engine.AdvanceNavigationTransition(0.5f);
        disposed.Count(name => name == "first").ShouldBe(1);
        disposed.ShouldContain("second");
        disposed.ShouldContain("second-child");
        disposed.ShouldContain("second-grandchild");
    }
}
