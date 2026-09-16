using Android.Content;
using Android.Net;
using Miko.Native;
using Miko.Native.Network;

namespace Miko.Android.Native;

/// <summary>
/// Android 网络状态，基于 <see cref="ConnectivityManager"/> 的 NetworkCallback。
/// <para>
/// 只在有订阅者时注册 callback，并在最后一个订阅者移除时注销——
/// 注册未注销会让系统一直持有回调对象（<c>issues/feat-platform.md</c> 要求第 2 条）。
/// </para>
/// </summary>
internal sealed class AndroidNetworkService : NativeServiceBase, INetworkService, IDisposable
{
    private readonly INativeHostContext _hostContext;
    private readonly Lock _gate = new();
    private Action<ConnectionStatus>? _statusChanged;
    private NetworkCallback? _callback;

    public AndroidNetworkService(INativeHostContext hostContext) => _hostContext = hostContext;

    private ConnectivityManager Manager =>
        _hostContext.RequireHost<AndroidNativeHost>().Context
            .GetSystemService(Context.ConnectivityService) as ConnectivityManager
        ?? throw new InvalidOperationException("ConnectivityManager is unavailable.");

    public Task<ConnectionStatus> GetStatusAsync() => Task.FromResult(ReadStatus(Manager));

    public event Action<ConnectionStatus>? OnNetworkStatusChange
    {
        add
        {
            if (value is null) return;

            lock (_gate)
            {
                _statusChanged += value;
                if (_callback is not null) return;

                _callback = new NetworkCallback(this);
                Manager.RegisterDefaultNetworkCallback(_callback);
            }
        }
        remove
        {
            if (value is null) return;

            lock (_gate)
            {
                _statusChanged -= value;
                if (_statusChanged is not null || _callback is null) return;

                Manager.UnregisterNetworkCallback(_callback);
                _callback.Dispose();
                _callback = null;
            }
        }
    }

    private void Publish()
    {
        Action<ConnectionStatus>? handlers;
        lock (_gate) handlers = _statusChanged;

        if (handlers is null) return;
        handlers(ReadStatus(Manager));
    }

    private static ConnectionStatus ReadStatus(ConnectivityManager manager)
    {
        var network = manager.ActiveNetwork;
        if (network is null) return new ConnectionStatus(false, ConnectionType.None);

        var capabilities = manager.GetNetworkCapabilities(network);
        if (capabilities is null) return new ConnectionStatus(false, ConnectionType.None);

        // NET_CAPABILITY_INTERNET 只说明该网络「应当」能上网；
        // VALIDATED 才表示系统实际验证过连通性（连上了没有网的 Wi-Fi 时两者不同）。
        var connected = capabilities.HasCapability(NetCapability.Internet)
                        && capabilities.HasCapability(NetCapability.Validated);

        if (!connected) return new ConnectionStatus(false, ConnectionType.None);

        if (capabilities.HasTransport(TransportType.Wifi) || capabilities.HasTransport(TransportType.Ethernet))
            return new ConnectionStatus(true, ConnectionType.Wifi);

        if (capabilities.HasTransport(TransportType.Cellular))
            return new ConnectionStatus(true, ConnectionType.Cellular);

        return new ConnectionStatus(true, ConnectionType.Unknown);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_callback is null) return;

            try { Manager.UnregisterNetworkCallback(_callback); }
            catch (Java.Lang.IllegalArgumentException) { /* 已注销 */ }

            _callback.Dispose();
            _callback = null;
            _statusChanged = null;
        }
    }

    private sealed class NetworkCallback : ConnectivityManager.NetworkCallback
    {
        private readonly AndroidNetworkService _owner;

        public NetworkCallback(AndroidNetworkService owner) => _owner = owner;

        public override void OnAvailable(global::Android.Net.Network network) => _owner.Publish();
        public override void OnLost(global::Android.Net.Network network) => _owner.Publish();

        public override void OnCapabilitiesChanged(
            global::Android.Net.Network network,
            NetworkCapabilities networkCapabilities)
            // 连上无网 Wi-Fi 后再变为 VALIDATED 时，只有这个回调会触发。
            => _owner.Publish();
    }
}
