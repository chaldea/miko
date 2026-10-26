using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Platform;
using Miko.Styling;
using Shouldly;
using SkiaSharp;

namespace Miko.Tests.Platform;

/// <summary>
/// 滚动条的默认样式跟随平台（ISSUE-156）：与浏览器的 <c>scrollbar-width: auto</c> 一致，
/// Windows/Linux 为 classic、macOS 为 overlay、iOS/Android 不显示；应用可按平台调整，
/// 交互控制器按当前平台把结果交给引擎——模拟器换设备时随之切换。
/// </summary>
public class PlatformScrollbarsTests : IDisposable
{
    private const float W = 400;
    private const float H = 300;

    private readonly SKBitmap _bitmap = new((int)W, (int)H);
    private readonly SKCanvas _canvas;

    public PlatformScrollbarsTests()
    {
        _canvas = new SKCanvas(_bitmap);
    }

    public void Dispose()
    {
        _canvas.Dispose();
        _bitmap.Dispose();
    }

    [Theory]
    [InlineData(HostPlatform.Windows, ScrollbarStyle.Classic)]
    [InlineData(HostPlatform.Linux, ScrollbarStyle.Classic)]
    [InlineData(HostPlatform.MacOS, ScrollbarStyle.Overlay)]
    [InlineData(HostPlatform.Ios, ScrollbarStyle.None)]
    [InlineData(HostPlatform.Android, ScrollbarStyle.None)]
    public void Should_default_to_the_platform_convention(HostPlatform platform, ScrollbarStyle expected)
    {
        PlatformScrollbars.GetDefault(platform).ShouldBe(expected);
        new MikoAppOptions().ResolveScrollbarStyle(platform).ShouldBe(expected);
    }

    [Fact]
    public void Should_override_a_single_platform_and_keep_the_others()
    {
        var options = BuildOptions(b => b.UseScrollbarStyle(HostPlatform.Android, ScrollbarStyle.Classic));

        options.ResolveScrollbarStyle(HostPlatform.Android).ShouldBe(ScrollbarStyle.Classic);
        options.ResolveScrollbarStyle(HostPlatform.Ios).ShouldBe(ScrollbarStyle.None);
        options.ResolveScrollbarStyle(HostPlatform.MacOS).ShouldBe(ScrollbarStyle.Overlay);
    }

    [Fact]
    public void Should_override_every_platform_then_refine_one()
    {
        var options = BuildOptions(b => b
            .UseScrollbarStyle(ScrollbarStyle.None)
            .UseScrollbarStyle(HostPlatform.Windows, ScrollbarStyle.Overlay));

        foreach (var platform in Enum.GetValues<HostPlatform>())
        {
            options.ResolveScrollbarStyle(platform)
                .ShouldBe(platform == HostPlatform.Windows ? ScrollbarStyle.Overlay : ScrollbarStyle.None);
        }
    }

    [Fact]
    public void Should_leave_standalone_engines_on_classic_scrollbars()
    {
        // DevTools、模拟器设置面板与单元测试直接构造引擎，不经交互控制器：保持原有表现，
        // 且不随运行测试的机器变化。
        new MikoEngineBuilder().Build().ScrollbarStyle.ShouldBe(ScrollbarStyle.Classic);
    }

    [Theory]
    [InlineData(HostPlatform.Android, ScrollbarStyle.None, W)]
    [InlineData(HostPlatform.Ios, ScrollbarStyle.None, W)]
    [InlineData(HostPlatform.MacOS, ScrollbarStyle.Overlay, W)]
    [InlineData(HostPlatform.Windows, ScrollbarStyle.Classic, W - LayoutBoxThickness)]
    [InlineData(HostPlatform.Linux, ScrollbarStyle.Classic, W - LayoutBoxThickness)]
    public void Should_apply_the_platform_style_from_the_first_layout(
        HostPlatform platform, ScrollbarStyle expected, float contentWidth)
    {
        var (app, content) = BuildApp(platform);

        app.Engine.ScrollbarStyle.ShouldBe(expected);
        // 只有 classic 滚动条占用布局宽度；首帧布局就已按平台排好，而不是先按 classic 排一遍。
        content.LayoutBox!.BoxModel.Content.Width.ShouldBe(contentWidth);
    }

    [Fact]
    public void Should_follow_a_platform_switch_on_the_next_frame()
    {
        // 模拟器换设备时改写 PlatformInfo.Platform（与 Ionic md/ios 模式同一个来源）。
        var (app, content) = BuildApp(HostPlatform.Ios);
        content.LayoutBox!.BoxModel.Content.Width.ShouldBe(W);

        app.Services.GetRequiredService<PlatformInfo>().Platform = HostPlatform.Windows;
        Frame(app);

        app.Engine.ScrollbarStyle.ShouldBe(ScrollbarStyle.Classic);
        content.LayoutBox!.BoxModel.Content.Width.ShouldBe(W - LayoutBoxThickness);

        app.Services.GetRequiredService<PlatformInfo>().Platform = HostPlatform.Android;
        Frame(app);

        app.Engine.ScrollbarStyle.ShouldBe(ScrollbarStyle.None);
        content.LayoutBox!.BoxModel.Content.Width.ShouldBe(W);
    }

    [Fact]
    public void Should_apply_an_app_override_for_the_current_platform()
    {
        var (app, content) = BuildApp(HostPlatform.Android,
            b => b.UseScrollbarStyle(HostPlatform.Android, ScrollbarStyle.Classic));

        app.Engine.ScrollbarStyle.ShouldBe(ScrollbarStyle.Classic);
        content.LayoutBox!.BoxModel.Content.Width.ShouldBe(W - LayoutBoxThickness);
    }

    [Fact]
    public void Should_let_the_host_idle_once_the_style_is_applied()
    {
        // 每帧都会把平台样式交给引擎；值不变时必须是空操作，否则每帧都整树重排、宿主永不空闲。
        var (app, _) = BuildApp(HostPlatform.Windows);
        Frame(app);

        app.Controller.HasPendingWork.ShouldBeFalse();
        Frame(app);
        app.Controller.HasPendingWork.ShouldBeFalse();
    }

    // ================================ helpers ================================

    private const float LayoutBoxThickness = Miko.Layout.LayoutBox.ScrollbarThickness;

    private static MikoAppOptions BuildOptions(Action<MikoAppBuilder> configure)
    {
        var builder = MikoAppBuilder.CreateDefault();
        configure(builder);
        builder.UseRootComponent(() => new DivElement());
        return builder.Build().Services.GetRequiredService<IOptions<MikoAppOptions>>().Value;
    }

    private (MikoAppContext App, DivElement Content) BuildApp(HostPlatform platform, Action<MikoAppBuilder>? configure = null)
    {
        var content = new DivElement { Style = new Style { Height = Length.Px(800) } };
        var builder = MikoAppBuilder.CreateDefault();
        builder.UseRootComponent(() => new DivElement
        {
            Style = new Style
            {
                Display = Display.Block,
                Width = Length.Px(W),
                Height = Length.Px(H),
                OverflowY = Overflow.Auto,
            },
            Children = { content },
        });
        configure?.Invoke(builder);
        var app = builder.Build();
        app.Services.GetRequiredService<PlatformInfo>().Platform = platform;
        app.Controller.Initialize(_canvas, W, H);
        return (app, content);
    }

    private void Frame(MikoAppContext app)
        => app.Controller.RenderFrame(_canvas, W, H, 1f / 60f, c => app.Engine.Render(c));
}
