using AVFoundation;
using Foundation;
using MobileCoreServices;
using Miko.Native;
using Miko.Native.Camera;
using Photos;
using UIKit;
using MediaType = Miko.Native.Camera.MediaType;

namespace Miko.iOS.Native;

/// <summary>
/// iOS 相机与相册，基于 <see cref="UIImagePickerController"/>。
/// <para>
/// 需要在应用的 Info.plist 中声明 <c>NSCameraUsageDescription</c>、
/// <c>NSPhotoLibraryUsageDescription</c> 与 <c>NSMicrophoneUsageDescription</c>（录像），
/// 否则调用会直接崩溃——这属于应用侧的平台配置职责（要求第 3 条）。
/// </para>
/// </summary>
internal sealed class IosCameraService : NativeServiceBase, ICameraService
{
    private readonly INativeHostContext _hostContext;

    public IosCameraService(INativeHostContext hostContext) => _hostContext = hostContext;

    private IosNativeHost Host => _hostContext.RequireHost<IosNativeHost>();

    public async Task<MediaResult> TakePhotoAsync(TakePhotoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var info = await PresentPickerAsync(picker =>
        {
            picker.SourceType = UIImagePickerControllerSourceType.Camera;
            picker.MediaTypes = [UTType.Image];
            picker.CameraDevice = options.CameraDirection == CameraDirection.Front
                ? UIImagePickerControllerCameraDevice.Front
                : UIImagePickerControllerCameraDevice.Rear;
            picker.AllowsEditing = options.Editable == PhotoEditMode.InApp;
            picker.ModalPresentationStyle = options.PresentationStyle == CameraPresentationStyle.Popover
                ? UIModalPresentationStyle.Popover
                : UIModalPresentationStyle.FullScreen;
        });

        var image = ResolveImage(info, options.Editable == PhotoEditMode.InApp);
        if (image is null) throw new InvalidOperationException("The picker returned no image.");

        // 回调参数非空标注，但保存结果这里不关心（失败不应让整次拍照作废）。
        if (options.SaveToGallery) image.SaveToPhotosAlbum((_, _) => { });

        var uri = (info.ValueForKey(UIImagePickerController.ImageUrl) as NSUrl)?.AbsoluteString;

        return new MediaResult(
            Type: MediaType.Photo,
            Uri: uri,
            Thumbnail: EncodeThumbnail(image, options.EncodingType, options.Quality),
            Saved: options.SaveToGallery,
            WebPath: uri,
            Metadata: options.IncludeMetadata ? DescribeImage(image, options.EncodingType) : null);
    }

    public async Task<MediaResult> RecordVideoAsync(RecordVideoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var info = await PresentPickerAsync(picker =>
        {
            picker.SourceType = UIImagePickerControllerSourceType.Camera;
            picker.MediaTypes = [UTType.Movie];
            picker.CameraCaptureMode = UIImagePickerControllerCameraCaptureMode.Video;
        });

        var url = info.ValueForKey(UIImagePickerController.MediaURL) as NSUrl;
        if (url is null) throw new InvalidOperationException("The picker returned no video.");

        return new MediaResult(
            Type: MediaType.Video,
            Uri: url.AbsoluteString,
            Thumbnail: string.Empty,
            Saved: options.SaveToGallery,
            WebPath: url.AbsoluteString,
            Metadata: options.IncludeMetadata ? DescribeVideo(url) : null);
    }

