namespace Miko.Native;

/// <summary>
/// 延迟提供平台宿主对象（Android <c>Activity</c>、iOS <c>UIViewController</c>、桌面 <c>IWindow</c>）
/// 的中立容器。
/// <para>
/// 为什么需要它：共享 UI 项目的 <c>App.CreateContext()</c> 在 <c>MikoAppBuilder.Build()</c> 处就
/// 构建了服务容器，而此时平台宿主（Activity / ViewController / 窗口）还不存在——Android 的
/// <c>MainActivity.OnCreate</c> 恰恰是在拿到 <c>MikoAppContext</c> 之后才创建视图的。原生服务
/// 因此不能在构造时接收宿主句柄，只能在**真正被调用时**才去取。
/// </para>
/// <para>
/// 宿主（<c>MikoSurfaceView</c> / <c>MikoViewController</c> / <c>SilkDesktopHost</c> /
/// <c>SimulatorHost</c>）构造时调用 <see cref="Attach"/> 回填自己；平台服务通过
/// <see cref="NativeHostContextExtensions.RequireHost{T}"/> 取用。
/// </para>
/// </summary>
public interface INativeHostContext
{
    /// <summary>当前已附加的平台宿主对象；宿主尚未创建时为 <c>null</c>。</summary>
    object? Host { get; }

    /// <summary>由平台宿主在自身构造完成时调用，回填宿主引用。</summary>
    void Attach(object host);

    /// <summary>宿主附加时触发。晚于附加订阅的处理器会立即以当前宿主回调一次。</summary>
    event Action<object>? HostAttached;
}

/// <summary>
/// 默认的可变 <see cref="INativeHostContext"/> 实现，由 <c>AddMikoNative()</c> 注册为单例。
/// </summary>
public sealed class NativeHostContext : INativeHostContext
{
    private readonly Lock _gate = new();
    private object? _host;
    private Action<object>? _hostAttached;

    /// <inheritdoc />
    public object? Host
    {
        get { lock (_gate) return _host; }
    }

    /// <inheritdoc />
    public void Attach(object host)
    {
        ArgumentNullException.ThrowIfNull(host);

        Action<object>? handlers;
        lock (_gate)
        {
            _host = host;
            handlers = _hostAttached;
        }

        // 在锁外回调：处理器可能同步回头访问 Host（或再次订阅），持锁调用会死锁。
        handlers?.Invoke(host);
    }

    /// <summary>
    /// 宿主附加事件。附加**之后**才订阅的处理器会立刻收到一次回调，避免订阅方因为
    /// 晚于宿主创建而永远收不到通知（宿主只附加一次，错过就没有第二次）。
    /// </summary>
    public event Action<object>? HostAttached
    {
        add
        {
            if (value is null) return;

            object? host;
            lock (_gate)
            {
                _hostAttached += value;
                host = _host;
            }

            if (host is not null) value(host);
        }
        remove
        {
            if (value is null) return;
            lock (_gate) _hostAttached -= value;
        }
    }
}

/// <summary>取用平台宿主的便捷扩展。</summary>
public static class NativeHostContextExtensions
{
    /// <summary>
    /// 取得指定类型的平台宿主。宿主尚未附加、或类型不匹配时抛出 <see cref="InvalidOperationException"/>
    /// ——这表示宿主接线有误（平台包忘了调 <see cref="INativeHostContext.Attach"/>），
    /// 而不是「该平台不支持此能力」（后者用 <see cref="PlatformNotSupportedException"/>）。
    /// </summary>
    public static T RequireHost<T>(this INativeHostContext context) where T : class
    {
        var host = context.Host;
        if (host is null)
        {
            throw new InvalidOperationException(
                $"No native host is attached yet. The platform host must call {nameof(INativeHostContext)}." +
                $"{nameof(INativeHostContext.Attach)} before native services of type {typeof(T).Name} are used.");
        }

        if (host is not T typed)
        {
            throw new InvalidOperationException(
                $"The attached native host is {host.GetType().Name}, but {typeof(T).Name} was required. " +
                "A native service is being used with a host from a different platform package.");
        }

        return typed;
    }
}
