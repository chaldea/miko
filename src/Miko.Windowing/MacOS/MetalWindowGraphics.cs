using System.Runtime.Versioning;
using Miko.Rendering;
using Miko.Windowing.Common;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SkiaSharp;
using static Miko.Windowing.MacOS.ObjC;

namespace Miko.Windowing.MacOS;

/// <summary>
/// macOS 的 Metal 呈现后端：把一个 <c>CAMetalLayer</c> 挂到 GLFW 窗口（以 <see cref="GraphicsAPI.None"/>
/// 创建，没有 GL 上下文）的内容视图上，每帧把可绘制纹理包装成 Skia 表面。
/// 设备、命令队列与 <see cref="GRContext"/> 都由本类持有，做法与 iOS 的 <c>MikoMetalView</c> 一致。
///
/// <para><b>Retina</b>：窗口坐标是点（point），可绘制纹理按 <c>backingScaleFactor</c> 分配物理像素，
/// 宿主按 <see cref="Scale"/> 缩放画布。尺寸与缩放在主线程随窗口事件更新（AppKit 的约定），
/// 渲染线程只读取纹理的实际尺寸，两边不必加锁。</para>
///
/// <para><b>帧节奏</b>：图层保持默认的垂直同步与三块可绘制纹理，GPU 落后时 <c>nextDrawable</c>
/// 会在渲染线程上等待，起到背压作用。</para>
/// </summary>
[SupportedOSPlatform("macos")]
internal sealed class MetalWindowGraphics : SilkWindowGraphics
{
    // 每帧都要发的消息，选择子只注册一次。
    private static readonly IntPtr SelNextDrawable = GetSelector("nextDrawable");
    private static readonly IntPtr SelTexture = GetSelector("texture");
    private static readonly IntPtr SelWidth = GetSelector("width");
    private static readonly IntPtr SelHeight = GetSelector("height");
    private static readonly IntPtr SelCommandBuffer = GetSelector("commandBuffer");
    private static readonly IntPtr SelPresentDrawable = GetSelector("presentDrawable:");
    private static readonly IntPtr SelCommit = GetSelector("commit");
    private static readonly IntPtr SelStatus = GetSelector("status");

    private readonly IWindow _window;
    private readonly IntPtr _nsWindow;
    private readonly string _deviceName;
    private IntPtr _device;
    private IntPtr _queue;
    private IntPtr _layer;
    private volatile float _scale = 1f;

    // 呈现命令缓冲尚未执行完的帧及其须活到那时的资源（见 MetalFrameResources）。仅渲染线程访问。
    private readonly Queue<(IntPtr CommandBuffer, List<IDisposable> Resources)> _inFlight = new();

    // 当前帧（BeginFrame 到 Present 之间）。
    private IntPtr _pool;
    private IntPtr _drawable;
    private GRBackendRenderTarget? _target;
    private SKSurface? _surface;

    /// <summary>本机是否有可用的 Metal 设备。</summary>
    internal static bool IsSupported
    {
        get
        {
            IntPtr device = MetalInterop.CreateSystemDefaultDevice();
            if (device == IntPtr.Zero) return false;
            CFRelease(device);
            return true;
        }
    }

