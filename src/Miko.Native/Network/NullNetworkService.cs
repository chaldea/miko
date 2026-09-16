namespace Miko.Native.Network;

/// <summary>未注册平台实现时的 <see cref="INetworkService"/> 占位实现，调用即抛。</summary>
public sealed class NullNetworkService : NativeServiceBase, INetworkService
{
    public Task<ConnectionStatus> GetStatusAsync() => UnsupportedAsync<ConnectionStatus>();

    public event Action<ConnectionStatus>? OnNetworkStatusChange { add { } remove { } }
}
