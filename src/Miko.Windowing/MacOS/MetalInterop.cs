using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Miko.Windowing.MacOS;

/// <summary>
/// Metal / QuartzCore / CoreGraphics 的 C 入口与常量。Objective-C 对象上的调用走 <see cref="ObjC"/>。
/// </summary>
[SupportedOSPlatform("macos")]
internal static class MetalInterop
{
    private const string Metal = "/System/Library/Frameworks/Metal.framework/Metal";
    private const string QuartzCore = "/System/Library/Frameworks/QuartzCore.framework/QuartzCore";
    private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    /// <summary>系统默认 GPU。遵循 Create 规则，返回 +1 引用；没有可用 Metal 设备时为 <c>nil</c>。</summary>
    [DllImport(Metal, EntryPoint = "MTLCreateSystemDefaultDevice")]
    internal static extern IntPtr CreateSystemDefaultDevice();

    [DllImport(CoreGraphics, EntryPoint = "CGColorSpaceCreateWithName")]
    internal static extern IntPtr CGColorSpaceCreateWithName(IntPtr name);

    [DllImport(CoreGraphics, EntryPoint = "CGColorSpaceRelease")]
    internal static extern void CGColorSpaceRelease(IntPtr colorSpace);

    /// <summary>
    /// 确保 QuartzCore 已加载，<c>CAMetalLayer</c> 才会注册进 Objective-C runtime。
    /// GLFW 链接的是 Cocoa，并不保证 QuartzCore 的类已可用。
    /// </summary>
    internal static void LoadQuartzCore() => NativeLibrary.Load(QuartzCore);

    /// <summary>sRGB 色彩空间（+1 引用，须 <see cref="CGColorSpaceRelease"/>）。</summary>
    internal static IntPtr CreateSrgbColorSpace()
    {
        IntPtr name = ObjC.ReadExportedConstant(CoreGraphics, "kCGColorSpaceSRGB");
        return name == IntPtr.Zero ? IntPtr.Zero : CGColorSpaceCreateWithName(name);
    }

    /// <summary><c>MTLPixelFormatR8Unorm</c>。</summary>
    internal const nuint PixelFormatR8Unorm = 10;

    /// <summary><c>MTLPixelFormatRG8Unorm</c>。</summary>
    internal const nuint PixelFormatRG8Unorm = 30;

    /// <summary><c>MTLPixelFormatBGRA8Unorm</c>。</summary>
    internal const nuint PixelFormatBgra8Unorm = 80;

    /// <summary><c>MTLCommandBufferStatusCompleted</c>；其后的 <c>Error</c> = 5 同样表示已结束。</summary>
    internal const nuint CommandBufferStatusCompleted = 4;
}
