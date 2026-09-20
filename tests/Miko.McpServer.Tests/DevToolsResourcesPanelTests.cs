using System.Reflection;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.DevTools;
using Miko.DevTools.Panels;
using Miko.DevTools.Styles;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform.Resources;
using Shouldly;
using SkiaSharp;

namespace Miko.McpServer.Tests;

/// <summary>
/// DevTools 的 Resources 面板（ISSUE-139）：嵌入资源清单、Miko 下载的网络资源、解码缓存。
///
/// <para>面板的 DOM 结构就是它的契约——三节标题总在（哪怕某节无数据），且嵌入资源那一节
/// 不依赖加载器：数据来自程序集元数据，应用换用自定义 <c>IImageLoader</c> 后仍应显示。</para>
/// </summary>
public class DevToolsResourcesPanelTests
{
    private static readonly Assembly TestAssembly = typeof(DevToolsResourcesPanelTests).Assembly;

    private static DevToolsBridge Bridge() => new(new DevToolsOptions());

    private sealed class StubAssemblyProvider : IResourceAssemblyProvider
    {
        private readonly Assembly[] _assemblies;
        public StubAssemblyProvider(params Assembly[] assemblies) => _assemblies = assemblies;
        public IEnumerable<Assembly> GetResourceAssemblies() => _assemblies;
    }

    /// <summary>不实现 <see cref="IResourceDiagnostics"/> 的加载器：网络/缓存两节应给出说明而非崩溃。</summary>
    private sealed class OpaqueImageLoader : IImageLoader
    {
        public Task<SKBitmap?> LoadAsync(MediaSource source, CancellationToken ct = default) =>
            Task.FromResult<SKBitmap?>(null);
    }

    private static IEnumerable<Element> Descendants(Element root)
    {
        foreach (var child in root.Children)
        {
            yield return child;
            foreach (var d in Descendants(child)) yield return d;
        }
    }

