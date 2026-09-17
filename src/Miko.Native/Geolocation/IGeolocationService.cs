namespace Miko.Native.Geolocation;

/// <summary>
/// 定位与位置监听。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/v8/native/geolocation#api">Geolocation API</see>。
/// 支持平台：Android、iOS、Windowing。
/// </summary>
public interface IGeolocationService
{
    /// <summary>获取当前 GPS 位置。</summary>
    Task<Position> GetCurrentPositionAsync(PositionOptions? options = null);

    /// <summary>
    /// 监听位置变化，返回 Watch ID。回调的两个参数互斥：成功时为位置、失败时为异常。
    /// 用 <see cref="ClearWatchAsync"/> 解除监听。
    /// </summary>
    Task<string> WatchPositionAsync(PositionOptions options, Action<Position?, Exception?> callback);

    /// <summary>清除位置监听。</summary>
    Task ClearWatchAsync(string watchId);

    /// <summary>查询定位权限。</summary>
    Task<GeolocationPermissionStatus> CheckPermissionsAsync();

    /// <summary>请求定位权限。</summary>
    Task<GeolocationPermissionStatus> RequestPermissionsAsync(GeolocationPermissions? permissions = null);
}
