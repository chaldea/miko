using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Miko.Windowing.Video.MacOS;

/// <summary>
/// macOS 上访问 AVFoundation / CoreVideo 所需的 Objective-C runtime 与 C API 声明。
/// 桌面项目是纯 net10.0（无 macOS 托管绑定），因此只能经 <c>objc_msgSend</c> 直接发消息。
///
/// <para>
/// 关键约束：<c>objc_msgSend</c> 的真实签名由被调方法决定，必须为每种返回类型/参数组合
/// 声明独立的 <c>DllImport</c> 入口（用 <c>EntryPoint</c> 指到同一符号）。
/// 把返回结构体的方法当返回指针的方法调用会破坏栈 —— 因此结构体返回值另用
/// <c>objc_msgSend_stret</c> 家族，见 <see cref="CMTime"/> 相关声明。
/// </para>
///
/// <para><b>未经真机运行验证</b>：本机为 Windows，此实现仅通过编译验证。</para>
/// </summary>
[SupportedOSPlatform("macos")]
internal static class AVFoundationInterop
{
    private const string Objc = "/usr/lib/libobjc.dylib";
    private const string CoreVideo = "/System/Library/Frameworks/CoreVideo.framework/CoreVideo";
    private const string CoreMedia = "/System/Library/Frameworks/CoreMedia.framework/CoreMedia";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    // ---- Objective-C runtime ----------------------------------------------

    [DllImport(Objc, EntryPoint = "objc_getClass")]
    internal static extern IntPtr GetClass([MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport(Objc, EntryPoint = "sel_registerName")]
    internal static extern IntPtr GetSelector([MarshalAs(UnmanagedType.LPStr)] string name);

    /// <summary>发消息，返回指针（对象/句柄）。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr SendPtr(IntPtr receiver, IntPtr selector);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr SendPtr(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr SendPtr(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    /// <summary>发消息，无返回值。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(IntPtr receiver, IntPtr selector);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(IntPtr receiver, IntPtr selector, float arg1);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(IntPtr receiver, IntPtr selector, bool arg1);

    /// <summary>发消息，返回 <c>NSInteger</c>（状态枚举等）。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern nint SendNInt(IntPtr receiver, IntPtr selector);

    /// <summary>发消息，返回 <c>BOOL</c>。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern bool SendBool(IntPtr receiver, IntPtr selector);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
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

    // ---- CoreFoundation / NSString ----------------------------------------

    [DllImport(CoreFoundation, EntryPoint = "CFRelease")]
    internal static extern void CFRelease(IntPtr obj);

    [DllImport(CoreFoundation, EntryPoint = "CFRetain")]
    internal static extern IntPtr CFRetain(IntPtr obj);

    /// <summary>用 UTF-8 C 字符串构造 NSString（用于 URL 等参数）。</summary>
    internal static IntPtr CreateNSString(string value)
    {
        IntPtr nsStringClass = GetClass("NSString");
        IntPtr selector = GetSelector("stringWithUTF8String:");
        IntPtr utf8 = Marshal.StringToHGlobalAnsi(value);
        try
        {
            return SendPtr(nsStringClass, selector, utf8);
        }
        finally
        {
            Marshal.FreeHGlobal(utf8);
        }
    }

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

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetBaseAddressOfPlane")]
    internal static extern IntPtr CVPixelBufferGetBaseAddressOfPlane(IntPtr pixelBuffer, nuint planeIndex);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetBytesPerRowOfPlane")]
    internal static extern nuint CVPixelBufferGetBytesPerRowOfPlane(IntPtr pixelBuffer, nuint planeIndex);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetHeightOfPlane")]
    internal static extern nuint CVPixelBufferGetHeightOfPlane(IntPtr pixelBuffer, nuint planeIndex);

    [DllImport(CoreVideo, EntryPoint = "CVPixelBufferGetWidthOfPlane")]
    internal static extern nuint CVPixelBufferGetWidthOfPlane(IntPtr pixelBuffer, nuint planeIndex);

    /// <summary>
    /// 读取 CoreVideo 导出的 <c>kCVPixelBufferPixelFormatTypeKey</c>（一个 <c>CFStringRef</c> 全局变量）。
    /// 该键的字符串内容不等于变量名，必须从符号读取真实指针，不能用字面量拼字符串。
    /// </summary>
    internal static IntPtr GetPixelFormatTypeKey()
    {
        IntPtr handle = NativeLibrary.Load(CoreVideo);
        if (!NativeLibrary.TryGetExport(handle, "kCVPixelBufferPixelFormatTypeKey", out IntPtr symbol))
            return IntPtr.Zero;

        // 符号本身是一个变量地址，其内容才是 CFStringRef。
        return Marshal.ReadIntPtr(symbol);
    }

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
