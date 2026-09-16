namespace Miko.Native.Geolocation;

/// <summary>位置坐标。</summary>
/// <param name="Latitude">纬度（度）。</param>
/// <param name="Longitude">经度（度）。</param>
/// <param name="Accuracy">水平精度（米）。</param>
/// <param name="AltitudeAccuracy">海拔精度（米）。</param>
/// <param name="Altitude">海拔（米）。</param>
/// <param name="Speed">速度（米/秒）。</param>
/// <param name="Heading">移动方向（度）。</param>
/// <param name="MagneticHeading">磁北方向（度，iOS）。</param>
/// <param name="TrueHeading">真北方向（度，iOS）。</param>
/// <param name="HeadingAccuracy">方向精度（度，iOS）。</param>
/// <param name="Course">航向（度，iOS）。</param>
public sealed record Coordinates(
    double Latitude,
    double Longitude,
    double Accuracy,
    double? AltitudeAccuracy = null,
    double? Altitude = null,
    double? Speed = null,
    double? Heading = null,
    double? MagneticHeading = null,
    double? TrueHeading = null,
    double? HeadingAccuracy = null,
    double? Course = null);

/// <summary>一次定位结果。</summary>
/// <param name="Timestamp">采样时间（Unix 毫秒时间戳）。</param>
/// <param name="Coords">坐标。</param>
public sealed record Position(long Timestamp, Coordinates Coords);

/// <summary>定位选项。</summary>
public sealed record PositionOptions
{
    /// <summary>是否启用高精度定位（GPS）。开启后耗电显著增加。</summary>
    public bool EnableHighAccuracy { get; init; }

    /// <summary>超时（毫秒），默认 10000。</summary>
    public int Timeout { get; init; } = 10000;

    /// <summary>可接受的缓存位置最大年龄（毫秒）。</summary>
    public int MaximumAge { get; init; }

    /// <summary>两次位置更新之间的最小间隔（毫秒，Android）。</summary>
    public int? MinimumUpdateInterval { get; init; }

    /// <summary>期望的位置更新间隔（毫秒，Android）。</summary>
    public int? Interval { get; init; }

    /// <summary>高精度定位不可用时是否退回粗略定位。</summary>
    public bool EnableLocationFallback { get; init; }
}

/// <summary>定位权限状态。</summary>
/// <param name="Location">精确定位权限。</param>
/// <param name="CoarseLocation">粗略定位权限。</param>
public sealed record GeolocationPermissionStatus(
    PermissionState Location,
    PermissionState CoarseLocation);

/// <summary>要请求的定位权限种类。</summary>
public sealed record GeolocationPermissions
{
    /// <summary>要请求的权限名称集合，如 <c>location</c>、<c>coarseLocation</c>。
    /// 为空时请求全部定位权限。</summary>
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}
