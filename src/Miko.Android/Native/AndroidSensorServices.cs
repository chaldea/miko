using Android;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.Locations;
using Android.OS;
using Miko.Native;
using Miko.Native.Geolocation;
using Miko.Native.Motion;
using NativePosition = Miko.Native.Geolocation.Position;

namespace Miko.Android.Native;

/// <summary>
/// Android 定位，基于 <see cref="LocationManager"/>。
/// <para>
/// 权限**请求**需要 Activity 的 <c>RequestPermissions</c> 回调；当前实现提供权限**查询**，
/// 请求则转为抛出——静默返回 Denied 会让调用方以为用户拒绝了，实际只是宿主没接回调。
/// </para>
/// </summary>
internal sealed class AndroidGeolocationService : NativeServiceBase, IGeolocationService
{
    private readonly INativeHostContext _hostContext;
    private readonly Lock _gate = new();
    private readonly Dictionary<string, LocationListener> _watches = new();

    public AndroidGeolocationService(INativeHostContext hostContext) => _hostContext = hostContext;

    private AndroidNativeHost Host => _hostContext.RequireHost<AndroidNativeHost>();

    private LocationManager Manager =>
        Host.Context.GetSystemService(Context.LocationService) as LocationManager
        ?? throw new InvalidOperationException("LocationManager is unavailable.");

    public Task<NativePosition> GetCurrentPositionAsync(PositionOptions? options = null)
    {
        EnsureLocationPermission();

        var manager = Manager;
        var timeout = options?.Timeout ?? 10000;
        var tcs = new TaskCompletionSource<NativePosition>(TaskCreationOptions.RunContinuationsAsynchronously);

        var provider = SelectProvider(manager, options?.EnableHighAccuracy ?? false);
        if (provider is null)
        {
            tcs.TrySetException(new InvalidOperationException("No enabled location provider is available."));
            return tcs.Task;
        }

        // 先看缓存位置：MaximumAge 内的旧定位可以直接用，省一次 GPS 定位。
        var maximumAge = options?.MaximumAge ?? 0;
        if (maximumAge > 0)
        {
            var last = GetLastKnownLocation(manager, provider);
            if (last is not null)
            {
                var age = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - last.Time;
                if (age <= maximumAge)
                {
                    tcs.TrySetResult(ToPosition(last));
                    return tcs.Task;
                }
            }
        }

        LocationListener? listener = null;
        var cts = new CancellationTokenSource(timeout);

        listener = new LocationListener(location =>
        {
            if (!tcs.TrySetResult(ToPosition(location))) return;

            // 单次定位：拿到第一个结果就必须撤掉监听，否则 GPS 一直开着。
            RemoveUpdatesSafely(manager, listener!);
            cts.Dispose();
        });

        // 先挂监听再登记超时。反过来的话，超时若恰好落在两者之间，RemoveUpdates 会先于
        // RequestUpdates 执行——随后监听被挂上却再没人撤，GPS 一直开着。
        RequestUpdates(manager, provider, 0, 0, listener);

        cts.Token.Register(() =>
        {
            if (tcs.TrySetException(new TimeoutException($"Location request timed out after {timeout} ms.")))
                RemoveUpdatesSafely(manager, listener!);
        });

        return tcs.Task;
    }

    public Task<string> WatchPositionAsync(PositionOptions options, Action<NativePosition?, Exception?> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        EnsureLocationPermission();

        var manager = Manager;
        var provider = SelectProvider(manager, options?.EnableHighAccuracy ?? false)
            ?? throw new InvalidOperationException("No enabled location provider is available.");

        var watchId = Guid.NewGuid().ToString("n");
        var listener = new LocationListener(location => callback(ToPosition(location), null));

        lock (_gate) _watches[watchId] = listener;

        var interval = options?.MinimumUpdateInterval ?? options?.Interval ?? 1000;
        RequestUpdates(manager, provider, interval, 0, listener);

        return Task.FromResult(watchId);
    }

    public Task ClearWatchAsync(string watchId)
    {
        LocationListener? listener;

        lock (_gate)
        {
            if (!_watches.Remove(watchId, out listener)) return Task.CompletedTask;
        }

        // 必须真正撤掉系统监听，否则 GPS 常开且回调持续累积（issue 要求第 2 条）。
        RemoveUpdatesSafely(Manager, listener);
        listener.Dispose();
        return Task.CompletedTask;
    }

    public Task<GeolocationPermissionStatus> CheckPermissionsAsync()
        => Task.FromResult(new GeolocationPermissionStatus(
            Location: ToState(Manifest.Permission.AccessFineLocation),
            CoarseLocation: ToState(Manifest.Permission.AccessCoarseLocation)));

