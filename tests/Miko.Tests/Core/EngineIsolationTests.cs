using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// ISSUE-129：多引擎实例之间必须互不干扰。
///
/// <para>变更版本号原本是 <c>Element</c> 上的<b>进程级全局静态</b>计数，任何元素的结构/样式/
/// 状态写入都递增它，而 <c>LayoutEngine</c> 用它做布局缓存键。于是一个引擎的 DOM 变更会击穿
/// <b>所有</b>引擎的布局缓存：次级引擎（DevTools 独立窗口、模拟器设置面板）永远无法空闲，
/// 每帧全量样式解析+布局（ISSUE-117 的现场）。</para>
///
/// <para>现在版本号按引擎实例计（<see cref="MikoEngine.Mutations"/>），元素经
/// <c>Element.Owner</c> 找到自己所属的引擎。本类锁住这个隔离性质。</para>
/// </summary>
public class EngineIsolationTests
{
    private static (MikoEngine engine, Element root, List<StyleSheet> sheets) CreateEngine(string marker)
    {
        var root = new DivElement { Class = marker, Children = { new SpanElement { TextContent = "hello" } } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Width = Length.Px(200), Height = Length.Px(100) },
        });

        return (new MikoEngineBuilder().Build(), root, new List<StyleSheet> { sheet });
    }

    /// <summary>渲染到稳态（首帧 + 一次 Render），返回其 surface 供后续帧复用。</summary>
    private static SKSurface RenderToIdle(MikoEngine engine, Element root, List<StyleSheet> sheets)
    {
        var surface = SKSurface.Create(new SKImageInfo(300, 300));
        engine.Initialize(root, sheets, surface.Canvas, 300, 300);
        engine.Render(surface.Canvas);
        return surface;
    }

    // ---- 构造 ------------------------------------------------------------

    [Fact]
    public void Builder_Should_Produce_Independent_Engines_And_Dependencies()
    {
        var a = new MikoEngineBuilder().Build();
        var b = new MikoEngineBuilder().Build();

        a.ShouldNotBeSameAs(b);
        // 依赖也必须各自一份，否则脏区域/动画/变更计数会串台。
        a.Mutations.ShouldNotBeSameAs(b.Mutations);
        a.AnimationManager.ShouldNotBeSameAs(b.AnimationManager);
    }

    // ---- 变更计数隔离 ----------------------------------------------------

    [Fact]
    public void Mutating_One_Engines_Dom_Should_Not_Bump_Another_Engines_Version()
    {
        var (a, aRoot, aSheets) = CreateEngine("a");
        var (b, bRoot, bSheets) = CreateEngine("b");
        using var aSurface = RenderToIdle(a, aRoot, aSheets);
        using var bSurface = RenderToIdle(b, bRoot, bSheets);

        long aBefore = a.Mutations.Version;
        long bBefore = b.Mutations.Version;

        for (int i = 0; i < 5; i++)
            aRoot.AddChild(new SpanElement { TextContent = $"child {i}" });

        a.Mutations.Version.ShouldBeGreaterThan(aBefore);   // 自己的变更照常记账
        b.Mutations.Version.ShouldBe(bBefore);              // 别人的引擎完全不受影响
    }

    [Fact]
    public void Each_Engine_Should_Count_Only_Its_Own_Mutations()
    {
        var (a, aRoot, aSheets) = CreateEngine("a");
        var (b, bRoot, bSheets) = CreateEngine("b");
        using var aSurface = RenderToIdle(a, aRoot, aSheets);
        using var bSurface = RenderToIdle(b, bRoot, bSheets);

        long aBefore = a.Mutations.Version;
        long bBefore = b.Mutations.Version;

        bRoot.Class = "b-changed";

        a.Mutations.Version.ShouldBe(aBefore);
        b.Mutations.Version.ShouldBeGreaterThan(bBefore);
    }

    // ---- 空闲判定隔离（ISSUE-117 的根因）---------------------------------

    [Fact]
    public void One_Engines_Dom_Churn_Should_Not_Keep_Another_Engine_Busy()
    {
        var (a, aRoot, aSheets) = CreateEngine("a");
        var (b, bRoot, bSheets) = CreateEngine("b");
        using var aSurface = RenderToIdle(a, aRoot, aSheets);
        using var bSurface = RenderToIdle(b, bRoot, bSheets);

        b.HasPendingVisualWork.ShouldBeFalse();

        // A 持续变更自己的 DOM —— 全局静态计数的年代，这会让 B 永远无法空闲。
        for (int i = 0; i < 5; i++)
            aRoot.AddChild(new SpanElement { TextContent = $"child {i}" });

        a.HasPendingVisualWork.ShouldBeTrue();    // A 自己确实有活要干
        b.HasPendingVisualWork.ShouldBeFalse();   // B 一个字节都没变，必须保持空闲
    }

    // ---- 布局缓存隔离 ----------------------------------------------------

    [Fact]
    public void One_Engines_Mutation_Should_Not_Invalidate_Another_Engines_Layout_Cache()
    {
        var (a, aRoot, aSheets) = CreateEngine("a");
        var (b, bRoot, bSheets) = CreateEngine("b");
        using var aSurface = RenderToIdle(a, aRoot, aSheets);
        using var bSurface = RenderToIdle(b, bRoot, bSheets);

        var bLayoutBefore = b.GetCurrentLayout();
        bLayoutBefore.ShouldNotBeNull();

        for (int i = 0; i < 5; i++)
            aRoot.AddChild(new SpanElement { TextContent = $"child {i}" });

        // B 重新渲染：输入未变，应命中布局缓存复用同一棵布局树（ISSUE-096 的快速路径）。
        b.Render(bSurface.Canvas);
        b.GetCurrentLayout().ShouldBeSameAs(bLayoutBefore);

        // 对照：B 自己变了，就必须重排出一棵新的布局树。
        bRoot.AddChild(new SpanElement { TextContent = "b's own child" });
        b.Render(bSurface.Canvas);
        b.GetCurrentLayout().ShouldNotBeSameAs(bLayoutBefore);
    }

    // ---- 引擎内部状态隔离 ------------------------------------------------

    [Fact]
    public void Animations_Started_On_One_Engine_Should_Not_Appear_On_Another()
    {
        var (a, aRoot, aSheets) = CreateEngine("a");
        var (b, bRoot, bSheets) = CreateEngine("b");
        using var aSurface = RenderToIdle(a, aRoot, aSheets);
        using var bSurface = RenderToIdle(b, bRoot, bSheets);

        var animation = new Miko.Animation.KeyframeAnimation
        {
            Name = "fade",
            Duration = 1f,
            Keyframes = new List<Miko.Animation.Keyframe>
            {
                new(0f, new Style { Opacity = 0f }),
                new(1f, new Style { Opacity = 1f }),
            },
        };

        a.StartAnimation(aRoot, animation);

        a.AnimationManager.HasActiveAnimations.ShouldBeTrue();
        b.AnimationManager.HasActiveAnimations.ShouldBeFalse();
    }

    [Fact]
    public void Invalidating_One_Engines_Element_Should_Not_Dirty_Another_Engine()
    {
        var (a, aRoot, aSheets) = CreateEngine("a");
        var (b, bRoot, bSheets) = CreateEngine("b");
        using var aSurface = RenderToIdle(a, aRoot, aSheets);
        using var bSurface = RenderToIdle(b, bRoot, bSheets);

        a.InvalidateElement(aRoot);

        a.HasPendingRenderWork.ShouldBeTrue();
        b.HasPendingRenderWork.ShouldBeFalse();
    }

    [Fact]
    public void AddStyleSheet_On_One_Engine_Should_Not_Disturb_Another()
    {
        var (a, aRoot, aSheets) = CreateEngine("a");
        var (b, bRoot, bSheets) = CreateEngine("b");
        using var aSurface = RenderToIdle(a, aRoot, aSheets);
        using var bSurface = RenderToIdle(b, bRoot, bSheets);

        long bBefore = b.Mutations.Version;

        var extra = new StyleSheet();
        extra.Add(new CssObject { ["span"] = new() { Color = Color.FromRgb(1, 2, 3) } });
        a.AddStyleSheet(extra);

        a.HasPendingVisualWork.ShouldBeTrue();
        b.Mutations.Version.ShouldBe(bBefore);
        b.HasPendingVisualWork.ShouldBeFalse();
    }

    // ---- 归属语义 --------------------------------------------------------

    [Fact]
    public void Detached_Elements_Should_Be_Mutable_Without_Affecting_Any_Engine()
    {
        var (a, aRoot, aSheets) = CreateEngine("a");
        using var aSurface = RenderToIdle(a, aRoot, aSheets);

        long before = a.Mutations.Version;

        // 尚未挂入任何引擎的游离子树：构造与修改都不该抛异常，也不该记到任何引擎头上。
        var orphan = new DivElement { Class = "orphan" };
        orphan.AddChild(new SpanElement { TextContent = "text" });
        orphan.Class = "orphan changed";
        orphan.Style = new Style { Width = Length.Px(10) };

        a.Mutations.Version.ShouldBe(before);
        a.HasPendingVisualWork.ShouldBeFalse();
    }

    [Fact]
    public void Subtree_Attached_Via_AddChild_Should_Inherit_The_Engines_Tracker()
    {
        var (a, aRoot, aSheets) = CreateEngine("a");
        using var aSurface = RenderToIdle(a, aRoot, aSheets);

        // 先在引擎之外把整棵子树搭好（此时无归属、不记账）……
        var subtree = new DivElement { Class = "panel" };
        var leaf = new SpanElement { TextContent = "leaf" };
        subtree.AddChild(leaf);

        // ……再整体挂入引擎的树。
        aRoot.AddChild(subtree);

        long afterAttach = a.Mutations.Version;

        // 挂入后，深层叶子节点的变更必须被本引擎记账（归属要沿子树下发到底）。
        leaf.TextContent = "leaf changed";
        a.Mutations.Version.ShouldBeGreaterThan(afterAttach);
    }

    [Fact]
    public void Initialize_Should_Claim_A_Tree_Built_Entirely_Outside_The_Engine()
    {
        var engine = new MikoEngineBuilder().Build();

        // 用集合初始化器搭树 —— 完全绕过 AddChild，这是仓库里最常见的构造方式。
        var leaf = new SpanElement { TextContent = "leaf" };
        var root = new DivElement { Class = "root", Children = { new DivElement { Children = { leaf } } } };

        var sheet = new StyleSheet();
        sheet.Add(new CssObject { ["div"] = new() { Width = Length.Px(200), Height = Length.Px(100) } });

        using var surface = SKSurface.Create(new SKImageInfo(300, 300));
        engine.Initialize(root, new List<StyleSheet> { sheet }, surface.Canvas, 300, 300);
        engine.Render(surface.Canvas);

        long before = engine.Mutations.Version;
        leaf.TextContent = "leaf changed";

        engine.Mutations.Version.ShouldBeGreaterThan(before);
        engine.HasPendingVisualWork.ShouldBeTrue();
    }
}
