using System.Net.NetworkInformation;
using Miko.Native.Network;

namespace Miko.Windowing.Native;

/// <summary>
/// 桌面网络状态，基于 <see cref="NetworkInterface"/> 与 <see cref="NetworkChange"/>。
/// <para>
/// 桌面无法区分 Wi-Fi 与蜂窝的计费语义，但可以分辨接口类型：无线接口报
/// <see cref="ConnectionType.Wifi"/>，有线以太网同样归入 <see cref="ConnectionType.Wifi"/>
/// （Capacitor 的四分法没有「有线」取值，而有线在计费/限速上的行为与 Wi-Fi 一致）。
/// </para>
/// </summary>
internal sealed class DesktopNetworkService : INetworkService, IDisposable
{
    private Action<ConnectionStatus>? _statusChanged;
    private bool _subscribed;
    private readonly Lock _gate = new();

    public Task<ConnectionStatus> GetStatusAsync() => Task.FromResult(CurrentStatus());

    /// <summary>
    /// 仅在有订阅者时才挂上系统事件，并在最后一个订阅者移除时摘掉——避免宿主退出后
    /// <see cref="NetworkChange"/> 仍持有本实例（<c>issues/feat-platform.md</c> 要求第 2 条）。
    /// </summary>
    public event Action<ConnectionStatus>? OnNetworkStatusChange
    {
        add
        {
            if (value is null) return;
            lock (_gate)
            {
                _statusChanged += value;
                if (_subscribed) return;

                NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
                NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
                _subscribed = true;
            }
        }
        remove
        {
            if (value is null) return;
            lock (_gate)
            {
                _statusChanged -= value;
                if (_statusChanged is not null || !_subscribed) return;

                NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
                NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
                _subscribed = false;
            }
        }
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e) => Publish();

    private void OnNetworkAddressChanged(object? sender, EventArgs e) => Publish();

    private void Publish()
    {
        Action<ConnectionStatus>? handlers;
        lock (_gate) handlers = _statusChanged;

        handlers?.Invoke(CurrentStatus());
    }

    private static ConnectionStatus CurrentStatus()
    {
        if (!NetworkInterface.GetIsNetworkAvailable())
            return new ConnectionStatus(Connected: false, ConnectionType.None);

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up) continue;
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            return nic.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Wireless80211 => new ConnectionStatus(true, ConnectionType.Wifi),
                NetworkInterfaceType.Ethernet
                    or NetworkInterfaceType.GigabitEthernet
                    or NetworkInterfaceType.FastEthernetT
                    or NetworkInterfaceType.FastEthernetFx => new ConnectionStatus(true, ConnectionType.Wifi),
                NetworkInterfaceType.Wman or NetworkInterfaceType.Wwanpp or NetworkInterfaceType.Wwanpp2
                    => new ConnectionStatus(true, ConnectionType.Cellular),
                _ => new ConnectionStatus(true, ConnectionType.Unknown),
            };
        }

        // 系统说有网但找不到具体接口：连接状态可信，类型未知。
        return new ConnectionStatus(Connected: true, ConnectionType.Unknown);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (!_subscribed) return;

            NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
            _subscribed = false;
            _statusChanged = null;
        }
    }
}
