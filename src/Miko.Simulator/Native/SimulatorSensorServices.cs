using Microsoft.Extensions.Logging;
using Miko.Native;
using Miko.Native.Camera;
using Miko.Native.Geolocation;
using Miko.Native.Motion;

namespace Miko.Simulator.Native;

/// <summary>
/// 模拟器相机：返回可辨识的仿真结果，让「调用相机 → 拿到结果 → 渲染缩略图」这条链路
/// 在桌面上就能跑通。所有 URI 都带 <c>miko-simulator://</c> 前缀，不会被误当成真实文件。
/// </summary>
internal sealed class SimulatorCameraService : SimulatorNativeServiceBase, ICameraService
{
    /// <summary>1×1 透明 PNG 的 Base64，作为缩略图占位。</summary>
    private const string PlaceholderPng =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    public SimulatorCameraService(INativeHostContext hostContext, ILogger<SimulatorCameraService>? logger)
        : base(hostContext, logger) { }

    public Task<MediaResult> TakePhotoAsync(TakePhotoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Log(nameof(TakePhotoAsync), $"{options.CameraDirection} q={options.Quality}");

        return Task.FromResult(SamplePhoto(options.IncludeMetadata, options.SaveToGallery));
    }

    public Task<MediaResult> RecordVideoAsync(RecordVideoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Log(nameof(RecordVideoAsync));

        return Task.FromResult(new MediaResult(
            Type: MediaType.Video,
            Uri: "miko-simulator://media/video.mp4",
            Thumbnail: PlaceholderPng,
            Saved: options.SaveToGallery,
            WebPath: "miko-simulator://media/video.mp4",
            Metadata: options.IncludeMetadata
                ? new MediaMetadata(1024 * 1024, 12.5, "mp4", "1920x1080", DateTimeOffset.UtcNow)
                : null));
    }

    public Task PlayVideoAsync(PlayVideoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Log(nameof(PlayVideoAsync), options.Uri);
        return Task.CompletedTask;
    }

    public Task<MediaResults> ChooseFromGalleryAsync(ChooseFromGalleryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Log(nameof(ChooseFromGalleryAsync), $"{options.MediaType} multi={options.AllowMultipleSelection}");

        // 单选时返回一条，多选时返回 Limit（默认 2）条——让应用的多选渲染路径也能被走到。
        var count = options.AllowMultipleSelection ? Math.Max(1, options.Limit ?? 2) : 1;

        var results = Enumerable.Range(0, count)
            .Select(i => options.MediaType == MediaTypeSelection.Video
                ? new MediaResult(MediaType.Video, $"miko-simulator://media/video-{i}.mp4", PlaceholderPng, false,
                    $"miko-simulator://media/video-{i}.mp4")
                : new MediaResult(MediaType.Photo, $"miko-simulator://media/photo-{i}.jpg", PlaceholderPng, false,
                    $"miko-simulator://media/photo-{i}.jpg"))
            .ToList();

        return Task.FromResult(new MediaResults(results));
    }

    /// <summary>模拟器不做真正的编辑，原样返回输入图片。</summary>
    public Task<EditPhotoResult> EditPhotoAsync(EditPhotoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Log(nameof(EditPhotoAsync));
        return Task.FromResult(new EditPhotoResult(options.InputImage));
    }

    public Task<MediaResult> EditUriPhotoAsync(EditUriPhotoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        Log(nameof(EditUriPhotoAsync), options.Uri);

        return Task.FromResult(new MediaResult(
            MediaType.Photo, options.Uri, PlaceholderPng, options.SaveToGallery, options.Uri));
    }

    public Task<GalleryPhotos> PickLimitedLibraryPhotosAsync()
    {
        Log(nameof(PickLimitedLibraryPhotosAsync));
        return Task.FromResult(SampleGallery());
    }

    public Task<GalleryPhotos> GetLimitedLibraryPhotosAsync()
    {
        Log(nameof(GetLimitedLibraryPhotosAsync));
        return Task.FromResult(SampleGallery());
    }

    /// <summary>模拟器上权限恒为已授权——否则开发者在桌面上永远走不到成功分支。</summary>
    public Task<CameraPermissionStatus> CheckPermissionsAsync()
        => Task.FromResult(new CameraPermissionStatus(PermissionState.Granted, PermissionState.Granted));

    public Task<CameraPermissionStatus> RequestPermissionsAsync(CameraPluginPermissions? permissions = null)
        => Task.FromResult(new CameraPermissionStatus(PermissionState.Granted, PermissionState.Granted));

    private static MediaResult SamplePhoto(bool includeMetadata, bool saveToGallery) => new(
        Type: MediaType.Photo,
        Uri: "miko-simulator://media/photo.jpg",
        Thumbnail: PlaceholderPng,
        Saved: saveToGallery,
        WebPath: "miko-simulator://media/photo.jpg",
        Metadata: includeMetadata
            ? new MediaMetadata(256 * 1024, null, "jpeg", "4032x3024", DateTimeOffset.UtcNow)
            : null);

