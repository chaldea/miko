namespace Miko.Native.Camera;

/// <summary>
/// 拍照、录像、媒体选择与编辑。对应 Capacitor v8 的
/// <see href="https://ionicframework.com/docs/v8/native/camera#api">Camera API</see>。
/// 支持平台：Android、iOS、Windowing。
/// <para>
/// 只暴露 8.1+ 的新 API。原文档中已弃用的 <c>getPhoto</c> / <c>pickImages</c> 按
/// <c>issues/feat-platform.md</c> 平台实现要求第 4 条不做接口定义——新代码使用
/// <see cref="TakePhotoAsync"/> 与 <see cref="ChooseFromGalleryAsync"/>。
/// </para>
/// </summary>
public interface ICameraService
{
    /// <summary>打开相机拍照（8.1+）。</summary>
    Task<MediaResult> TakePhotoAsync(TakePhotoOptions options);

    /// <summary>录像（非 Web，8.1+）。</summary>
    Task<MediaResult> RecordVideoAsync(RecordVideoOptions options);

    /// <summary>打开原生视频播放器（非 Web）。</summary>
    Task PlayVideoAsync(PlayVideoOptions options);

    /// <summary>从相册选择照片、视频或混合媒体（8.1+）。</summary>
    Task<MediaResults> ChooseFromGalleryAsync(ChooseFromGalleryOptions options);

    /// <summary>编辑 Base64 图片（非 Web）。</summary>
    Task<EditPhotoResult> EditPhotoAsync(EditPhotoOptions options);

    /// <summary>编辑 URI 图片（非 Web）。</summary>
    Task<MediaResult> EditUriPhotoAsync(EditUriPhotoOptions options);

    /// <summary>更新 iOS 有限照片库的选择范围。</summary>
    Task<GalleryPhotos> PickLimitedLibraryPhotosAsync();

    /// <summary>获取 iOS 有限照片库的当前内容。</summary>
    Task<GalleryPhotos> GetLimitedLibraryPhotosAsync();

    /// <summary>查询相机/照片权限。</summary>
    Task<CameraPermissionStatus> CheckPermissionsAsync();

    /// <summary>请求相机/照片权限。</summary>
    Task<CameraPermissionStatus> RequestPermissionsAsync(CameraPluginPermissions? permissions = null);
}