    public Task PlayVideoAsync(PlayVideoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var host = Host;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try
            {
                var player = new AVPlayer(NSUrl.FromString(options.Uri)
                    ?? throw new ArgumentException($"Not a valid URL: {options.Uri}", nameof(options)));

                var controller = new AVKit.AVPlayerViewController { Player = player };

                host.TopViewController.PresentViewController(controller, animated: true, completionHandler: () =>
                {
                    player.Play();
                    tcs.TrySetResult();
                });
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    public async Task<MediaResults> ChooseFromGalleryAsync(ChooseFromGalleryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // UIImagePickerController 只能单选。多选需要 PHPickerViewController（iOS 14+），
        // 其结果是异步逐项加载的，与本服务的一次性返回语义差距较大，故当前只支持单选。
        if (options.AllowMultipleSelection)
            throw new PlatformNotSupportedException(
                $"{nameof(IosCameraService)} does not support multiple selection yet; " +
                "it requires PHPickerViewController's asynchronous item loading.");

        // UTType.* 是 NSString 常量，而 MediaTypes 取 string[]——显式转一次。
        string[] mediaTypes = options.MediaType switch
        {
            MediaTypeSelection.Video => [UTType.Movie],
            MediaTypeSelection.All => [UTType.Image, UTType.Movie],
            _ => [UTType.Image],
        };

        var info = await PresentPickerAsync(picker =>
        {
            picker.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
            picker.MediaTypes = mediaTypes;
            picker.AllowsEditing = options.Editable == PhotoEditMode.InApp;
        });

        var url = info.ValueForKey(UIImagePickerController.MediaURL) as NSUrl;
        var isVideo = (info.ValueForKey(UIImagePickerController.MediaType) as NSString)?.ToString() == UTType.Movie;

        if (isVideo)
        {
            if (url is null) throw new InvalidOperationException("The picker returned no video.");

            return new MediaResults([
                new MediaResult(MediaType.Video, url.AbsoluteString, string.Empty, false, url.AbsoluteString,
                    options.IncludeMetadata ? DescribeVideo(url) : null),
            ]);
        }

        var image = ResolveImage(info, options.Editable == PhotoEditMode.InApp);
        if (image is null) throw new InvalidOperationException("The picker returned no image.");

        var imageUrl = (info.ValueForKey(UIImagePickerController.ImageUrl) as NSUrl)?.AbsoluteString;

        return new MediaResults([
            new MediaResult(MediaType.Photo, imageUrl,
                EncodeThumbnail(image, EncodingType.Jpeg, options.Quality), false, imageUrl,
                options.IncludeMetadata ? DescribeImage(image, EncodingType.Jpeg) : null),
        ]);
    }

    /// <summary>iOS 没有系统级的图片编辑器 Intent（<c>AllowsEditing</c> 只在选取流程中裁剪）。</summary>
    public Task<EditPhotoResult> EditPhotoAsync(EditPhotoOptions options) => UnsupportedAsync<EditPhotoResult>();
    public Task<MediaResult> EditUriPhotoAsync(EditUriPhotoOptions options) => UnsupportedAsync<MediaResult>();

    /// <summary>
    /// 呈现系统的「管理所选照片」界面。
    /// <para>
    /// <c>PHPhotoLibrary.PresentLimitedLibraryPicker</c> 未出现在当前 .NET for iOS 绑定中，
    /// 无法在不引入手写 P/Invoke 的前提下调用；明确抛出而不是静默返回当前库内容
    /// （那会让调用方以为用户已经改过选择）。改用
    /// <see cref="GetLimitedLibraryPhotosAsync"/> 读取当前授权范围。
    /// </para>
    /// </summary>
    public Task<GalleryPhotos> PickLimitedLibraryPhotosAsync() => UnsupportedAsync<GalleryPhotos>();

    public Task<GalleryPhotos> GetLimitedLibraryPhotosAsync() => Task.FromResult(ReadLimitedLibrary());

    public Task<CameraPermissionStatus> CheckPermissionsAsync()
    {
        var camera = AVCaptureDevice.GetAuthorizationStatus(AVAuthorizationMediaType.Video) switch
        {
            AVAuthorizationStatus.Authorized => PermissionState.Granted,
            AVAuthorizationStatus.Denied or AVAuthorizationStatus.Restricted => PermissionState.Denied,
            _ => PermissionState.Prompt,
        };

        var photos = PHPhotoLibrary.GetAuthorizationStatus(PHAccessLevel.ReadWrite) switch
        {
            PHAuthorizationStatus.Authorized or PHAuthorizationStatus.Limited => PermissionState.Granted,
            PHAuthorizationStatus.Denied or PHAuthorizationStatus.Restricted => PermissionState.Denied,
            _ => PermissionState.Prompt,
        };

        return Task.FromResult(new CameraPermissionStatus(camera, photos));
    }

    public async Task<CameraPermissionStatus> RequestPermissionsAsync(CameraPluginPermissions? permissions = null)
    {
        var wanted = permissions?.Permissions ?? [];
        var wantCamera = wanted.Count == 0 || wanted.Contains("camera", StringComparer.OrdinalIgnoreCase);
        var wantPhotos = wanted.Count == 0 || wanted.Contains("photos", StringComparer.OrdinalIgnoreCase);

        if (wantCamera) await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);

        if (wantPhotos)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            PHPhotoLibrary.RequestAuthorization(PHAccessLevel.ReadWrite, _ => tcs.TrySetResult());
            await tcs.Task;
        }

        return await CheckPermissionsAsync();
    }