    /// <summary>主线程：创建设备与命令队列，并把 <c>CAMetalLayer</c> 挂到窗口的内容视图上。</summary>
    public MetalWindowGraphics(IWindow window)
    {
        _window = window;
        _nsWindow = window.Native?.Cocoa
            ?? throw new PlatformNotSupportedException("The window does not expose a Cocoa NSWindow.");

        _device = MetalInterop.CreateSystemDefaultDevice();
        if (_device == IntPtr.Zero)
            throw new PlatformNotSupportedException("Metal is not available on this Mac.");
        _deviceName = ReadNSString(SendPtr(_device, GetSelector("name"))) ?? "unknown";

        _queue = SendPtr(_device, GetSelector("newCommandQueue"));
        if (_queue == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create a Metal command queue.");

        MetalInterop.LoadQuartzCore();
        _layer = CFRetain(SendPtr(GetClass("CAMetalLayer"), GetSelector("layer")));
        SendVoid(_layer, GetSelector("setDevice:"), _device);
        SendVoid(_layer, GetSelector("setPixelFormat:"), MetalInterop.PixelFormatBgra8Unorm);
        // 让 Skia 在需要读回目标的混合模式下仍可采样它；MIKO_DUMP_FRAME 的帧转储也要读回。
        SendVoid(_layer, GetSelector("setFramebufferOnly:"), false);
        // 每帧都整屏清除，不需要与窗口背景混合。
        SendVoid(_layer, GetSelector("setOpaque:"), true);

        // Skia 按 sRGB 出图（CSS 颜色即 sRGB）。不标注色彩空间时 macOS 不做颜色匹配，
        // 在 P3 广色域屏上会过饱和；iOS 对未标注内容本就按 sRGB 解释，这里对齐它。
        IntPtr srgb = MetalInterop.CreateSrgbColorSpace();
        if (srgb != IntPtr.Zero)
        {
            SendVoid(_layer, GetSelector("setColorspace:"), srgb);
            MetalInterop.CGColorSpaceRelease(srgb);
        }

        // 与 GLFW 为 Vulkan/MoltenVK 挂图层的做法相同：先设图层再开 wantsLayer，视图成为该图层的宿主。
        IntPtr contentView = SendPtr(_nsWindow, GetSelector("contentView"));
        SendVoid(contentView, GetSelector("setLayer:"), _layer);
        SendVoid(contentView, GetSelector("setWantsLayer:"), true);

        UpdateDrawableSize(window.Size);
        // 在 Retina 与普通屏之间拖动窗口时尺寸不变、帧缓冲尺寸变，两个事件都要跟。
        window.Resize += UpdateDrawableSize;
        window.FramebufferResize += OnFramebufferResize;
    }

    public override string Description => $"Metal ({_deviceName})";

    public override float Scale => _scale;

    public override void Initialize()
    {
        using var backend = new GRMtlBackendContext { DeviceHandle = _device, QueueHandle = _queue };
        Context = GRContext.CreateMetal(backend)
            ?? throw new InvalidOperationException("Failed to create a Skia Metal context.");
        GpuResourceCache.Configure(Context);
    }

    public override SKSurface? BeginFrame(int width, int height)
    {
        ReleaseCompletedFrames();
        if (width <= 0 || height <= 0) return null;

        _pool = AutoreleasePoolPush();

        // 可绘制纹理全部在 GPU 上排队时，这里会等待其中一块空出来（最多约 1 秒，超时返回 nil）。
        _drawable = SendPtr(_layer, SelNextDrawable);
        IntPtr texture = _drawable == IntPtr.Zero ? IntPtr.Zero : SendPtr(_drawable, SelTexture);
        if (texture != IntPtr.Zero)
        {
            int pixelWidth = (int)SendNUInt(texture, SelWidth);
            int pixelHeight = (int)SendNUInt(texture, SelHeight);

            // 图层每帧轮换可绘制纹理，包装对象随帧新建、随帧释放。
            _target = new GRBackendRenderTarget(pixelWidth, pixelHeight, new GRMtlTextureInfo(texture));
            _surface = SKSurface.Create(Context, _target, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888);
            if (_surface != null)
                return _surface;
        }

        EndFrame();
        return null;
    }

    public override void Present()
    {
        if (_surface == null) return;

        // 先提交 Skia 本帧的命令，再在同一队列上用一个命令缓冲呈现。
        Context.Flush();
        IntPtr commandBuffer = SendPtr(_queue, SelCommandBuffer);
        if (commandBuffer != IntPtr.Zero)
        {
            // 本帧登记的视频纹理等到呈现缓冲执行完（即 Skia 本帧命令已执行完）再释放。
            var retained = MetalFrameResources.TakePending();
            if (retained != null)
                _inFlight.Enqueue((CFRetain(commandBuffer), retained));

            SendVoid(commandBuffer, SelPresentDrawable, _drawable);
            SendVoid(commandBuffer, SelCommit);
        }

        EndFrame();
    }

    public override void Dispose()
    {
        _window.Resize -= UpdateDrawableSize;
        _window.FramebufferResize -= OnFramebufferResize;

        EndFrame();

        // 等队列上已提交的命令全部执行完，再释放它们引用的视频纹理与上下文。
        if (_queue != IntPtr.Zero)
        {
            IntPtr pool = AutoreleasePoolPush();
            IntPtr fence = SendPtr(_queue, SelCommandBuffer);
            if (fence != IntPtr.Zero)
            {
                SendVoid(fence, SelCommit);
                SendVoid(fence, GetSelector("waitUntilCompleted"));
            }
            AutoreleasePoolPop(pool);
        }

        while (_inFlight.TryDequeue(out var frame))
            ReleaseFrame(frame.CommandBuffer, frame.Resources);
        if (MetalFrameResources.TakePending() is { } orphaned)
            foreach (var resource in orphaned) resource.Dispose();

        DisposeContext();

        ReleaseHandle(ref _layer);
        ReleaseHandle(ref _queue);
        ReleaseHandle(ref _device);
    }

    /// <summary>释放本帧的包装对象并弹出本帧的自动释放池（可绘制对象随之释放）。</summary>
    private void EndFrame()
    {
        _surface?.Dispose();
        _surface = null;
        _target?.Dispose();
        _target = null;
        _drawable = IntPtr.Zero;

        if (_pool != IntPtr.Zero)
        {
            AutoreleasePoolPop(_pool);
            _pool = IntPtr.Zero;
        }
    }

    /// <summary>释放呈现缓冲已执行完的帧所保留的资源。缓冲按提交顺序完成，遇到未完成的即可停止。</summary>
    private void ReleaseCompletedFrames()
    {
        while (_inFlight.TryPeek(out var frame)
               && SendNUInt(frame.CommandBuffer, SelStatus) >= MetalInterop.CommandBufferStatusCompleted)
        {
            _inFlight.Dequeue();
            ReleaseFrame(frame.CommandBuffer, frame.Resources);
        }
    }

    private static void ReleaseFrame(IntPtr commandBuffer, List<IDisposable> resources)
    {
        foreach (var resource in resources)
            resource.Dispose();
        CFRelease(commandBuffer);
    }

    private static void ReleaseHandle(ref IntPtr handle)
    {
        if (handle == IntPtr.Zero) return;
        CFRelease(handle);
        handle = IntPtr.Zero;
    }

    private void OnFramebufferResize(Vector2D<int> _) => UpdateDrawableSize(_window.Size);

    /// <summary>主线程：按窗口尺寸（点）与当前 backing scale 设置可绘制纹理的像素尺寸。</summary>
    private void UpdateDrawableSize(Vector2D<int> size)
    {
        if (_layer == IntPtr.Zero) return;

        double scale = SendDouble(_nsWindow, GetSelector("backingScaleFactor"));
        if (scale <= 0) scale = 1;
        SendVoid(_layer, GetSelector("setContentsScale:"), scale);
        _scale = (float)scale;

        // 最小化等 0 尺寸：保留上一次的可绘制尺寸，宿主会跳过这些帧。
        if (size.X <= 0 || size.Y <= 0) return;
        SendVoid(_layer, GetSelector("setDrawableSize:"),
            new CGSize(Math.Round(size.X * scale), Math.Round(size.Y * scale)));
    }
}
