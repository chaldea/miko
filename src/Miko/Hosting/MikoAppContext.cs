using Microsoft.Extensions.Options;
using Miko.Core;
using Miko.Fonts;
using Miko.Platform;

namespace Miko.Hosting;

/// <summary>
/// 平台无关的应用上下文，由 <see cref="MikoAppBuilder.Build"/> 创建。
/// 它持有已配置好的服务容器、应用选项、交互控制器与渲染引擎，
/// 供各平台实现层（桌面 / Android / iOS）的宿主驱动渲染循环。
/// </summary>
public sealed class MikoAppContext
{
    private readonly MikoAppOptions _options;

    public MikoAppContext(
        IServiceProvider services,
        IOptions<MikoAppOptions> options,
        MikoInteractionController controller,
        HotReloadService hotReloadService,
        MikoEngine engine)
    {
        Services = services;
        _options = options.Value;
        Controller = controller;
        HotReloadService = hotReloadService;
        Engine = engine;
    }

    /// <summary>已构建的服务容器。</summary>
    public IServiceProvider Services { get; }

    /// <summary>应用选项（标题、初始尺寸、字体等）。</summary>
    public MikoAppOptions Options => _options;

    /// <summary>平台无关的交互控制器，平台层将归一化输入转发到此处。</summary>
    public MikoInteractionController Controller { get; }

    /// <summary>渲染引擎，平台层用其渲染到 SkiaSharp 画布。</summary>
    public MikoEngine Engine { get; }

    /// <summary>热重载服务（供生成的 MikoHotReloadHandler 使用）。</summary>
    public HotReloadService HotReloadService { get; }

    /// <summary>
    /// 获取该应用的热重载服务。用于应用特定的热重载处理器初始化。
    /// </summary>
    public HotReloadService GetHotReloadService() => HotReloadService;

    /// <summary>
    /// 注册选项中声明的所有嵌入字体资源。平台宿主在初始化渲染前调用。
    /// </summary>
    public void RegisterFonts()
    {
        foreach (var font in _options.Fonts)
        {
            using var stream = font.Assembly.GetManifestResourceStream(font.ResourceName);
            if (stream == null)
                throw new InvalidOperationException($"Embedded resource not found: {font.ResourceName}");

            FontManager.Instance.RegisterFont(font.FamilyName, stream);
        }
    }
}
