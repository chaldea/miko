namespace Miko.Native.Geolocation;

/// <summary>未注册平台实现时的 <see cref="IGeolocationService"/> 占位实现，调用即抛。</summary>
public sealed class NullGeolocationService : NativeServiceBase, IGeolocationService
{
    public Task<Position> GetCurrentPositionAsync(PositionOptions? options = null)
        => UnsupportedAsync<Position>();

    public Task<string> WatchPositionAsync(PositionOptions options, Action<Position?, Exception?> callback)
        => UnsupportedAsync<string>();

    public Task ClearWatchAsync(string watchId) => UnsupportedAsync();

    public Task<GeolocationPermissionStatus> CheckPermissionsAsync()
        => UnsupportedAsync<GeolocationPermissionStatus>();

    public Task<GeolocationPermissionStatus> RequestPermissionsAsync(GeolocationPermissions? permissions = null)
        => UnsupportedAsync<GeolocationPermissionStatus>();
}
