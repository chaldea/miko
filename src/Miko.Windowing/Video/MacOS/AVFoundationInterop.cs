using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Miko.Windowing.MacOS;

namespace Miko.Windowing.Video.MacOS;

/// <summary>
/// macOS 上访问 AVFoundation / CoreVideo 所需的 C API 与带 <see cref="CMTime"/> 的消息声明。
/// 通用的 Objective-C runtime 声明在 <see cref="ObjC"/>，与 Metal 窗口后端共用。
///
/// <para>
/// 关键约束：<c>objc_msgSend</c> 的真实签名由被调方法决定，必须为每种返回类型/参数组合
/// 声明独立的 <c>DllImport</c> 入口（用 <c>EntryPoint</c> 指到同一符号）。
/// 把返回结构体的方法当返回指针的方法调用会破坏栈 —— 见 <see cref="CMTime"/> 相关声明。
/// </para>
/// </summary>
[SupportedOSPlatform("macos")]
internal static class AVFoundationInterop
{
    private const string Objc = "/usr/lib/libobjc.dylib";
    private const string CoreVideo = "/System/Library/Frameworks/CoreVideo.framework/CoreVideo";

    // ---- 带 CMTime 的 Objective-C 消息 -------------------------------------

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool SendBool(IntPtr receiver, IntPtr selector, CMTime arg1);

    /// <summary>
    /// 返回 <see cref="CMTime"/>（16 字节结构体）的消息。
    /// Apple Silicon (arm64) 与 x86-64 对该尺寸结构体均走寄存器返回，可直接用 objc_msgSend；
    /// 无需 _stret 变体（_stret 在 arm64 上已不存在）。
    /// </summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern CMTime SendCMTime(IntPtr receiver, IntPtr selector);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr SendPtrWithCMTime(IntPtr receiver, IntPtr selector, CMTime arg1, IntPtr arg2);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoidWithCMTime(IntPtr receiver, IntPtr selector, CMTime arg1);

    // ---- CoreVideo：CVPixelBuffer ------------------------------------------

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferLockBaseAddress")]
    internal static extern int CVPixelBufferLockBaseAddress(IntPtr pixelBuffer, ulong lockFlags);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferUnlockBaseAddress")]
    internal static extern int CVPixelBufferUnlockBaseAddress(IntPtr pixelBuffer, ulong lockFlags);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetWidth")]
    internal static extern nuint CVPixelBufferGetWidth(IntPtr pixelBuffer);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetHeight")]
    internal static extern nuint CVPixelBufferGetHeight(IntPtr pixelBuffer);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetPlaneCount")]
    internal static extern nuint CVPixelBufferGetPlaneCount(IntPtr pixelBuffer);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetBaseAddress")]
    internal static extern IntPtr CVPixelBufferGetBaseAddress(IntPtr pixelBuffer);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetBytesPerRow")]
    internal static extern nuint CVPixelBufferGetBytesPerRow(IntPtr pixelBuffer);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetBaseAddressOfPlane")]
    internal static extern IntPtr CVPixelBufferGetBaseAddressOfPlane(IntPtr pixelBuffer, nuint planeIndex);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetBytesPerRowOfPlane")]
    internal static extern nuint CVPixelBufferGetBytesPerRowOfPlane(IntPtr pixelBuffer, nuint planeIndex);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetHeightOfPlane")]
    internal static extern nuint CVPixelBufferGetHeightOfPlane(IntPtr pixelBuffer, nuint planeIndex);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetWidthOfPlane")]
    internal static extern nuint CVPixelBufferGetWidthOfPlane(IntPtr pixelBuffer, nuint planeIndex);

    /// <summary>读取 CoreVideo 导出的 <c>kCVPixelBufferPixelFormatTypeKey</c>。</summary>
    internal static IntPtr GetPixelFormatTypeKey()
        => ObjC.ReadExportedConstant(CoreVideo, "kCVPixelBufferPixelFormatTypeKey");

