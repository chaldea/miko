using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Miko.Windowing.MacOS;

/// <summary>
/// macOS 上 Objective-C runtime 与 CoreFoundation 的最小声明，由 Metal 窗口后端与 AVFoundation
/// 视频后端共用。桌面项目是纯 net10.0（无 macOS 托管绑定），只能经 <c>objc_msgSend</c> 直接发消息。
///
/// <para>
/// 关键约束：<c>objc_msgSend</c> 的真实签名由被调方法决定，必须为每种返回类型/参数组合
/// 声明独立的 <c>DllImport</c> 入口（用 <c>EntryPoint</c> 指到同一符号）。
/// Objective-C 的 <c>BOOL</c> 只有 1 字节，按 <c>I1</c> 封送：默认的 4 字节 Win32 BOOL
/// 会把返回寄存器里未定义的高位也读进来。
/// </para>
/// </summary>
[SupportedOSPlatform("macos")]
internal static class ObjC
{
    private const string Objc = "/usr/lib/libobjc.dylib";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

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
    internal static extern void SendVoid(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(IntPtr receiver, IntPtr selector, float arg1);

    /// <summary>参数为 <c>CGFloat</c>（64 位平台上即 double）。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(IntPtr receiver, IntPtr selector, double arg1);

    /// <summary>参数为 <c>NSUInteger</c>（枚举、像素格式等）。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(IntPtr receiver, IntPtr selector, nuint arg1);

    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool arg1);

    /// <summary>参数为 <c>CGSize</c>（两个 double，按平台 ABI 经浮点寄存器传递）。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(IntPtr receiver, IntPtr selector, CGSize arg1);

    /// <summary>发消息，返回 <c>NSInteger</c>（状态枚举等）。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern nint SendNInt(IntPtr receiver, IntPtr selector);

    /// <summary>发消息，返回 <c>NSUInteger</c>。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern nuint SendNUInt(IntPtr receiver, IntPtr selector);

    /// <summary>发消息，返回 <c>CGFloat</c>。arm64 与 x86-64 的 double 返回值都走普通 objc_msgSend。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    internal static extern double SendDouble(IntPtr receiver, IntPtr selector);

    /// <summary>发消息，返回 <c>BOOL</c>。</summary>
    [DllImport(Objc, EntryPoint = "objc_msgSend")]
    [return: MarshalAs(UnmanagedType.I1)]
    internal static extern bool SendBool(IntPtr receiver, IntPtr selector);

    /// <summary>
    /// 压入一个自动释放池。非主线程没有 AppKit 维护的池，autorelease 对象（如 <c>nextDrawable</c>、
    /// <c>commandBuffer</c> 的返回值）会一直堆积到线程退出，渲染线程必须每帧自行压入/弹出。
    /// </summary>
    [DllImport(Objc, EntryPoint = "objc_autoreleasePoolPush")]
    internal static extern IntPtr AutoreleasePoolPush();

    [DllImport(Objc, EntryPoint = "objc_autoreleasePoolPop")]
    internal static extern void AutoreleasePoolPop(IntPtr pool);

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

    /// <summary>把 NSString 读成托管字符串；<c>nil</c> 返回 <c>null</c>。</summary>
    internal static string? ReadNSString(IntPtr nsString)
    {
        if (nsString == IntPtr.Zero) return null;
        return Marshal.PtrToStringUTF8(SendPtr(nsString, GetSelector("UTF8String")));
    }

    /// <summary>
    /// 读取框架导出的 <c>CFStringRef</c> 常量（如 <c>kCVPixelBufferPixelFormatTypeKey</c>）。
    /// 这类键的字符串内容不等于变量名，必须从符号读取真实指针，不能用字面量拼字符串。
    /// 拿不到符号时返回 <see cref="IntPtr.Zero"/>。
    /// </summary>
    internal static IntPtr ReadExportedConstant(string framework, string symbolName)
    {
        IntPtr handle = NativeLibrary.Load(framework);
        if (!NativeLibrary.TryGetExport(handle, symbolName, out IntPtr symbol))
            return IntPtr.Zero;

        // 符号本身是一个变量地址，其内容才是 CFStringRef。
        return Marshal.ReadIntPtr(symbol);
    }

    /// <summary><c>CGSize</c> 的内存布局（64 位平台上为两个 double）。</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct CGSize(double width, double height)
    {
        public readonly double Width = width;
        public readonly double Height = height;
    }
}
