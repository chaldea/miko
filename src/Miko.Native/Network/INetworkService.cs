namespace Miko.Native.Network;

/// <summary>
/// 网络连接状态。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/native/network#api">Network API</see>。
/// 支持平台：Android、iOS、Windowing。
/// </summary>
public interface INetworkService
{
    /// <summary>查询当前网络连接状态。</summary>
    Task<ConnectionStatus> GetStatusAsync();

    /// <summary>网络状态变化。</summary>
    event Action<ConnectionStatus>? OnNetworkStatusChange;
}