    private static GalleryPhotos SampleGallery() => new(
    [
        new GalleryPhoto("miko-simulator://media/photo-0.jpg", "miko-simulator://media/photo-0.jpg", "jpeg"),
        new GalleryPhoto("miko-simulator://media/photo-1.jpg", "miko-simulator://media/photo-1.jpg", "jpeg"),
    ]);
}

/// <summary>
/// 模拟器定位：返回一个固定样本坐标，并可按需周期性推送轻微抖动的位置，
/// 使「监听位置 → 更新地图」这条链路在桌面上可验证。
/// </summary>
internal sealed class SimulatorGeolocationService : SimulatorNativeServiceBase, IGeolocationService
{
    // 上海人民广场附近，一个明确可辨的固定样本点。
    private const double SampleLatitude = 31.2304;
    private const double SampleLongitude = 121.4737;

    private readonly Lock _gate = new();
    private readonly Dictionary<string, CancellationTokenSource> _watches = new();

    public SimulatorGeolocationService(INativeHostContext hostContext, ILogger<SimulatorGeolocationService>? logger)
        : base(hostContext, logger) { }

    public Task<Position> GetCurrentPositionAsync(PositionOptions? options = null)
    {
        Log(nameof(GetCurrentPositionAsync));
        return Task.FromResult(SamplePosition(0));
    }

    public Task<string> WatchPositionAsync(PositionOptions options, Action<Position?, Exception?> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var watchId = Guid.NewGuid().ToString("n");
        var cts = new CancellationTokenSource();

        lock (_gate) _watches[watchId] = cts;
        Log(nameof(WatchPositionAsync), watchId);

        var interval = Math.Max(200, options?.Interval ?? 1000);

        _ = Task.Run(async () =>
        {
            var tick = 0;
            try
            {
                // 首次立即回调，随后按间隔推送——与真机「先给当前位置再持续更新」一致。
                while (!cts.Token.IsCancellationRequested)
                {
                    callback(SamplePosition(tick++), null);
                    await Task.Delay(interval, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // ClearWatchAsync 正常取消，不是错误。
            }
            catch (ObjectDisposedException)
            {
                // 防御性：正常路径到不了这里（Cancel 是同步的，Delay 在 Dispose 之前就已取消），
                // 但退订与释放的相对时序不值得赌——真走到了也是正常收尾，不是定位故障，
                // 绝不能当成错误丢给用户回调。
            }
            catch (Exception ex)
            {
                callback(null, ex);
            }
            finally
            {
                // 由循环自己释放 CTS——它是最后一个用到 token 的人，在这里释放不必推敲
                // 「退订方先 Dispose 会不会撞上仍在 Delay 的循环」这类时序问题。
                cts.Dispose();
            }
        }, cts.Token);

        return Task.FromResult(watchId);
    }

    public Task ClearWatchAsync(string watchId)
    {
        CancellationTokenSource? cts;
        lock (_gate)
        {
            if (!_watches.Remove(watchId, out cts)) return Task.CompletedTask;
        }

        // 必须真正停掉推送循环，否则页面反复进出会累积监听（issue 要求第 2 条）。
        // 只 Cancel 不 Dispose：释放交给循环自身的 finally，由最后一个用 token 的人负责。
        cts.Cancel();
        Log(nameof(ClearWatchAsync), watchId);
        return Task.CompletedTask;
    }

    public Task<GeolocationPermissionStatus> CheckPermissionsAsync()
        => Task.FromResult(new GeolocationPermissionStatus(PermissionState.Granted, PermissionState.Granted));

    public Task<GeolocationPermissionStatus> RequestPermissionsAsync(GeolocationPermissions? permissions = null)
        => Task.FromResult(new GeolocationPermissionStatus(PermissionState.Granted, PermissionState.Granted));

    /// <summary>样本位置。<paramref name="tick"/> 制造微小漂移，使连续更新可见。</summary>
    private static Position SamplePosition(int tick) => new(
        Timestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        Coords: new Coordinates(
            Latitude: SampleLatitude + tick * 0.0001,
            Longitude: SampleLongitude + tick * 0.0001,
            Accuracy: 10,
            Altitude: 12,
            Speed: 0,
            Heading: 0));
}

/// <summary>
/// 模拟器运动传感器。桌面没有加速度计，事件永不触发——
/// 编造抖动数据会让「摇一摇」之类的逻辑在桌面上被误触发。
/// </summary>
internal sealed class SimulatorMotionService : IMotionService
{
    public event Action<AccelEvent>? OnAccel { add { } remove { } }
    public event Action<RotationRate>? OnOrientation { add { } remove { } }
}