    /// <summary>
    /// 读取 <c>kCVPixelBufferMetalCompatibilityKey</c>。请求 Metal 兼容的像素缓冲（IOSurface 支撑），
    /// 零拷贝映射才能成功。
    /// </summary>
    internal static IntPtr GetMetalCompatibilityKey()
        => ObjC.ReadExportedConstant(CoreVideo, "kCVPixelBufferMetalCompatibilityKey");

    // ---- CoreVideo：CVMetalTextureCache（零拷贝） ---------------------------

    [DllImport(CoreVideo, EntryPoint = "CVMetalTextureCacheCreate")]
    internal static extern int CVMetalTextureCacheCreate(
        IntPtr allocator, IntPtr cacheAttributes, IntPtr metalDevice, IntPtr textureAttributes, out IntPtr cacheOut);

    /// <summary>
    /// 把像素缓冲的一个平面映射为 Metal 纹理（IOSurface 直接包装，不拷贝像素）。
    /// 输出的 <c>CVMetalTexture</c> 为 +1 引用，须 <c>CFRelease</c>。
    /// </summary>
    [DllImport(CoreVideo, EntryPoint = "CVMetalTextureCacheCreateTextureFromImage")]
    internal static extern int CVMetalTextureCacheCreateTextureFromImage(
        IntPtr allocator,
        IntPtr textureCache,
        IntPtr sourceImage,
        IntPtr textureAttributes,
        nuint pixelFormat,
        nuint width,
        nuint height,
        nuint planeIndex,
        out IntPtr textureOut);

    /// <summary>取出 <c>CVMetalTexture</c> 包装的 <c>id&lt;MTLTexture&gt;</c>（不转移所有权）。</summary>
    [DllImport(CoreVideo, EntryPoint = "CVMetalTextureGetTexture")]
    internal static extern IntPtr CVMetalTextureGetTexture(IntPtr texture);

    [DllImport(CoreVideo, EntryPoint = "CVMetalTextureCacheFlush")]
    internal static extern void CVMetalTextureCacheFlush(IntPtr textureCache, ulong options);

    /// <summary><c>kCVReturnSuccess</c>。</summary>
    internal const int CVReturnSuccess = 0;

    // ---- CoreMedia：CMTime -------------------------------------------------

    /// <summary>
    /// <c>CMTime</c> 的内存布局（16 字节）：value/timescale/flags/epoch。
    /// AVFoundation 全部时间参数都用它。
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct CMTime
    {
        public long Value;
        public int Timescale;
        public uint Flags;
        public long Epoch;

        internal const uint FlagValid = 1;

        /// <summary>按秒构造，时基取 600（可整除常见帧率 24/25/30/60）。</summary>
        internal static CMTime FromSeconds(double seconds) => new()
        {
            Value = (long)(seconds * 600),
            Timescale = 600,
            Flags = FlagValid,
            Epoch = 0,
        };

        internal double Seconds => Timescale == 0 || (Flags & FlagValid) == 0
            ? 0
            : Value / (double)Timescale;

        internal bool IsValid => (Flags & FlagValid) != 0 && Timescale != 0;
    }

    // ---- 像素格式常量 ------------------------------------------------------

    /// <summary>
    /// <c>kCVPixelFormatType_420YpCbCr8BiPlanarVideoRange</c> = '420v'。
    /// 即窄带 NV12，VideoToolbox 硬解的原生输出格式。
    /// </summary>
    internal const uint PixelFormatNv12VideoRange = 0x34323076;

    /// <summary><c>kCVPixelFormatType_32BGRA</c> = 'BGRA'。CPU 回退格式。</summary>
    internal const uint PixelFormat32Bgra = 0x42475241;

    /// <summary><c>AVPlayerItemStatus</c> / <c>AVPlayerStatus</c>：1 = ReadyToPlay。</summary>
    internal const nint StatusReadyToPlay = 1;

    /// <summary><c>AVPlayerItemStatus</c>：2 = Failed。</summary>
    internal const nint StatusFailed = 2;
}
