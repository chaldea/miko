using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Miko.Animation;
using Miko.Core;
using Miko.Events;
using Miko.Highlight;
using Miko.Layout;
using Miko.Platform;
using Miko.Platform.Resources;
using Miko.Platform.Video;
using Miko.Rendering;

namespace Miko.Hosting;

/// <summary>
/// 引擎的 DI 注册扩展（ISSUE-129）。把一个 <see cref="MikoEngine"/> 及其全部依赖注册为
/// 容器内的单例，使「一个容器 = 一个引擎实例」，多个容器彼此完全隔离。
///
/// <para><see cref="MikoAppBuilder.CreateDefault"/> 与 <see cref="MikoEngineBuilder"/>
/// 都复用本方法，因此两条路径构造出的引擎在依赖与配置上完全一致。</para>
/// </summary>
public static class EngineExtensions
{
    /// <summary>
    /// 注册引擎及其依赖（布局、渲染、脏区域、事件派发、动画、调度器、变更计数器），
    /// 并带上引擎正常工作所需的默认服务（视频后端、图片加载、语法高亮）。
    ///
    /// <para>全部为单例：<see cref="MutationTracker"/> 按容器唯一，是「引擎之间互不干扰」的
    /// 支点——它决定了哪些 DOM 变更会使<b>本</b>引擎的布局缓存失效（见 ISSUE-129）。</para>
    ///
    /// <para><b>注册策略：一律 <c>TryAdd</c>。</b>因此本方法既可重复调用而不重复注册，也
    /// <b>不会覆盖调用方已有的注册</b>——无论用户的自定义注册发生在本方法之前还是之后，
    /// 它都胜出。这一点很重要：<see cref="MikoEngineBuilder"/> 允许先
    /// <c>ConfigureServices(...)</c> 再 <c>Build()</c>，而 <c>Build()</c> 内部才调用本方法；
    /// 若这里用 <c>AddSingleton</c>，默认实现会后注册从而覆盖掉用户的自定义实现
    /// （Microsoft DI 单服务解析取最后一项），与 Builder「可覆盖默认服务」的承诺相悖。</para>
    /// </summary>
    public static IServiceCollection AddMikoEngine(this IServiceCollection services)
    {
        services.AddLogging();

        // 本引擎的变更版本号。按容器唯一 —— 多实例隔离的支点。
        services.TryAddSingleton<MutationTracker>();

        services.TryAddSingleton<LayoutEngine>();
        services.TryAddSingleton<RenderEngine>();
        services.TryAddSingleton<DirtyRegionManager>();
        services.TryAddSingleton<EventDispatcher>();
        services.TryAddSingleton<AnimationManager>();
        services.TryAddSingleton<MikoDispatcher>();

        // 三个可选服务在核心库里都有默认实现，因此引擎可以直接构造器注入它们，
        // 不必依赖 IServiceProvider 做可选解析：
        // - 视频后端默认为空实现（不创建会话，<video> 只显示背景/poster）；
        //   平台宿主注册 FFmpeg/MediaCodec/AVFoundation 后端即被覆盖。
        services.TryAddSingleton<IVideoBackend, NullVideoBackend>();
        // - 图片加载（<img> 需要它才能自动加载）。
        services.AddImageLoader();
        // - 语法高亮（应用可重新注册 ISyntaxHighlighter 覆盖，见 ISSUE-098）。
        services.AddSyntaxHighlighter();

        services.TryAddSingleton<MikoEngine>();

        return services;
    }
}