    /// <summary>呈现 picker 并等待用户选择；取消时抛 <see cref="OperationCanceledException"/>。</summary>
    private Task<NSDictionary> PresentPickerAsync(Action<UIImagePickerController> configure)
    {
        var host = Host;
        var tcs = new TaskCompletionSource<NSDictionary>(TaskCreationOptions.RunContinuationsAsynchronously);

        UIApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try
            {
                var picker = new UIImagePickerController();
                configure(picker);

                var pickerDelegate = new PickerDelegate(tcs);
                picker.Delegate = pickerDelegate;

                // UIKit 的 delegate 属性是**弱引用**（避免循环引用），而本方法返回后就没有别的
                // 托管引用指向 delegate 了。相机界面开着的几秒里一次 GC 就会回收它，
                // 回调永不触发、await 永远挂住。存进字段撑到回调完成为止。
                lock (_pickerGate) _livePickerDelegates.Add(pickerDelegate);
                pickerDelegate.Completed += () =>
                {
                    lock (_pickerGate) _livePickerDelegates.Remove(pickerDelegate);
                };

                host.TopViewController.PresentViewController(picker, animated: true, completionHandler: null);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    // 保活集合，见 PresentPickerAsync 中的说明。
    private readonly Lock _pickerGate = new();
    private readonly List<PickerDelegate> _livePickerDelegates = new();

    /// <summary>取编辑后的图片；未开启编辑或无编辑结果时退回原图。</summary>
    private static UIImage? ResolveImage(NSDictionary info, bool edited)
    {
        if (edited && info.ValueForKey(UIImagePickerController.EditedImage) is UIImage editedImage)
            return editedImage;

        return info.ValueForKey(UIImagePickerController.OriginalImage) as UIImage;
    }

    private static string EncodeThumbnail(UIImage image, EncodingType encoding, int quality)
    {
        using var data = encoding == EncodingType.Png
            ? image.AsPNG()
            : image.AsJPEG(Math.Clamp(quality, 0, 100) / 100f);

        return data?.GetBase64EncodedString(NSDataBase64EncodingOptions.None) ?? string.Empty;
    }

    private static MediaMetadata DescribeImage(UIImage image, EncodingType encoding)
    {
        using var data = encoding == EncodingType.Png ? image.AsPNG() : image.AsJPEG(1f);

        return new MediaMetadata(
            Size: (long)(data?.Length ?? 0),
            Duration: null,
            Format: encoding == EncodingType.Png ? "png" : "jpeg",
            Resolution: $"{(int)image.Size.Width}x{(int)image.Size.Height}",
            CreationDate: DateTimeOffset.UtcNow);
    }

    private static MediaMetadata DescribeVideo(NSUrl url)
    {
        using var asset = AVAsset.FromUrl(url);
        var duration = asset?.Duration.Seconds ?? 0;

        long size = 0;
        if (url.Path is { } path && File.Exists(path)) size = new System.IO.FileInfo(path).Length;

        return new MediaMetadata(
            Size: size,
            Duration: duration,
            Format: "mp4",
            Resolution: string.Empty,
            CreationDate: DateTimeOffset.UtcNow);
    }

    /// <summary>读取「有限照片库」授权下应用可见的照片。</summary>
    private static GalleryPhotos ReadLimitedLibrary()
    {
        var photos = new List<GalleryPhoto>();

        using var options = new PHFetchOptions();
        using var assets = PHAsset.FetchAssets(PHAssetMediaType.Image, options);

        foreach (var asset in assets.OfType<PHAsset>())
        {
            // 只给出标识：真正取图要走 PHImageManager 的异步请求，不宜在此同步阻塞。
            var identifier = asset.LocalIdentifier ?? string.Empty;
            photos.Add(new GalleryPhoto(identifier, $"ph://{identifier}", "jpeg"));
        }

        return new GalleryPhotos(photos);
    }

    /// <summary>把 picker 的选择/取消回调桥接为 <see cref="Task"/>。</summary>
    private sealed class PickerDelegate : UIImagePickerControllerDelegate
    {
        private readonly TaskCompletionSource<NSDictionary> _tcs;

        public PickerDelegate(TaskCompletionSource<NSDictionary> tcs) => _tcs = tcs;

        /// <summary>回调已完成（成功或取消），保活引用可以撤掉了。</summary>
        public event Action? Completed;

        public override void FinishedPickingMedia(UIImagePickerController picker, NSDictionary info)
        {
            picker.DismissViewController(animated: true, completionHandler: () =>
            {
                _tcs.TrySetResult(info);
                Completed?.Invoke();
            });
        }

        public override void Canceled(UIImagePickerController picker)
        {
            picker.DismissViewController(animated: true, completionHandler: () =>
            {
                _tcs.TrySetException(new OperationCanceledException("The user cancelled the picker."));
                Completed?.Invoke();
            });
        }
    }
}
