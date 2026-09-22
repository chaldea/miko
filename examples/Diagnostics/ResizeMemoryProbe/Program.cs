using System.Diagnostics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace ResizeMemoryProbe;

/// <summary>
/// ISSUE-143 的<b>基线</b>探针：只有 Silk.NET 窗口 + Skia，<b>完全不含 Miko 引擎</b>
/// （没有 DOM、没有样式、没有布局、没有 Painter）。每帧做的事就是宿主必做的那三件：
/// 包一个 <see cref="GRBackendRenderTarget"/>、由它建一张 <see cref="SKSurface"/>、清屏后交换缓冲。
///
/// <para><b>为什么必须有它</b>：真实应用缩放时私有内存从 200 MB 涨到 290 MB。单看这个数字
/// 无法区分两种完全不同的结论——「Miko 的引擎/绘制路径在缩放时泄漏或重复创建资源」，还是
/// 「GL 驱动 + Skia 在帧缓冲尺寸变化时本身就要重新分配一批内部资源」。把引擎整个拿掉之后
/// 如果增长依旧存在，那它就与 Miko 的代码无关，任何在引擎侧的"优化"都只是在动无关的代码。</para>
///
/// <para>用法：<c>--fixed</c> 保持尺寸不变（对照组），默认自行驱动尺寸变化（实验组）。
/// 结果按 CSV 追加到 <c>MIKO_RESIZE_LOG</c> 指向的文件。</para>
/// </summary>
internal static class Program
{
    // 尺寸扫描范围与步长，与 artifacts/issue-143 的 PowerShell 扫描一致，便于交叉比对。
    private const int MinWidth = 700;
    private const int MaxWidth = 1400;
    private const int WidthStep = 20;

    [STAThread]
    public static void Main(string[] args)
    {
        bool fixedSize = args.Contains("--fixed");
        // --cache：只在尺寸变化时重建 target/surface，尺寸不变的帧复用上一张。
        bool cacheSurface = args.Contains("--cache");
        string? logPath = Environment.GetEnvironmentVariable("MIKO_RESIZE_LOG");
        int frameBudget = int.TryParse(Environment.GetEnvironmentVariable("MIKO_PROBE_FRAMES"), out var f) ? f : 3000;

        var options = WindowOptions.Default with
        {
            Title = fixedSize ? "Resize probe (fixed)" : "Resize probe (sweeping)",
            Size = new Vector2D<int>(MinWidth, 500),
            API = GraphicsAPI.Default,
            ShouldSwapAutomatically = false,
        };

        using var window = Window.Create(options);
        GL? gl = null;
        GRContext? grContext = null;
        int width = MinWidth, height = 500;
        int direction = WidthStep;
        int frame = 0;
        var timer = Stopwatch.StartNew();

        SKSurface? cachedSurface = null;
        GRBackendRenderTarget? cachedTarget = null;
        int cachedWidth = 0, cachedHeight = 0, cachedFbo = -1;

        window.Load += () =>
        {
            gl = window.CreateOpenGL();
            var grInterface = GRGlInterface.Create(name =>
                window.GLContext!.TryGetProcAddress(name, out var addr) ? addr : IntPtr.Zero);
            grContext = GRContext.CreateGl(grInterface);
            // 与宿主同样的缓存上限，排除"上限不同导致行为不同"这个变量。
            grContext.SetResourceCacheLimit(64L * 1024 * 1024);
        };

        window.Resize += size =>
        {
            width = size.X;
            height = size.Y;
            gl?.Viewport(size);
        };

        window.Render += _ =>
        {
            if (gl == null || grContext == null) return;
            if (width <= 0 || height <= 0) return;

            // 与 SilkDesktopHost.RenderFrame 逐行对应的最小实现。
            int fboId = gl.GetInteger(GLEnum.FramebufferBinding);
            var fbInfo = new GRGlFramebufferInfo((uint)fboId, 0x8058); // GL_RGBA8

            SKSurface? surface;
            GRBackendRenderTarget? target = null;
            if (cacheSurface)
            {
                if (cachedSurface == null || cachedWidth != width || cachedHeight != height || cachedFbo != fboId)
                {
                    cachedSurface?.Dispose();
                    cachedTarget?.Dispose();
                    cachedTarget = new GRBackendRenderTarget(width, height, 0, 8, fbInfo);
                    cachedSurface = SKSurface.Create(grContext, cachedTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
                    cachedWidth = width;
                    cachedHeight = height;
                    cachedFbo = fboId;
                }
                surface = cachedSurface;
            }
            else
            {
                target = new GRBackendRenderTarget(width, height, 0, 8, fbInfo);
                surface = SKSurface.Create(grContext, target, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
            }

            if (surface == null) { target?.Dispose(); return; }

            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Flush();
            grContext.Flush();
            window.GLContext?.SwapBuffers();

            if (!cacheSurface)
            {
                surface.Dispose();
                target?.Dispose();
            }

            if (++frame % 100 == 0) Report(logPath, grContext, width, height, frame, timer);

            if (!fixedSize)
            {
                // 自行驱动尺寸变化：每帧改一次，等价于用户以最快速度拖动边框。
                int next = width + direction;
                if (next > MaxWidth) { direction = -WidthStep; next = width + direction; }
                else if (next < MinWidth) { direction = WidthStep; next = width + direction; }
                window.Size = new Vector2D<int>(next, 500 + (next - MinWidth) / 2);
            }

            if (frame >= frameBudget) window.Close();
        };

        window.Run();

        cachedSurface?.Dispose();
        cachedTarget?.Dispose();
        grContext?.Dispose();
        gl?.Dispose();
    }

    private static void Report(string? logPath, GRContext grContext, int width, int height, int frame, Stopwatch timer)
    {
        grContext.GetResourceCacheUsage(out int count, out long bytes);
        using var process = Process.GetCurrentProcess();
        string line = $"{frame},{width}x{height},{count},{bytes},{process.PrivateMemorySize64}," +
                      $"{GC.GetTotalMemory(false)},{(int)timer.Elapsed.TotalSeconds}";
        Console.WriteLine(line);
        if (logPath != null) File.AppendAllText(logPath, line + "\n");
    }
}
