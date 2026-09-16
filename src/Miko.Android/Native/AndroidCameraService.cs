using Android;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Miko.Native;
using Miko.Native.Camera;
using AndroidUri = Android.Net.Uri;
// Android.Provider 也有一个 MediaType，会与接口的媒体类型枚举冲突。
using MediaType = Miko.Native.Camera.MediaType;

namespace Miko.Android.Native;

/// <summary>
/// Android 相机与相册，基于系统 Intent（<c>ACTION_IMAGE_CAPTURE</c>、
/// <c>ACTION_VIDEO_CAPTURE</c>、<c>ACTION_PICK</c>）。
/// <para>
/// 结果通过 <see cref="AndroidActivityResultRelay"/> 回到这里——宿主 Activity 必须把
/// <c>OnActivityResult</c> 转发进来，否则调用会一直等待。
/// </para>
/// <para>
/// 采用系统 Intent 而非 CameraX：前者零依赖、复用系统相机 UI，与 Capacitor 在 Android 上的
/// 默认行为一致；后者需要引入 AndroidX 相机套件。
/// </para>
/// </summary>
internal sealed class AndroidCameraService : NativeServiceBase, ICameraService
{
    private readonly INativeHostContext _hostContext;

    public AndroidCameraService(INativeHostContext hostContext) => _hostContext = hostContext;

    private AndroidNativeHost Host => _hostContext.RequireHost<AndroidNativeHost>();

    public async Task<MediaResult> TakePhotoAsync(TakePhotoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var host = Host;
        var activity = host.RequireActivity();

        using var intent = new Intent(MediaStore.ActionImageCapture);

        // 不传 EXTRA_OUTPUT 时系统相机只在 Intent 里回一张缩略图（thumbnail），
        // 这正是 Capacitor 在未配置 FileProvider 时的行为。要拿全尺寸原图需要应用侧
        // 配置 FileProvider 并提供输出 URI，那属于平台清单配置职责。
        var (requestCode, resultTask) = host.ActivityResults.Register();
        activity.StartActivityForResult(intent, requestCode);

        var result = await resultTask;
        if (!result.IsOk)
            throw new OperationCanceledException("The user cancelled taking a photo.");

        var uri = result.Data?.Data;

        return new MediaResult(
            Type: MediaType.Photo,
            Uri: uri?.ToString(),
            Thumbnail: string.Empty,
            Saved: options.SaveToGallery,
            WebPath: uri?.ToString(),
            Metadata: options.IncludeMetadata ? ReadMetadata(host.Context, uri, MediaType.Photo) : null);
    }

    public async Task<MediaResult> RecordVideoAsync(RecordVideoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var host = Host;
        var activity = host.RequireActivity();

        using var intent = new Intent(MediaStore.ActionVideoCapture);

        var (requestCode, resultTask) = host.ActivityResults.Register();
        activity.StartActivityForResult(intent, requestCode);

        var result = await resultTask;
        if (!result.IsOk)
            throw new OperationCanceledException("The user cancelled recording a video.");

        var uri = result.Data?.Data;

        return new MediaResult(
            Type: MediaType.Video,
            Uri: uri?.ToString(),
            Thumbnail: string.Empty,
            Saved: options.SaveToGallery,
            WebPath: uri?.ToString(),
            Metadata: options.IncludeMetadata ? ReadMetadata(host.Context, uri, MediaType.Video) : null);
    }

    public Task PlayVideoAsync(PlayVideoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var host = Host;

        using var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(AndroidUri.Parse(options.Uri), "video/*");
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        if (host.Activity is null) intent.AddFlags(ActivityFlags.NewTask);

        (host.Activity ?? (Context)host.Context).StartActivity(intent);
        return Task.CompletedTask;
    }

    public async Task<MediaResults> ChooseFromGalleryAsync(ChooseFromGalleryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var host = Host;
        var activity = host.RequireActivity();

        var mimeType = options.MediaType switch
        {
            MediaTypeSelection.Video => "video/*",
            MediaTypeSelection.All => "*/*",
            _ => "image/*",
        };

        using var intent = new Intent(Intent.ActionGetContent);
        intent.SetType(mimeType);
        intent.AddCategory(Intent.CategoryOpenable);

        if (options.MediaType == MediaTypeSelection.All)
            intent.PutExtra(Intent.ExtraMimeTypes, new[] { "image/*", "video/*" });

        if (options.AllowMultipleSelection)
            intent.PutExtra(Intent.ExtraAllowMultiple, true);

        var (requestCode, resultTask) = host.ActivityResults.Register();
        activity.StartActivityForResult(intent, requestCode);

        var result = await resultTask;
        if (!result.IsOk)
            throw new OperationCanceledException("The user cancelled choosing media.");

        var picked = new List<MediaResult>();
        var data = result.Data;

        // 多选结果在 ClipData 里，单选在 Data 里——两条路径都要处理，否则多选会返回空。
        if (data?.ClipData is { } clip)
        {
            var limit = options.Limit is > 0 ? Math.Min(options.Limit.Value, clip.ItemCount) : clip.ItemCount;
            for (var i = 0; i < limit; i++)
            {
                var uri = clip.GetItemAt(i)?.Uri;
                if (uri is not null) picked.Add(Describe(host.Context, uri, options));
            }
        }
        else if (data?.Data is { } single)
        {
            picked.Add(Describe(host.Context, single, options));
        }

        return new MediaResults(picked);
    }

