namespace Miko.Platform.Video;

/// <summary>
/// 视频帧的像素布局。决定 <see cref="VideoFrameBuffer"/> 里各平面的含义与采样方式。
/// </summary>
public enum VideoPixelFormat
{
    /// <summary>单平面 RGBA8888，紧凑排列。软解与 CPU 回退路径的通用格式。</summary>
    Rgba8888,

    /// <summary>单平面 BGRA8888。Media Foundation / AVFoundation 的常见 CPU 输出。</summary>
    Bgra8888,

    /// <summary>
    /// 双平面 NV12：平面 0 为全分辨率 Y（8bpp），平面 1 为半分辨率交错 UV（16bpp）。
    /// 系统硬解器的原生输出格式，需经 YUV→RGB 转换（见 <c>Nv12FrameComposer</c>）。
    /// </summary>
    Nv12,

    /// <summary>
    /// 平台外部纹理，像素格式由平台自身语义决定（Android 的 OES 外部纹理、
    /// macOS 的 TEXTURE_RECTANGLE）。采样即得 RGB，无需自行转换。
    /// </summary>
    ExternalTexture,
}

/// <summary>
/// 一个 GPU 纹理平面的后端句柄。字段直接对应
/// <c>GRGlTextureInfo(target, id, format)</c>，由帧源在渲染线程包装为 Skia 图像。
/// </summary>
/// <param name="Target">GL 纹理目标，如 <c>GL_TEXTURE_2D</c> / <c>GL_TEXTURE_EXTERNAL_OES</c>。</param>
/// <param name="Id">GL 纹理对象 id。</param>
/// <param name="Format">GL 内部格式（<c>GL_R8</c> / <c>GL_RG8</c> / <c>GL_RGBA8</c>）。</param>
/// <param name="Width">该平面的像素宽（NV12 的 UV 平面为亮度平面的一半）。</param>
/// <param name="Height">该平面的像素高。</param>
public readonly record struct VideoTexturePlane(
    uint Target,
    uint Id,
    uint Format,
    int Width,
    int Height);

/// <summary>
/// 解码器产出的一帧，以「GPU 纹理」或「CPU 缓冲」两种形态之一承载 —— 即零拷贝路径与回退路径。
/// <para>
/// 解码线程构造本类型并入队；渲染线程在 <see cref="IVideoFrameSource.AcquireCurrentFrame"/>
/// 中据形态选择包装方式（详见 <c>VideoFrameSourceBase</c>）。这样「后端如何拿到帧」
/// 与「如何把帧变成 <c>SKImage</c>」彻底解耦：五个平台各自只负责填充本类型。
/// </para>
/// </summary>
public sealed class VideoFrameBuffer
{
    /// <summary>帧的显示宽（亮度平面尺寸，非 UV 平面）。</summary>
    public int Width { get; }

    /// <summary>帧的显示高。</summary>
    public int Height { get; }

    /// <summary>像素布局，决定如何解释各平面。</summary>
    public VideoPixelFormat Format { get; }

    /// <summary>此帧的呈现时间戳。</summary>
    public TimeSpan Pts { get; }

    /// <summary>
    /// GPU 纹理平面（零拷贝路径）。非空时渲染线程直接包装这些纹理，CPU 不接触像素。
    /// RGBA/外部纹理为 1 个平面，NV12 为 2 个（Y、UV）。
    /// </summary>
    public IReadOnlyList<VideoTexturePlane>? TexturePlanes { get; }

    /// <summary>
    /// CPU 像素缓冲（回退路径）。与 <see cref="TexturePlanes"/> 互斥；非空时渲染线程上传为纹理。
    /// RGBA/BGRA 为 1 个平面，NV12 为 2 个。
    /// </summary>
    public IReadOnlyList<byte[]>? CpuPlanes { get; }

    /// <summary>
    /// 各 CPU 平面的行字节数（stride）。硬解器输出常带行填充，不等于 <c>width * bpp</c>，
    /// 上传前必须按此值逐行寻址，否则画面会斜切。
    /// </summary>
    public IReadOnlyList<int>? CpuStrides { get; }

    /// <summary>是否为零拷贝帧（持 GPU 纹理句柄）。</summary>
    public bool IsGpuFrame => TexturePlanes is { Count: > 0 };

    /// <summary>
    /// 帧被渲染线程用完后的回收回调。零拷贝路径用它把 GPU 资源交还解码器
    /// （解锁 D3D11 共享句柄 / 释放 CVPixelBuffer / 归还 MediaCodec 输出缓冲）。
    /// 不需要回收时为 <c>null</c>。
    /// </summary>
    public Action? OnReleased { get; }

    private VideoFrameBuffer(
        int width,
        int height,
        VideoPixelFormat format,
        TimeSpan pts,
        IReadOnlyList<VideoTexturePlane>? texturePlanes,
        IReadOnlyList<byte[]>? cpuPlanes,
        IReadOnlyList<int>? cpuStrides,
        Action? onReleased)
    {
        Width = width;
        Height = height;
        Format = format;
        Pts = pts;
        TexturePlanes = texturePlanes;
        CpuPlanes = cpuPlanes;
        CpuStrides = cpuStrides;
        OnReleased = onReleased;
    }

    /// <summary>构造零拷贝帧：像素驻留 GPU，只传纹理句柄。</summary>
    public static VideoFrameBuffer FromTextures(
        int width,
        int height,
        VideoPixelFormat format,
        TimeSpan pts,
        IReadOnlyList<VideoTexturePlane> planes,
        Action? onReleased = null)
    {
        ArgumentNullException.ThrowIfNull(planes);
        if (planes.Count == 0)
            throw new ArgumentException("A GPU frame needs at least one texture plane.", nameof(planes));

        return new VideoFrameBuffer(width, height, format, pts, planes, null, null, onReleased);
    }

    /// <summary>构造 CPU 回退帧：像素在托管缓冲中，渲染线程负责上传。</summary>
    public static VideoFrameBuffer FromCpuPlanes(
        int width,
        int height,
        VideoPixelFormat format,
        TimeSpan pts,
        IReadOnlyList<byte[]> planes,
        IReadOnlyList<int> strides,
        Action? onReleased = null)
    {
        ArgumentNullException.ThrowIfNull(planes);
        ArgumentNullException.ThrowIfNull(strides);
        if (planes.Count == 0)
            throw new ArgumentException("A CPU frame needs at least one pixel plane.", nameof(planes));
        if (strides.Count != planes.Count)
            throw new ArgumentException("Each CPU plane needs a matching stride.", nameof(strides));

        return new VideoFrameBuffer(width, height, format, pts, null, planes, strides, onReleased);
    }
}
