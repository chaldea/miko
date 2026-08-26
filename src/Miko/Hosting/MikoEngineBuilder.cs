using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miko.Core;

namespace Miko.Hosting;

/// <summary>
/// 构建独立的 <see cref="MikoEngine"/> 实例（ISSUE-129）。
///
/// <para>适用于「只要一个渲染引擎、不要整个应用宿主」的场景：DevTools 独立窗口、
/// 模拟器的设置面板、单元测试。需要路由/热重载/交互控制器等完整应用能力时用
/// <see cref="MikoAppBuilder"/>——它复用同一套 <see cref="EngineExtensions.AddMikoEngine"/> 注册。</para>
///
/// <para>每次 <see cref="Build"/> 产出一个<b>独立的容器与引擎</b>，其变更计数器、布局缓存、
/// 脏区域、动画状态都与其它引擎完全隔离。</para>
///
/// <example>
/// <code>
/// var engine = new MikoEngineBuilder().Build();
///
/// // 覆盖默认服务 / 追加自定义注册
/// var engine2 = new MikoEngineBuilder()
///     .ConfigureServices(s => s.AddSingleton&lt;ISyntaxHighlighter, MyHighlighter&gt;())
///     .Build();
/// </code>
/// </example>
/// </summary>
public class MikoEngineBuilder
{
    /// <summary>引擎的服务容器。可在 <see cref="Build"/> 前追加或覆盖注册。</summary>
    public IServiceCollection Services { get; } = new ServiceCollection();

    public MikoEngineBuilder()
    {
        Services.AddMikoEngine();
    }

    /// <summary>
    /// 追加/覆盖服务注册。后注册者优先，因此可用来替换 <c>AddMikoEngine</c> 的默认实现
    /// （如自定义 <c>IImageLoader</c>、<c>ISyntaxHighlighter</c>）。
    /// </summary>
    public MikoEngineBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(Services);
        return this;
    }

    /// <summary>配置日志。</summary>
    public MikoEngineBuilder UseLogging(Action<ILoggingBuilder> configure)
    {
        Services.AddLogging(configure);
        return this;
    }

    /// <summary>
    /// 构建引擎。内部注册引擎依赖并解析出实例；容器随引擎存活
    /// （引擎的可选依赖已在注册阶段注入，无需调用方持有容器）。
    /// </summary>
    public MikoEngine Build()
    {
        var serviceProvider = Services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<MikoEngine>();
    }
}