    /// <summary>
    /// 面板里每个元素的文本。跳过 <see cref="TextNode"/>：文本以文本节点承载、由
    /// <c>TextContent</c> 门面转发（ISSUE-086），若一并遍历则每条文本会被数两次。
    /// </summary>
    private static IEnumerable<string> AllText(Element root) =>
        Descendants(root)
            .Where(e => e is not TextNode)
            .Select(e => e.TextContent)
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => t!);

    private static bool ContainsText(Element root, string needle) =>
        AllText(root).Any(t => t.Contains(needle, StringComparison.Ordinal));

    // ---- 面板骨架 -----------------------------------------------------------

    [Fact]
    public void Panel_Should_Render_All_Three_Sections()
    {
        var panel = ResourcesPanel.Build(Bridge(), visible: true);

        // 三节标题是面板的骨架，与是否有数据无关。
        ContainsText(panel, "Embedded Resources").ShouldBeTrue();
        ContainsText(panel, "Network Resources").ShouldBeTrue();
        ContainsText(panel, "Cached Resources").ShouldBeTrue();
    }

    [Fact]
    public void Panel_Should_Be_Hidden_When_Tab_Is_Inactive()
    {
        // 面板与 Elements/Console 同构：非活动页用 display:none 留在树里，
        // 这样切回来时滚动位置与布局缓存都还在。
        var hidden = ResourcesPanel.Build(Bridge(), visible: false);
        var shown = ResourcesPanel.Build(Bridge(), visible: true);

        hidden.Style?.Display?.Value.ShouldBe(Display.None);
        shown.Style.ShouldBeNull();
    }

    [Fact]
    public void Panel_Should_Have_A_Scrollable_Output_Region()
    {
        // 窗口按 "resources-output" 这个类名保存/恢复滚动位置（见 DevToolsWindow.RebuildUI）。
        var panel = ResourcesPanel.Build(Bridge(), visible: true);

        Descendants(panel).Any(e => e.HasClass("resources-output")).ShouldBeTrue();
    }

    // ---- 嵌入资源 -----------------------------------------------------------

    [Fact]
    public void Embedded_Section_Should_List_Writable_Res_Paths()
    {
        var bridge = Bridge();
        bridge.Initialize(new MikoEngineBuilder().Build(), new StubAssemblyProvider(TestAssembly));

        var panel = ResourcesPanel.Build(bridge, visible: true);

        // 展示的必须是可书写的 res:// 路径（用户要复制的就是这个），不是裸清单名。
        AllText(panel).Any(t => t.StartsWith("res://", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void Embedded_Section_Should_Report_The_Owning_Assembly()
    {
        var bridge = Bridge();
        bridge.Initialize(new MikoEngineBuilder().Build(), new StubAssemblyProvider(TestAssembly));

        var panel = ResourcesPanel.Build(bridge, visible: true);

        ContainsText(panel, "Miko.McpServer.Tests").ShouldBeTrue();
    }

    [Fact]
    public void Embedded_Section_Should_Explain_A_Missing_Provider()
    {
        // 未注册提供器（应用自定义了 IImageLoader 且没调 AddImageLoader）：说明原因，不是空白。
        var panel = ResourcesPanel.Build(Bridge(), visible: true);

        ContainsText(panel, "No resource assembly provider").ShouldBeTrue();
    }

    [Fact]
    public void Embedded_Section_Should_Work_Without_A_Diagnostics_Capable_Loader()
    {
        // 嵌入资源来自程序集元数据，与加载器无关——换了自定义加载器这节仍要有内容。
        var engine = new MikoEngineBuilder().Build();
        engine.ImageLoader = new OpaqueImageLoader();

        var bridge = Bridge();
        bridge.Initialize(engine, new StubAssemblyProvider(TestAssembly));

        var panel = ResourcesPanel.Build(bridge, visible: true);

        AllText(panel).Any(t => t.StartsWith("res://", StringComparison.Ordinal)).ShouldBeTrue();
    }

    // ---- 网络与缓存 ---------------------------------------------------------

    [Fact]
    public void Diagnostics_Sections_Should_Explain_An_Opaque_Loader()
    {
        var engine = new MikoEngineBuilder().Build();
        engine.ImageLoader = new OpaqueImageLoader();

        var bridge = Bridge();
        bridge.Initialize(engine, new StubAssemblyProvider(TestAssembly));
        bridge.ResourceDiagnostics.ShouldBeNull();

        var panel = ResourcesPanel.Build(bridge, visible: true);

        // 两节都要说明「加载器不上报诊断」，而不是伪装成「没有资源」。
        AllText(panel).Count(t => t.Contains("does not report diagnostics", StringComparison.Ordinal))
            .ShouldBe(2);
    }

    [Fact]
    public async Task Cache_Section_Should_List_Loaded_Entries()
    {
        var loader = new ResourceManager(assemblyProvider: new StubAssemblyProvider(TestAssembly));
        var engine = new MikoEngineBuilder().Build();
        engine.ImageLoader = loader;

        var bridge = Bridge();
        bridge.Initialize(engine, new StubAssemblyProvider(TestAssembly));
        bridge.ResourceDiagnostics.ShouldNotBeNull();

        // 一次失败的加载也会进缓存（失败结果被缓存，不会重试）——面板必须显示它，
        // 否则「图为什么不出来」这类排查正好看不到线索。
        await loader.LoadAsync("res://does/not/exist.svg");

        var panel = ResourcesPanel.Build(bridge, visible: true);

        ContainsText(panel, "res://does/not/exist.svg").ShouldBeTrue();
        ContainsText(panel, "failed").ShouldBeTrue();
    }

    [Fact]
    public void Default_Loader_Should_Expose_Diagnostics()
    {
        // 默认装配（AddMikoEngine → AddImageLoader → ResourceManager）必须是可观测的，
        // 否则面板在开箱即用的应用里就是空的。
        var bridge = Bridge();
        bridge.Initialize(new MikoEngineBuilder().Build());

        bridge.ResourceDiagnostics.ShouldNotBeNull();
    }

    // ---- 真实布局 -----------------------------------------------------------

    [Fact]
    public void Panel_Should_Lay_Out_Rows_Under_The_Real_Stylesheet()
    {
        // 前面的断言只看 DOM。这一条把面板喂给真实引擎 + 真实 DevTools 样式表，
        // 确认新增的那批规则真的产出可见的行（宽高非零、在视口内），而不是塌缩成 0 高。
        var bridge = Bridge();
        bridge.Initialize(new MikoEngineBuilder().Build(), new StubAssemblyProvider(TestAssembly));

        var root = new DivElement { Class = "devtools-root" };
        root.AddChild(ResourcesPanel.Build(bridge, visible: true));

        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo(900, 600));
        engine.Initialize(root, new List<Styling.StyleSheet> { DevToolsStyleSheet.Create() },
            surface.Canvas, 900, 600);

        var layout = engine.GetCurrentLayout();
        layout.ShouldNotBeNull();

        var rows = new List<LayoutBox>();
        CollectByClass(layout!, "res-row", rows);

        // 嵌入资源那节的表头 + 至少一条资源行（网络/缓存此时为空，只给说明文字，不出行）。
        var dataRows = rows.Where(r => !r.Element.HasClass("res-row-header")).ToList();
        rows.Any(r => r.Element.HasClass("res-row-header")).ShouldBeTrue();
        dataRows.ShouldNotBeEmpty();

        foreach (var row in rows)
        {
            var box = row.BoxModel.BorderBox;
            box.Height.ShouldBeGreaterThan(0, "a resource row collapsed to zero height");
            box.Width.ShouldBeGreaterThan(0);
            box.Width.ShouldBeLessThanOrEqualTo(900);
        }
    }

    [Fact]
    public void Long_Paths_Should_Not_Squeeze_Out_The_Size_Column()
    {
        // 嵌入资源的路径可以很长；固定宽的尺寸列必须保持自己的宽度（FlexShrink = 0），
        // 否则大小数字会被挤成 0 宽而看不见。
        var bridge = Bridge();
        bridge.Initialize(new MikoEngineBuilder().Build(), new StubAssemblyProvider(TestAssembly));

        var root = new DivElement { Class = "devtools-root" };
        root.AddChild(ResourcesPanel.Build(bridge, visible: true));

        var engine = new MikoEngineBuilder().Build();
        using var surface = SKSurface.Create(new SKImageInfo(420, 600));
        engine.Initialize(root, new List<Styling.StyleSheet> { DevToolsStyleSheet.Create() },
            surface.Canvas, 420, 600);

        var sizeCells = new List<LayoutBox>();
        CollectByClass(engine.GetCurrentLayout()!, "res-cell-size", sizeCells);

        sizeCells.ShouldNotBeEmpty();
        foreach (var cell in sizeCells)
            cell.BoxModel.BorderBox.Width.ShouldBeGreaterThan(0);
    }

    private static void CollectByClass(LayoutBox box, string className, List<LayoutBox> sink)
    {
        if (box.Element.HasClass(className)) sink.Add(box);
        foreach (var child in box.Children)
            CollectByClass(child, className, sink);
    }
}