    /// <summary>
    /// 权限请求需要 <c>Activity.RequestPermissions</c> + <c>OnRequestPermissionsResult</c> 回调；
    /// 宿主未接线该回调，无法在此等待用户选择。应用应在 Activity 中自行申请后再调用定位。
    /// </summary>
    public Task<GeolocationPermissionStatus> RequestPermissionsAsync(GeolocationPermissions? permissions = null)
        => UnsupportedAsync<GeolocationPermissionStatus>();

    private void EnsureLocationPermission()
    {
        if (ToState(Manifest.Permission.AccessFineLocation) == PermissionState.Granted) return;
        if (ToState(Manifest.Permission.AccessCoarseLocation) == PermissionState.Granted) return;

        throw new UnauthorizedAccessException(
            "Location permission has not been granted. Request ACCESS_FINE_LOCATION or " +
            "ACCESS_COARSE_LOCATION from the hosting Activity before using geolocation.");
    }

    // 用平台自带的 Context.CheckSelfPermission，避免为一次权限查询引入 AndroidX 依赖。
    private PermissionState ToState(string permission)
        => Host.Context.CheckSelfPermission(permission) == Permission.Granted
            ? PermissionState.Granted
            : PermissionState.Denied;

    private static string? SelectProvider(LocationManager manager, bool highAccuracy)
    {
        if (highAccuracy && manager.IsProviderEnabled(LocationManager.GpsProvider))
            return LocationManager.GpsProvider;

        if (manager.IsProviderEnabled(LocationManager.NetworkProvider))
            return LocationManager.NetworkProvider;

        return manager.IsProviderEnabled(LocationManager.GpsProvider) ? LocationManager.GpsProvider : null;
    }

    // 权限已在调用前校验；这些调用点用 try/catch 兜住系统侧的竞态（用户中途撤销权限）。
    private static Location? GetLastKnownLocation(LocationManager manager, string provider)
    {
        try { return manager.GetLastKnownLocation(provider); }
        catch (Java.Lang.SecurityException) { return null; }
    }

    private static void RequestUpdates(
        LocationManager manager, string provider, long minTimeMs, float minDistanceM, LocationListener listener)
    {
        manager.RequestLocationUpdates(provider, minTimeMs, minDistanceM, listener, Looper.MainLooper);
    }

    private static void RemoveUpdatesSafely(LocationManager manager, LocationListener listener)
    {
        try { manager.RemoveUpdates(listener); }
        catch (Java.Lang.SecurityException) { /* 权限被撤销，监听本就已停 */ }
    }

    private static NativePosition ToPosition(Location location) => new(
        Timestamp: location.Time,
        Coords: new Coordinates(
            Latitude: location.Latitude,
            Longitude: location.Longitude,
            Accuracy: location.HasAccuracy ? location.Accuracy : 0,
            Altitude: location.HasAltitude ? location.Altitude : null,
            Speed: location.HasSpeed ? location.Speed : null,
            Heading: location.HasBearing ? location.Bearing : null));

    /// <summary>把 Android 的位置回调桥接为委托。</summary>
    private sealed class LocationListener : Java.Lang.Object, ILocationListener
    {
        private readonly Action<Location> _onLocation;

        public LocationListener(Action<Location> onLocation) => _onLocation = onLocation;

        public void OnLocationChanged(Location location) => _onLocation(location);

        // API 29 起这些成员在接口上有默认实现，但 C# 绑定仍要求显式实现。
        public void OnProviderDisabled(string provider) { }
        public void OnProviderEnabled(string provider) { }
        public void OnStatusChanged(string? provider, [global::Android.Runtime.GeneratedEnum] Availability status, Bundle? extras) { }
    }
}

/// <summary>
/// Android 运动传感器，基于 <see cref="SensorManager"/>。
/// <para>
/// 只在有订阅者时注册监听并在最后一个订阅者移除时注销——传感器常开是明显的耗电问题。
/// </para>
/// </summary>
internal sealed class AndroidMotionService : IMotionService, IDisposable
{
    private readonly INativeHostContext _hostContext;
    private readonly Lock _gate = new();

    private Action<AccelEvent>? _accel;
    private Action<RotationRate>? _orientation;
    private SensorListener? _listener;

    public AndroidMotionService(INativeHostContext hostContext) => _hostContext = hostContext;

    private SensorManager? Manager =>
        (_hostContext.Host as AndroidNativeHost)?.Context.GetSystemService(Context.SensorService) as SensorManager;

