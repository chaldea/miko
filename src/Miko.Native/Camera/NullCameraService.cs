namespace Miko.Native.Camera;

/// <summary>未注册平台实现时的 <see cref="ICameraService"/> 占位实现，调用即抛。</summary>
public sealed class NullCameraService : NativeServiceBase, ICameraService
{
    public Task<MediaResult> TakePhotoAsync(TakePhotoOptions options) => UnsupportedAsync<MediaResult>();
    public Task<MediaResult> RecordVideoAsync(RecordVideoOptions options) => UnsupportedAsync<MediaResult>();
    public Task PlayVideoAsync(PlayVideoOptions options) => UnsupportedAsync();
    public Task<MediaResults> ChooseFromGalleryAsync(ChooseFromGalleryOptions options) => UnsupportedAsync<MediaResults>();
    public Task<EditPhotoResult> EditPhotoAsync(EditPhotoOptions options) => UnsupportedAsync<EditPhotoResult>();
    public Task<MediaResult> EditUriPhotoAsync(EditUriPhotoOptions options) => UnsupportedAsync<MediaResult>();
    public Task<GalleryPhotos> PickLimitedLibraryPhotosAsync() => UnsupportedAsync<GalleryPhotos>();
    public Task<GalleryPhotos> GetLimitedLibraryPhotosAsync() => UnsupportedAsync<GalleryPhotos>();
    public Task<CameraPermissionStatus> CheckPermissionsAsync() => UnsupportedAsync<CameraPermissionStatus>();
    public Task<CameraPermissionStatus> RequestPermissionsAsync(CameraPluginPermissions? permissions = null)
        => UnsupportedAsync<CameraPermissionStatus>();
}
