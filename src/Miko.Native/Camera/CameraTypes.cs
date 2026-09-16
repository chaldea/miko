namespace Miko.Native.Camera;

/// <summary>图片编码格式。</summary>
public enum EncodingType
{
    /// <summary>JPEG（默认，有损但体积小）。</summary>
    Jpeg,

    /// <summary>PNG（无损）。</summary>
    Png,
}

/// <summary>使用的摄像头。</summary>
public enum CameraDirection
{
    /// <summary>后置摄像头（默认）。</summary>
    Rear,

    /// <summary>前置摄像头。</summary>
    Front,
}

/// <summary>拍照/选图后的编辑方式。</summary>
public enum PhotoEditMode
{
    /// <summary>在应用内编辑。</summary>
    InApp,

    /// <summary>调用系统编辑器。</summary>
    External,

    /// <summary>不编辑（默认）。</summary>
    No,
}

/// <summary>相机界面的呈现方式（iOS）。</summary>
public enum CameraPresentationStyle
{
    /// <summary>全屏（默认）。</summary>
    Fullscreen,

    /// <summary>弹出卡片。</summary>
    Popover,
}

/// <summary>从相册选择的媒体种类。</summary>
public enum MediaTypeSelection
{
    /// <summary>仅照片。</summary>
    Photo,

    /// <summary>仅视频。</summary>
    Video,

    /// <summary>照片与视频。</summary>
    All,
}

/// <summary>一条媒体结果的实际类型。</summary>
public enum MediaType
{
    /// <summary>照片。</summary>
    Photo,

    /// <summary>视频。</summary>
    Video,
}

/// <summary>拍照选项（8.1+）。</summary>
public sealed record TakePhotoOptions
{
    /// <summary>压缩质量，0～100，默认 100。</summary>
    public int Quality { get; init; } = 100;

    /// <summary>目标宽度（像素），设置后按比例缩放。</summary>
    public int? TargetWidth { get; init; }

    /// <summary>目标高度（像素），设置后按比例缩放。</summary>
    public int? TargetHeight { get; init; }

    /// <summary>是否按 EXIF 方向自动校正，默认 <c>true</c>。</summary>
    public bool CorrectOrientation { get; init; } = true;

    /// <summary>编码格式。</summary>
    public EncodingType EncodingType { get; init; } = EncodingType.Jpeg;

    /// <summary>是否保存到系统相册。</summary>
    public bool SaveToGallery { get; init; }

    /// <summary>使用的摄像头。</summary>
    public CameraDirection CameraDirection { get; init; } = CameraDirection.Rear;

    /// <summary>拍照后的编辑方式。</summary>
    public PhotoEditMode Editable { get; init; } = PhotoEditMode.No;

    /// <summary>相机界面呈现方式（iOS）。</summary>
    public CameraPresentationStyle PresentationStyle { get; init; } = CameraPresentationStyle.Fullscreen;

    /// <summary>Web 上是否使用 <c>&lt;input type="file"&gt;</c> 而非 getUserMedia。</summary>
    public bool WebUseInput { get; init; }

    /// <summary>是否在结果中包含元数据。</summary>
    public bool IncludeMetadata { get; init; }
}

/// <summary>录像选项（8.1+，非 Web）。</summary>
public sealed record RecordVideoOptions
{
    /// <summary>是否保存到系统相册。</summary>
    public bool SaveToGallery { get; init; }

    /// <summary>是否在结果中包含元数据。</summary>
    public bool IncludeMetadata { get; init; }

    /// <summary>结果文件是否持久保存（而非临时目录），默认 <c>true</c>。</summary>
    public bool IsPersistent { get; init; } = true;
}

/// <summary>从相册选择媒体的选项（8.1+）。</summary>
public sealed record ChooseFromGalleryOptions
{
    /// <summary>可选择的媒体种类。</summary>
    public MediaTypeSelection MediaType { get; init; } = MediaTypeSelection.Photo;

    /// <summary>是否允许多选。</summary>
    public bool AllowMultipleSelection { get; init; }

    /// <summary>多选时的数量上限。</summary>
    public int? Limit { get; init; }

    /// <summary>是否在结果中包含元数据。</summary>
    public bool IncludeMetadata { get; init; }

    /// <summary>选择后的编辑方式。</summary>
    public PhotoEditMode Editable { get; init; } = PhotoEditMode.No;

    /// <summary>选择界面呈现方式（iOS）。</summary>
    public CameraPresentationStyle PresentationStyle { get; init; } = CameraPresentationStyle.Fullscreen;