    /// <summary>Android 没有系统级的「编辑 Base64 图片」入口。</summary>
    public Task<EditPhotoResult> EditPhotoAsync(EditPhotoOptions options) => UnsupportedAsync<EditPhotoResult>();

    public async Task<MediaResult> EditUriPhotoAsync(EditUriPhotoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var host = Host;
        var activity = host.RequireActivity();

        // Uri.Parse 对无法解析的字符串返回 null；放行会让下面的 edited.ToString() 崩溃。
        var uri = AndroidUri.Parse(options.Uri)
            ?? throw new ArgumentException($"Not a valid URI: {options.Uri}", nameof(options));

        using var intent = new Intent(Intent.ActionEdit);
        intent.SetDataAndType(uri, "image/*");
        intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);

        var (requestCode, resultTask) = host.ActivityResults.Register();
        activity.StartActivityForResult(Intent.CreateChooser(intent, "Edit")!, requestCode);

        var result = await resultTask;
        if (!result.IsOk)
            throw new OperationCanceledException("The user cancelled editing the photo.");

        // 编辑器可能就地覆盖（不回新 URI），此时沿用原 URI。
        var edited = result.Data?.Data ?? uri;

        return new MediaResult(
            MediaType.Photo,
            edited.ToString(),
            string.Empty,
            options.SaveToGallery,
            edited.ToString(),
            options.IncludeMetadata ? ReadMetadata(host.Context, edited, MediaType.Photo) : null);
    }

    /// <summary>iOS 专有（有限照片库授权是 iOS 的概念）。</summary>
    public Task<GalleryPhotos> PickLimitedLibraryPhotosAsync() => UnsupportedAsync<GalleryPhotos>();
    public Task<GalleryPhotos> GetLimitedLibraryPhotosAsync() => UnsupportedAsync<GalleryPhotos>();

    public Task<CameraPermissionStatus> CheckPermissionsAsync()
    {
        var context = Host.Context;

        var camera = context.CheckSelfPermission(Manifest.Permission.Camera) == Permission.Granted
            ? PermissionState.Granted
            : PermissionState.Denied;

        // Android 13 (API 33) 起读媒体用 READ_MEDIA_IMAGES，旧版用 READ_EXTERNAL_STORAGE。
        var photosPermission = OperatingSystem.IsAndroidVersionAtLeast(33)
            ? Manifest.Permission.ReadMediaImages
            : Manifest.Permission.ReadExternalStorage;

        var photos = context.CheckSelfPermission(photosPermission) == Permission.Granted
            ? PermissionState.Granted
            : PermissionState.Denied;

        return Task.FromResult(new CameraPermissionStatus(camera, photos));
    }

    /// <summary>
    /// 权限请求需要 <c>Activity.OnRequestPermissionsResult</c> 回调；宿主未接线该回调，
    /// 无法在此等待用户选择。应用应在 Activity 中自行申请后再调用相机。
    /// </summary>
    public Task<CameraPermissionStatus> RequestPermissionsAsync(CameraPluginPermissions? permissions = null)
        => UnsupportedAsync<CameraPermissionStatus>();

    private static MediaResult Describe(Context context, AndroidUri uri, ChooseFromGalleryOptions options)
    {
        var type = context.ContentResolver?.GetType(uri)?.StartsWith("video", StringComparison.OrdinalIgnoreCase) == true
            ? MediaType.Video
            : MediaType.Photo;

        return new MediaResult(
            type,
            uri.ToString(),
            string.Empty,
            Saved: false,
            WebPath: uri.ToString(),
            Metadata: options.IncludeMetadata ? ReadMetadata(context, uri, type) : null);
    }

    private static MediaMetadata? ReadMetadata(Context context, AndroidUri? uri, MediaType type)
    {
        if (uri is null) return null;

        long size = 0;
        try
        {
            using var descriptor = context.ContentResolver?.OpenFileDescriptor(uri, "r");
            size = descriptor?.StatSize ?? 0;
        }
        catch (Exception)
        {
            // URI 可能已失效或无权限；元数据是可选信息，不该因此让整次拍照失败。
        }

        var format = context.ContentResolver?.GetType(uri) ?? (type == MediaType.Video ? "video/*" : "image/*");

        return new MediaMetadata(
            Size: size,
            Duration: null,
            Format: format,
            Resolution: string.Empty,
            CreationDate: DateTimeOffset.UtcNow);
    }
}
