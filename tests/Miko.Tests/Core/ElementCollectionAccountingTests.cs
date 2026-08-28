using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Highlight;
using Miko.Hosting;
using Miko.Platform.Video;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Core;

/// <summary>
/// ISSUE-129 审查发现的四个问题的回归测试。
///
/// <para>前三个都源于同一个根因：<c>Element.Children</c> 是公开可写集合，而「结构变了」这件事
/// 原先只在 <c>AddChild</c>/<c>RemoveChild</c> 里记账。绕过这两个方法直接写集合的调用方
/// （包括仓库内部的组件重渲染路径）就不会让布局缓存失效，稳态帧之后的增删改因此完全不呈现。
/// 现由 <see cref="ElementCollection"/> 统一记账。</para>
/// </summary>
public class ElementCollectionAccountingTests
{
    private static (MikoEngine engine, Element root, List<StyleSheet> sheets) NewEngine()
    {
        var root = new DivElement { Class = "root" };
        var sheet = new StyleSheet();
        sheet.Add(new CssObject
        {
            ["div"] = new() { Width = Length.Px(200), Height = Length.Px(100) },
            ["span"] = new() { Width = Length.Px(50), Height = Length.Px(20) },
        });
        return (new MikoEngineBuilder().Build(), root, new List<StyleSheet> { sheet });
    }

    /// <summary>渲染到稳态：此后若无变更，<c>HasPendingVisualWork</c> 应为 false。</summary>
    private static SKSurface RenderToIdle(MikoEngine engine, Element root, List<StyleSheet> sheets)
    {
        var surface = SKSurface.Create(new SKImageInfo(300, 300));
        engine.Initialize(root, sheets, surface.Canvas, 300, 300);
        engine.Render(surface.Canvas);
        return surface;
    }

    // ---- P1：直接修改 Children 必须使布局缓存失效 ---------------------------------