    public event Action<AccelEvent>? OnAccel
    {
        add { if (value is not null) { lock (_gate) { _accel += value; EnsureListening(); } } }
        remove { if (value is not null) { lock (_gate) { _accel -= value; StopIfIdle(); } } }
    }

    public event Action<RotationRate>? OnOrientation
    {
        add { if (value is not null) { lock (_gate) { _orientation += value; EnsureListening(); } } }
        remove { if (value is not null) { lock (_gate) { _orientation -= value; StopIfIdle(); } } }
    }

    private void EnsureListening()
    {
        if (_listener is not null) return;

        var manager = Manager;
        if (manager is null) return;

        _listener = new SensorListener(this);

        Register(manager, SensorType.Accelerometer);
        Register(manager, SensorType.LinearAcceleration);
        Register(manager, SensorType.Gyroscope);
        Register(manager, SensorType.RotationVector);
    }

    private void Register(SensorManager manager, SensorType type)
    {
        var sensor = manager.GetDefaultSensor(type);
        if (sensor is not null) manager.RegisterListener(_listener, sensor, SensorDelay.Ui);
    }

    private void StopIfIdle()
    {
        if (_accel is not null || _orientation is not null) return;
        if (_listener is null) return;

        Manager?.UnregisterListener(_listener);
        _listener.Dispose();
        _listener = null;
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _accel = null;
            _orientation = null;
            StopIfIdle();
        }
    }

    /// <summary>
    /// 汇总各传感器的最新读数并合成 <see cref="AccelEvent"/>。
    /// Android 的传感器是**分别**回调的（加速度、陀螺仪各来各的），而接口要求一次给出全部分量，
    /// 因此必须在这里做一次合并。
    /// </summary>
    private sealed class SensorListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly AndroidMotionService _owner;

        private float[] _accelWithGravity = new float[3];
        private float[] _accelLinear = new float[3];
        private float[] _gyro = new float[3];
        private long _lastTimestampNs;

        public SensorListener(AndroidMotionService owner) => _owner = owner;

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values is null || e.Sensor is null) return;

            var values = e.Values;

            switch (e.Sensor.Type)
            {
                case SensorType.Accelerometer:
                    CopyTriplet(values, _accelWithGravity);
                    break;
                case SensorType.LinearAcceleration:
                    CopyTriplet(values, _accelLinear);
                    break;
                case SensorType.Gyroscope:
                    CopyTriplet(values, _gyro);
                    break;
                case SensorType.RotationVector:
                    EmitOrientation(values);
                    return;
                default:
                    return;
            }

            // 采样间隔由相邻事件的纳秒时间戳求得（传感器不直接给间隔）。
            var intervalMs = _lastTimestampNs == 0 ? 0 : (e.Timestamp - _lastTimestampNs) / 1_000_000.0;
            _lastTimestampNs = e.Timestamp;

            Action<AccelEvent>? handlers;
            lock (_owner._gate) handlers = _owner._accel;
            if (handlers is null) return;

            handlers(new AccelEvent(
                Acceleration: new Acceleration(_accelLinear[0], _accelLinear[1], _accelLinear[2]),
                AccelerationIncludingGravity: new Acceleration(
                    _accelWithGravity[0], _accelWithGravity[1], _accelWithGravity[2]),
                // 陀螺仪给的是弧度/秒，接口约定为度/秒。
                RotationRate: new RotationRate(
                    RadiansToDegrees(_gyro[2]), RadiansToDegrees(_gyro[0]), RadiansToDegrees(_gyro[1])),
                Interval: intervalMs));
        }

        private void EmitOrientation(IList<float> rotationVector)
        {
            Action<RotationRate>? handlers;
            lock (_owner._gate) handlers = _owner._orientation;
            if (handlers is null) return;

            var rotationMatrix = new float[9];
            var vector = rotationVector.Take(Math.Min(4, rotationVector.Count)).ToArray();
            SensorManager.GetRotationMatrixFromVector(rotationMatrix, vector);

            var values = new float[3];
            SensorManager.GetOrientation(rotationMatrix, values);

            // GetOrientation 返回 [azimuth, pitch, roll]（弧度），对应 alpha/beta/gamma（度）。
            handlers(new RotationRate(
                RadiansToDegrees(values[0]), RadiansToDegrees(values[1]), RadiansToDegrees(values[2])));
        }

        private static void CopyTriplet(IList<float> source, float[] target)
        {
            for (var i = 0; i < 3 && i < source.Count; i++) target[i] = source[i];
        }

        private static double RadiansToDegrees(float radians) => radians * 180.0 / Math.PI;
    }
}
