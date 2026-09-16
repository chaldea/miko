using CoreLocation;
using Foundation;
using Miko.Native;
using Miko.Native.Geolocation;
using NativePosition = Miko.Native.Geolocation.Position;

namespace Miko.iOS.Native;

/// <summary>
/// iOS 定位，基于 <see cref="CLLocationManager"/>。
/// <para>
/// 需要在应用的 Info.plist 中声明 <c>NSLocationWhenInUseUsageDescription</c>，
/// 否则系统会直接拒绝授权请求——这属于应用侧的平台配置职责（要求第 3 条）。
/// </para>
/// </summary>
internal sealed class IosGeolocationService : NativeServiceBase, IGeolocationService, IDisposable
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, CLLocationManager> _watches = new();
    private CLLocationManager? _permissionManager;

    public Task<NativePosition> GetCurrentPositionAsync(PositionOptions? options = null)
    {
        var tcs = new TaskCompletionSource<NativePosition>(TaskCreationOptions.RunContinuationsAsynchronously);
        var timeout = options?.Timeout ?? 10000;

        // manager 只在主队列上创建、使用和释放。超时回调跑在别的线程上，不能直接碰它——
        // 否则既有数据竞争，又可能与成功路径重复释放。用一个持有单元在主队列内同步收尾。
        var cell = new SingleShotManager();
        var cts = new CancellationTokenSource(timeout);

        NSOperationQueue.MainQueue.AddOperation(() =>
        {
            try
            {
                var manager = CreateManager(options);

                manager.LocationsUpdated += (_, e) =>
                {
                    var location = e.Locations.LastOrDefault();
                    if (location is null) return;
                    if (!tcs.TrySetResult(ToPosition(location))) return;

                    // 单次定位：拿到第一个结果就停，否则 GPS 一直开着。
                    cell.StopAndDispose();
                    cts.Dispose();
                };

                manager.Failed += (_, e) =>
                {
                    if (!tcs.TrySetException(new InvalidOperationException(e.Error?.LocalizedDescription
                                                                          ?? "Location request failed."))) return;
                    cell.StopAndDispose();
                    cts.Dispose();
                };

                // 先登记再发起：超时若已经先一步 StopAndDispose 过，Adopt 会立即收掉这个
                // manager，避免它带着 GPS 悬在那里。
                if (!cell.Adopt(manager))
                {
                    StopAndDispose(manager);
                    return;
                }

                manager.RequestLocation();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        cts.Token.Register(() =>
        {
            if (tcs.TrySetException(new TimeoutException($"Location request timed out after {timeout} ms.")))
                NSOperationQueue.MainQueue.AddOperation(cell.StopAndDispose);
        });

        return tcs.Task;
    }

    /// <summary>
    /// 单次定位用的 manager 持有单元。成功、失败与超时三条路径都可能来收尾，
    /// 本类型保证 <see cref="StopAndDispose"/> 只真正执行一次，且全部在主队列上。
    /// </summary>
    private sealed class SingleShotManager
    {
        private CLLocationManager? _manager;
        private bool _finished;

        /// <summary>登记 manager；若已经收尾过则返回 <c>false</c>，调用方应自行释放。</summary>
        public bool Adopt(CLLocationManager manager)
        {
            if (_finished) return false;

            _manager = manager;
            return true;
        }

        public void StopAndDispose()
        {
            if (_finished) return;
            _finished = true;

            var manager = _manager;
            _manager = null;
            IosGeolocationService.StopAndDispose(manager);
        }
    }

    public Task<string> WatchPositionAsync(PositionOptions options, Action<NativePosition?, Exception?> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var watchId = Guid.NewGuid().ToString("n");
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        NSOperationQueue.MainQueue.AddOperation(() =>
        {
            try
            {
                var manager = CreateManager(options);

                manager.LocationsUpdated += (_, e) =>
                {
                    var location = e.Locations.LastOrDefault();
                    if (location is not null) callback(ToPosition(location), null);
                };

                manager.Failed += (_, e) => callback(null,
                    new InvalidOperationException(e.Error?.LocalizedDescription ?? "Location updates failed."));

                lock (_gate) _watches[watchId] = manager;

                manager.StartUpdatingLocation();
                tcs.TrySetResult(watchId);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    public Task ClearWatchAsync(string watchId)
    {
        CLLocationManager? manager;

        lock (_gate)
        {
            if (!_watches.Remove(watchId, out manager)) return Task.CompletedTask;
        }

        // 必须真正停掉更新，否则 GPS 常开且回调持续累积（要求第 2 条）。
        NSOperationQueue.MainQueue.AddOperation(() => StopAndDispose(manager));
        return Task.CompletedTask;
    }

    public Task<GeolocationPermissionStatus> CheckPermissionsAsync()
    {
        var status = CLLocationManager.Status;
        var state = ToState(status);

        // iOS 的「精确/粗略」是同一次授权下的精度等级（AccuracyAuthorization），
        // 不是两项独立权限；两者取同一状态。
        return Task.FromResult(new GeolocationPermissionStatus(state, state));
    }

    public Task<GeolocationPermissionStatus> RequestPermissionsAsync(GeolocationPermissions? permissions = null)
    {
        var tcs = new TaskCompletionSource<GeolocationPermissionStatus>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        NSOperationQueue.MainQueue.AddOperation(() =>
        {
            try
            {
                // 授权对话框是异步的，结果经 AuthorizationChanged 回调返回。
                // manager 必须存活到回调为止，故存成字段而不是局部变量。
                _permissionManager?.Dispose();
                var manager = _permissionManager = new CLLocationManager();

                manager.AuthorizationChanged += (_, e) =>
                {
                    // NotDetermined 是「对话框还没出结果」，不是最终答案。
                    if (e.Status == CLAuthorizationStatus.NotDetermined) return;

                    var state = ToState(e.Status);
                    tcs.TrySetResult(new GeolocationPermissionStatus(state, state));
                };

                manager.RequestWhenInUseAuthorization();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    private static CLLocationManager CreateManager(PositionOptions? options)
    {
        var manager = new CLLocationManager
        {
            DesiredAccuracy = options?.EnableHighAccuracy == true
                ? CLLocation.AccuracyBest
                : CLLocation.AccuracyHundredMeters,
        };

        // MinimumUpdateInterval 是时间间隔，而 CoreLocation 只按**距离**过滤；
        // 两者语义不同，故只在未设置时用默认（不过滤）。
        if (options?.MinimumUpdateInterval is > 0) manager.DistanceFilter = 1;

        return manager;
    }

    private static void StopAndDispose(CLLocationManager? manager)
    {
        if (manager is null) return;

        manager.StopUpdatingLocation();
        manager.Dispose();
    }

    private static PermissionState ToState(CLAuthorizationStatus status) => status switch
    {
        CLAuthorizationStatus.AuthorizedAlways or CLAuthorizationStatus.AuthorizedWhenInUse => PermissionState.Granted,
        CLAuthorizationStatus.Denied or CLAuthorizationStatus.Restricted => PermissionState.Denied,
        _ => PermissionState.Prompt,
    };

    private static NativePosition ToPosition(CLLocation location) => new(
        Timestamp: new DateTimeOffset((DateTime)location.Timestamp).ToUnixTimeMilliseconds(),
        Coords: new Coordinates(
            Latitude: location.Coordinate.Latitude,
            Longitude: location.Coordinate.Longitude,
            // 负值表示该分量无效，这是 CoreLocation 的约定。
            Accuracy: location.HorizontalAccuracy >= 0 ? location.HorizontalAccuracy : 0,
            AltitudeAccuracy: location.VerticalAccuracy >= 0 ? location.VerticalAccuracy : null,
            Altitude: location.VerticalAccuracy >= 0 ? location.Altitude : null,
            Speed: location.Speed >= 0 ? location.Speed : null,
            Heading: location.Course >= 0 ? location.Course : null,
            Course: location.Course >= 0 ? location.Course : null));

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var manager in _watches.Values) StopAndDispose(manager);
            _watches.Clear();

            _permissionManager?.Dispose();
            _permissionManager = null;
        }
    }
}