    /// <summary>压缩质量，0～100。</summary>
    public int Quality { get; init; } = 100;

    /// <summary>目标宽度（像素）。</summary>
    public int? TargetWidth { get; init; }

    /// <summary>目标高度（像素）。</summary>
    public int? TargetHeight { get; init; }

    /// <summary>是否按 EXIF 方向自动校正。</summary>
    public bool CorrectOrientation { get; init; } = true;

    /// <summary>Web 上是否使用 <c>&lt;input type="file"&gt;</c>。</summary>
    public bool WebUseInput { get; init; }
}

/// <summary>媒体文件的元数据。</summary>
/// <param name="Size">文件大小（字节）。</param>
/// <param name="Duration">时长（秒），仅视频。</param>
/// <param name="Format">格式/容器，如 <c>jpeg</c>、<c>mp4</c>。</param>
/// <param name="Resolution">分辨率，如 <c>1920x1080</c>。</param>
/// <param name="CreationDate">创建时间。</param>
/// <param name="Exif">EXIF 数据的序列化表示。</param>
public sealed record MediaMetadata(
    long Size,
    double? Duration,
    string Format,
    string Resolution,
    DateTimeOffset? CreationDate = null,
    string? Exif = null);

/// <summary>一条媒体结果（拍照、录像、相册选择、URI 编辑共用）。</summary>
/// <param name="Type">媒体类型。</param>
/// <param name="Uri">原生文件 URI。</param>
/// <param name="Thumbnail">缩略图，Base64 或 Data URL。</param>
/// <param name="Saved">是否已保存到系统相册。</param>
/// <param name="WebPath">可直接用于 <c>&lt;img&gt;</c>/<c>&lt;video&gt;</c> 的路径。</param>
/// <param name="Metadata">元数据，请求时才有值。</param>
public sealed record MediaResult(
    MediaType Type,
    string? Uri,
    string Thumbnail,
    bool Saved,
    string? WebPath = null,
    MediaMetadata? Metadata = null);

/// <summary>多条媒体结果。</summary>
/// <param name="Results">结果列表。</param>
public sealed record MediaResults(IReadOnlyList<MediaResult> Results);

/// <summary>编辑 Base64 图片的选项（非 Web）。</summary>
public sealed record EditPhotoOptions
{
    /// <summary>待编辑的图片，Base64 字符串。</summary>
    public required string InputImage { get; init; }
}

/// <summary>Base64 图片编辑结果。</summary>
/// <param name="OutputImage">编辑后的图片，Base64 字符串。</param>
public sealed record EditPhotoResult(string OutputImage);

/// <summary>编辑 URI 图片的选项（非 Web）。</summary>
public sealed record EditUriPhotoOptions
{
    /// <summary>待编辑图片的 URI。</summary>
    public required string Uri { get; init; }

    /// <summary>编辑结果是否保存到系统相册。</summary>
    public bool SaveToGallery { get; init; }

    /// <summary>是否在结果中包含元数据。</summary>
    public bool IncludeMetadata { get; init; }
}

/// <summary>播放视频的选项（非 Web）。</summary>
public sealed record PlayVideoOptions
{
    /// <summary>要播放的视频 URI。</summary>
    public required string Uri { get; init; }
}

/// <summary>有限照片库中的一张照片。</summary>
/// <param name="Path">原生文件路径。</param>
/// <param name="WebPath">可直接用于 <c>&lt;img&gt;</c> 的路径。</param>
/// <param name="Format">图片格式。</param>
/// <param name="Base64String">Base64 内容，请求时才有值。</param>
/// <param name="Exif">EXIF 数据。</param>
public sealed record GalleryPhoto(
    string Path,
    string WebPath,
    string Format,
    string? Base64String = null,
    object? Exif = null);

/// <summary>有限照片库内容。</summary>
/// <param name="Photos">照片列表。</param>
public sealed record GalleryPhotos(IReadOnlyList<GalleryPhoto> Photos);

/// <summary>相机与照片权限状态。</summary>
/// <param name="Camera">相机权限。</param>
/// <param name="Photos">照片库权限。</param>
public sealed record CameraPermissionStatus(PermissionState Camera, PermissionState Photos);

/// <summary>要请求的相机权限种类。</summary>
public sealed record CameraPluginPermissions
{
    /// <summary>要请求的权限名称集合，如 <c>camera</c>、<c>photos</c>。为空时请求全部。</summary>
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}