    [Fact]
    public void ChildrenAdd_AfterIdle_InvalidatesLayout()
    {
        var (engine, root, sheets) = NewEngine();
        using var surface = RenderToIdle(engine, root, sheets);
        engine.HasPendingVisualWork.ShouldBeFalse("前提：应已进入稳态");

        root.Children.Add(new SpanElement { TextContent = "spliced" });

        engine.HasPendingVisualWork.ShouldBeTrue();
        engine.Render(surface.Canvas);
        // 新节点真的参与了布局，而不是命中旧缓存被丢弃。
        engine.GetCurrentLayout()!.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void ChildrenClear_AfterIdle_InvalidatesLayout()
    {
        var (engine, root, sheets) = NewEngine();
        root.AddChild(new SpanElement { TextContent = "a" });
        using var surface = RenderToIdle(engine, root, sheets);
        engine.GetCurrentLayout()!.Children.Count.ShouldBe(1);

        root.Children.Clear();

        engine.HasPendingVisualWork.ShouldBeTrue();
        engine.Render(surface.Canvas);
        engine.GetCurrentLayout()!.Children.ShouldBeEmpty();
    }

    [Fact]
    public void ChildrenIndexerReplace_AfterIdle_InvalidatesLayout()
    {
        var (engine, root, sheets) = NewEngine();
        root.AddChild(new SpanElement { TextContent = "old" });
        using var surface = RenderToIdle(engine, root, sheets);

        root.Children[0] = new SpanElement { TextContent = "new" };

        engine.HasPendingVisualWork.ShouldBeTrue();
        engine.Render(surface.Canvas);
        root.Children[0].TextContent.ShouldBe("new");
    }

    [Fact]
    public void ChildrenInsertAndRemoveAt_AfterIdle_InvalidateLayout()
    {
        var (engine, root, sheets) = NewEngine();
        using var surface = RenderToIdle(engine, root, sheets);

        root.Children.Insert(0, new SpanElement { TextContent = "ins" });
        engine.HasPendingVisualWork.ShouldBeTrue("Insert 必须记账");
        engine.Render(surface.Canvas);

        root.Children.RemoveAt(0);
        engine.HasPendingVisualWork.ShouldBeTrue("RemoveAt 必须记账");
    }

    [Fact]
    public void ChildrenAdd_SetsParentAndOwner()
    {
        var (engine, root, sheets) = NewEngine();
        using var surface = RenderToIdle(engine, root, sheets);

        var child = new SpanElement { TextContent = "x" };
        root.Children.Add(child);

        // 认领：父引用与引擎归属都要就位，否则子树自身后续的变更也不会被记账。
        child.Parent.ShouldBeSameAs(root);
        engine.Render(surface.Canvas);
        engine.HasPendingVisualWork.ShouldBeFalse();
        child.Class = "now-owned";
        engine.HasPendingVisualWork.ShouldBeTrue("新子树自身的变更也应记到本引擎");
    }

    [Fact]
    public void CollectionInitializer_StillSupported_AndSetsParent()
    {
        // 仓库里 100+ 处这样构造 DOM，改动不能破坏它。
        var child = new SpanElement { TextContent = "a" };
        var parent = new DivElement { Class = "p", Children = { child } };

        parent.Children.Count.ShouldBe(1);
        child.Parent.ShouldBeSameAs(parent);
    }

    // ---- P2：移除后不再记到旧引擎头上 ---------------------------------------------

    [Fact]
    public void RemovedSubtree_NoLongerBumpsOldEngine()
    {
        var (engine, root, sheets) = NewEngine();
        var child = new SpanElement { TextContent = "x" };
        root.AddChild(child);
        using var surface = RenderToIdle(engine, root, sheets);

        root.RemoveChild(child);
        // 移除本身仍然要记账——旧引擎的布局缓存必须失效。
        engine.HasPendingVisualWork.ShouldBeTrue();
        engine.Render(surface.Canvas);

        long before = engine.Mutations.Version;
        child.TextContent = "changed while detached";
        child.Class = "also-changed";
        engine.Mutations.Version.ShouldBe(before, "脱离 DOM 的元素不该再产生无意义的重排工作");
        engine.HasPendingVisualWork.ShouldBeFalse();
    }

    [Theory]
    [InlineData("Remove")]
    [InlineData("RemoveAt")]
    [InlineData("Clear")]
    [InlineData("Indexer")]
    public void AllCollectionRemovalPaths_ClearOwnerLikeRemoveChild(string how)
    {
        var (engine, root, sheets) = NewEngine();
        var child = new SpanElement { TextContent = "x" };
        root.AddChild(child);
        using var surface = RenderToIdle(engine, root, sheets);

        switch (how)
        {
            case "Remove": root.Children.Remove(child); break;
            case "RemoveAt": root.Children.RemoveAt(0); break;
            case "Clear": root.Children.Clear(); break;
            case "Indexer": root.Children[0] = new SpanElement { TextContent = "replacement" }; break;
        }

        // 移除/替换本身仍要记账（旧引擎的布局缓存必须失效）。
        engine.HasPendingVisualWork.ShouldBeTrue();
        engine.Render(surface.Canvas);

        // 但被摘下的子树此后不再属于本引擎——与 RemoveChild 的语义必须一致，
        // 否则「经集合移除」就成了绕过该语义的另一扇门。
        child.Parent.ShouldBeNull();
        long before = engine.Mutations.Version;
        child.Class = "changed while detached";
        engine.Mutations.Version.ShouldBe(before);
    }

    [Fact]
    public void ReattachedSubtree_BumpsNewEngineAgain()
    {
        var (engineA, rootA, sheetsA) = NewEngine();
        var (engineB, rootB, sheetsB) = NewEngine();
        var child = new SpanElement { TextContent = "x" };
        rootA.AddChild(child);
        using var surfaceA = RenderToIdle(engineA, rootA, sheetsA);
        using var surfaceB = RenderToIdle(engineB, rootB, sheetsB);

        rootA.RemoveChild(child);
        engineA.Render(surfaceA.Canvas);

        // 挂到另一个引擎的树上后，变更应记到新引擎、且不影响旧引擎。
        rootB.Children.Add(child);
        engineB.Render(surfaceB.Canvas);
        long a = engineA.Mutations.Version;

        child.Class = "moved";
        engineB.HasPendingVisualWork.ShouldBeTrue();
        engineA.Mutations.Version.ShouldBe(a, "旧引擎不应再被已迁走的子树影响");
    }

    // ---- P1：Builder 的自定义注册必须覆盖默认实现（与调用顺序无关）-----------------

    private sealed class CustomHighlighter : ISyntaxHighlighter
    {
        public SyntaxTheme Theme => new();
        public IReadOnlyList<CodeToken>? Tokenize(string text, string? language) => null;
    }

    private sealed class CustomVideoBackend : IVideoBackend
    {
        public VideoBackendCapabilities Capabilities { get; } =
            new(HardwareDecode: true, Hdr: false, SupportedMimeTypes: new[] { "video/mp4" });
        public IVideoSession CreateSession(VideoSourceDescriptor s, VideoSessionOptions o)
            => throw new NotSupportedException();
    }

    [Fact]
    public void Builder_ConfigureServicesBeforeBuild_OverridesDefaults()
    {
        // ConfigureServices 在 Build() 之前执行，而 Build() 内部才调 AddMikoEngine()。
        // 默认注册若用 AddSingleton 就会后注册并覆盖掉这里的自定义实现（DI 取最后一项）。
        var engine = new MikoEngineBuilder()
            .ConfigureServices(s => s.AddSingleton<ISyntaxHighlighter, CustomHighlighter>())
            .Build();

        engine.SyntaxHighlighter.ShouldBeOfType<CustomHighlighter>();
    }

    [Fact]
    public void Builder_CustomVideoBackend_Wins()
    {
        var engine = new MikoEngineBuilder()
            .ConfigureServices(s => s.AddSingleton<IVideoBackend, CustomVideoBackend>())
            .Build();

        engine.VideoBackend.ShouldBeOfType<CustomVideoBackend>();
    }

    [Fact]
    public void Builder_WithoutOverrides_GetsWorkingDefaults()
    {
        var engine = new MikoEngineBuilder().Build();

        // 三个可选服务都由构造器注入，默认实现在核心库内。
        engine.SyntaxHighlighter.ShouldNotBeNull();
        engine.ImageLoader.ShouldNotBeNull();
        // 默认视频后端是空实现：不具备播放能力，<video> 只显示背景/poster。
        engine.VideoBackend.ShouldBeOfType<NullVideoBackend>();
    }

    [Fact]
    public void AddMikoEngine_IsIdempotent_AndPreservesEarlierRegistrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISyntaxHighlighter, CustomHighlighter>();
        services.AddMikoEngine();
        services.AddMikoEngine(); // 重复调用不应重复注册或覆盖

        var engine = services.BuildServiceProvider().GetRequiredService<MikoEngine>();
        engine.SyntaxHighlighter.ShouldBeOfType<CustomHighlighter>();
    }
}
