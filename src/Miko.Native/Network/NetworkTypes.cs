namespace Miko.Native.Network;

/// <summary>网络连接类型。</summary>
public enum ConnectionType
{
    /// <summary>Wi-Fi（桌面上有线以太网也归入此类）。</summary>
    Wifi,

    /// <summary>蜂窝移动网络。</summary>
    Cellular,

    /// <summary>无连接。</summary>
    None,

    /// <summary>已连接但类型未知。</summary>
    Unknown,
}

/// <summary>网络连接状态。</summary>
/// <param name="Connected">当前是否已连接网络。</param>
/// <param name="ConnectionType">连接类型。</param>
public sealed record ConnectionStatus(bool Connected, ConnectionType ConnectionType);
