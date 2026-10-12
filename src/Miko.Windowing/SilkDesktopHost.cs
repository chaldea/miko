using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Common;
using Miko.Hosting;
using Miko.Native;
using Miko.Platform;
using Miko.Rendering;
using Miko.Windowing.Common;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SkiaSharp;

namespace Miko.Windowing;

/// <summary>
/// 基于 Silk.NET 的桌面（Windows/Linux/macOS）宿主。
/// <para>
/// 线程模型：主线程只泵原生消息（<see cref="IView.DoEvents"/>），把输入/缩放事件**入队**；
/// 一个专用**渲染线程**持有 GPU 上下文，每帧排空队列、转发输入给
/// <see cref="MikoInteractionController"/>，再渲染并呈现。
/// </para>
/// <para>
/// 呈现后端见 <see cref="SilkWindowGraphics"/>：Windows/Linux 为 OpenGL，macOS 为 Metal（ISSUE-147）。
/// 窗口尺寸与输入坐标都是窗口坐标；Retina 屏上表面是物理像素，绘制前按后端的缩放系数缩放画布。
/// </para>
/// <para>
/// 这样做的原因（见 ISSUE-059）：Windows 上拖动标题栏/缩放边框会进入模态 move/size 消息循环，
/// 阻塞主线程的 <c>DoEvents</c> 直到松手。若渲染也在主线程（Silk 默认的 <c>Run()</c>），
/// 拖动期间画面会冻结、动画与视频卡住。把渲染移到独立线程后，主线程被模态循环卡住时渲染照常进行。
/// </para>
/// <para>
/// 同步采用**消息队列**而非锁：DOM 与引擎只被渲染线程这一个线程触碰（输入经队列在渲染线程消费），
/// 从根上避免跨线程 DOM 竞争。
/// </para>
/// </summary>
public sealed class SilkDesktopHost
{
    private readonly MikoAppContext _context;
    private readonly MikoInteractionController _controller;
    private readonly ILogger _logger;

    private IWindow? _window;
    private IInputContext? _inputContext;
    private SilkWindowGraphics? _graphics;
    private GRContext? _grContext;

    // 本帧画布缩放系数（窗口坐标 → 表面像素），由渲染线程在帧开始时从后端读取。
    private float _scale = 1f;
    private readonly Action<SKCanvas> _paint;

    // 仅渲染线程读写（队列消费时更新），无需同步。
    private int _width;
    private int _height;

    // 主线程 → 渲染线程的输入/缩放消息队列。
    private readonly ConcurrentQueue<SilkInputMessage> _messages = new();

    // 渲染线程 → 主线程的“最新光标”反向通道（只关心最新值，故用单字段而非队列）。
    private volatile StandardCursor _pendingCursor = StandardCursor.Default;
    private StandardCursor _appliedCursor = StandardCursor.Default;

    // 首帧（以及初始化/图形重建后的帧）必须呈现一次，即使引擎报告无待办工作——
    // 否则窗口在后端缓冲尚未绘制过时一直保持空白（ISSUE-096 空闲跳过逻辑的兜底）。
    private volatile bool _needsPresent = true;

    // 渲染线程帧计时。
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;

    // 按键重复（主线程按节拍入队 RepeatKey 消息；判定逻辑保持在窗口层）。
    private MikoKey? _heldKey;
    private bool _keyRepeatStarted;
    private readonly Stopwatch _keyHoldTimer = new();
    private const long KeyRepeatDelayMs = 500;
    private const long KeyRepeatIntervalMs = 33;

    private IMouse? _primaryMouse;
    private readonly SilkInputMethod _inputMethod;

    public SilkDesktopHost(MikoAppContext context, ILogger<SilkDesktopHost>? logger = null)
    {
        _context = context;
        _controller = context.Controller;
        _logger = logger ?? NullLogger<SilkDesktopHost>.Instance;
        _width = context.Options.Width;
        _height = context.Options.Height;
        _inputMethod = new SilkInputMethod();
        _inputMethod.SetLogicalViewport(_width, _height);
        _controller.AttachInputMethod(_inputMethod);

        _controller.CursorChanged += OnCursorChanged;
        _paint = PaintFrame;
    }

    public void Run()
    {
        var options = WindowOptions.Default with
        {
            Title = _context.Options.Title,
            Size = new Vector2D<int>(_context.Options.Width, _context.Options.Height),
            // macOS 的 Metal 后端需要不带 GL 上下文的窗口。
            API = SilkWindowGraphics.WindowApi,
            // 我们自行管理 GL 上下文与缓冲交换（上下文要转移到渲染线程）。
            IsContextControlDisabled = true,
            ShouldSwapAutomatically = false,
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Resize += OnResize;

        // 把窗口交给 Native 能力层。服务容器早在 MikoAppBuilder.Build() 时就构建好了，
        // 那时窗口还不存在，因此桌面 Native 服务只能在这里拿到宿主（延迟注入）。
        _context.Services.GetService<INativeHostContext>()?.Attach(_window);

        _logger.LogInformation("Starting Miko application: {Title}", _context.Options.Title);

        // 不用 _window.Run()（它在单线程上泵消息+渲染）。改为手动：
        // 主线程 Initialize + 泵消息，渲染在独立线程。
        var runner = new SilkWindowThreadRunner(
            _window,
            InitGraphics,
            RenderIteration,
            ShutdownGraphics)
        {
            ThreadName = "miko-desktop-render",
            BeforeEvents = _inputMethod.ApplyPendingState,
            AfterEvents = _inputMethod.ApplyPendingState,
            MainThreadIteration = () =>
            {
                PumpKeyRepeat();
                ApplyPendingCursor();
            },
        };
        runner.Run();

        _inputContext?.Dispose();
        _window.Reset();
        _window.Dispose();
    }

    /// <summary>
    /// 主线程：创建呈现后端并订阅原生输入。GPU 上下文与引擎初始化不在这里——它们需要在
    /// 渲染线程进行（见 <see cref="InitGraphics"/>）。
    /// </summary>
    private void OnLoad()
    {
        // Metal 后端要在主线程把图层挂到窗口视图上，因此后端在这里创建。
        _graphics = SilkWindowGraphics.Create(_window!);

        _inputMethod.SetWindowHandle(_window!.Native?.Win32?.Hwnd ?? IntPtr.Zero);
        _inputContext = _window!.CreateInput();
        foreach (var mouse in _inputContext.Mice)
        {
            _primaryMouse ??= mouse;
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
            mouse.MouseMove += OnMouseMove;
            mouse.Scroll += OnMouseScroll;
        }

        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
            keyboard.KeyChar += OnKeyChar;
        }
    }

    // ---------------------------------------------------------------------
    // 渲染线程
    // ---------------------------------------------------------------------

    private bool RenderIteration()
    {
        DrainMessages();
        // 帧转储未完成前强制出帧：ISSUE-096 的空闲跳过会让画面稳定后不再调用 RenderFrame，
        // 转储的帧计数就永远等不到（实测停在第 2 帧）。
        if (_dumpPath != null && !_dumpDone)
            _needsPresent = true;
        if (!_needsPresent && !_controller.HasPendingWork)
            return false;

        if (RenderFrame())
            _graphics!.Present();
        _needsPresent = false;
        return true;
    }

    private void ShutdownGraphics()
    {
        // 视频帧源持有本上下文上的 GPU 图像，须先于上下文释放。
        _controller.Engine.DisposeVideoSessions();
        _graphics?.Dispose();
        _grContext = null;
    }

    private void InitGraphics()
    {
        var graphics = _graphics!;
        graphics.Initialize();
        _grContext = graphics.Context;
        _logger.LogInformation("{Graphics} context initialized on render thread", graphics.Description);

        // 把 GPU 上下文交给引擎，供视频帧源把解码 GPU 资源零拷贝包装为图像。
        _controller.Engine.GraphicsContext = _grContext;

        _context.RegisterFonts();

        using var tempSurface = SKSurface.Create(new SKImageInfo(_width, _height));
        _controller.Initialize(tempSurface.Canvas, _width, _height);
        _controller.RunPostInitHooks();
        _frameTimer.Start();
    }

    /// <summary>渲染一帧到窗口表面。返回 false 表示本帧没有表面可画（未初始化/最小化），无需呈现。</summary>
    private bool RenderFrame()
    {
        if (_graphics == null || _grContext == null) return false;

        float currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        float deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        // OpenGL 后端的窗口表面按 (尺寸, FBO) 复用，不每帧重建（ISSUE-143，见 WindowRenderTarget）；
        // Metal 后端每帧包装图层轮换出的可绘制纹理。
        var surface = _graphics.BeginFrame(_width, _height);
        // 最小化时窗口为 0×0，返回 null：跳过本帧，恢复后尺寸正常渲染随之恢复。
        if (surface == null) return false;

        _scale = _graphics.Scale;

        // RenderFrame 内部仍走 _sync（对桌面已不竞争，因为输入也在本线程消费），
        // 与移动端保持一致。
        _controller.RenderFrame(surface.Canvas, _width, _height, deltaTime, _paint);

        MaybeDumpFrame(surface);
        return true;
    }

    private void PaintFrame(SKCanvas canvas)
    {
        canvas.Clear(SKColors.White);
        // 引擎按窗口坐标绘制；Retina 屏上表面是物理像素。
        canvas.Save();
        canvas.Scale(_scale);
        _controller.Engine.Render(canvas);
        canvas.Restore();
        canvas.Flush();
    }

    /// <summary>
    /// 诊断用：把真实窗口帧缓冲的内容转储为 PNG，由环境变量
    /// <c>MIKO_DUMP_FRAME=&lt;路径&gt;</c> 门控（未设置时只是一次字段判断）。
    ///
    /// <para>存在的意义是排除外部截图工具的干扰：截图软件可能缩放、重编码或丢弃 alpha，
    /// 拿它判断渲染质量会把工具的产物误认为引擎缺陷。这里抓的是 Skia 呈现到窗口的<b>同一张</b>
    /// 表面，因此能直接回答"引擎/宿主到底输出了什么"。</para>
    ///
    /// <para>只转储一帧（首帧之后自行关闭）：渲染循环每秒数十帧，逐帧写盘会拖垮它。
    /// 同时跳过前若干帧，等布局、字体与图片异步加载稳定后再抓。</para>
    /// </summary>
    private void MaybeDumpFrame(SKSurface surface)
    {
        if (_dumpPath == null || _dumpDone) return;

        // 前几帧图片可能还没解码完成（异步加载），等画面稳定。
        if (++_dumpFrameCounter < DumpAfterFrames) return;

        _dumpDone = true;
        try
        {
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            if (data != null)
            {
                File.WriteAllBytes(_dumpPath, data.ToArray());
                _logger.LogInformation("Dumped frame to {Path} ({Width}x{Height})",
                    _dumpPath, image.Width, image.Height);
            }
        }
        catch (Exception ex)
        {
            // 诊断功能失败不应影响渲染。
            _logger.LogWarning(ex, "Failed to dump frame to {Path}", _dumpPath);
        }
    }

    private readonly string? _dumpPath = Environment.GetEnvironmentVariable("MIKO_DUMP_FRAME");
    private bool _dumpDone;
    private int _dumpFrameCounter;
    private const int DumpAfterFrames = 10;

    /// <summary>渲染线程：排空主线程投递的输入/缩放消息并转发给控制器。</summary>
    private void DrainMessages()
    {
        while (_messages.TryDequeue(out var msg))
        {
            switch (msg.Kind)
            {
                case SilkInputMessageKind.PointerDown:
                    _controller.OnPointerDown(msg.X, msg.Y, msg.Button);
                    break;
                case SilkInputMessageKind.PointerUp:
                    _controller.OnPointerUp(msg.X, msg.Y, msg.Button);
                    break;
                case SilkInputMessageKind.PointerMove:
                    _controller.OnPointerMove(msg.X, msg.Y);
                    break;
                case SilkInputMessageKind.Scroll:
                    _controller.OnScroll(msg.X, msg.Y, msg.DeltaX, msg.DeltaY);
                    break;
                case SilkInputMessageKind.KeyDown:
                    _controller.OnKeyDown(msg.Key, msg.Modifiers);
                    break;
                case SilkInputMessageKind.KeyUp:
                    _controller.OnKeyUp(msg.Key, msg.Modifiers);
                    break;
                case SilkInputMessageKind.RepeatKey:
                    _controller.RepeatKey(msg.Key);
                    break;
                case SilkInputMessageKind.TextInput:
                    _inputMethod.Commit(msg.Text!);
                    break;
                case SilkInputMessageKind.Resize:
                    ApplyResize((int)msg.X, (int)msg.Y);
                    break;
            }
        }
    }

    private void ApplyResize(int width, int height)
    {
        _width = width;
        _height = height;
        // 尺寸变化后帧缓冲内容失效，必须强制呈现一帧（ISSUE-096 空闲跳过逻辑的兜底）。
        _needsPresent = true;
        _graphics?.Resize(width, height);
        _controller.SetViewportSize(width, height);
        _logger.LogDebug("Viewport resized to {Width}x{Height}", width, height);
        ReportResizeMemory(width, height);
    }

    /// <summary>
    /// 诊断用：由 <c>MIKO_RESIZE_LOG=&lt;路径&gt;</c> 门控，记录每次缩放后 Skia GPU 资源缓存
    /// 与进程内存的对应关系（ISSUE-143）。未设置时只是一次字段判断。
    ///
    /// <para>存在的意义：进程私有内存是个汇总数字，单看它无法区分「Skia 按新尺寸缓存了资源」
    /// 与「某处每帧泄漏」。把 Skia 自己报告的缓存字节数与同一时刻的私有内存并排记录，
    /// 才能把增长归因到具体持有方。</para>
    /// </summary>
    private void ReportResizeMemory(int width, int height)
    {
        if (_resizeLogPath == null || _grContext == null) return;

        var usage = GpuResourceCache.GetUsage(_grContext);
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        try
        {
            File.AppendAllText(_resizeLogPath,
                $"{width}x{height},{usage.ResourceCount},{usage.ResourceBytes}," +
                $"{process.PrivateMemorySize64},{GC.GetTotalMemory(false)}," +
                $"{SKGraphics.GetResourceCacheTotalBytesUsed()},{SKGraphics.GetFontCacheUsed()}," +
                $"{SKGraphics.GetFontCacheCountUsed()}\n");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to append resize diagnostics to {Path}", _resizeLogPath);
        }
    }

    private readonly string? _resizeLogPath = Environment.GetEnvironmentVariable("MIKO_RESIZE_LOG");

    // ---------------------------------------------------------------------
    // 主线程：原生输入回调 → 入队
    // ---------------------------------------------------------------------

    private void OnResize(Vector2D<int> size)
    {
        _inputMethod.SetLogicalViewport(size.X, size.Y);
        _messages.Enqueue(SilkInputMessage.Resize(size.X, size.Y));
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        _messages.Enqueue(SilkInputMessage.Pointer(SilkInputMessageKind.PointerDown, mouse.Position.X, mouse.Position.Y, SilkKeyMap.ToMikoButton(button)));
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        _messages.Enqueue(SilkInputMessage.Pointer(SilkInputMessageKind.PointerUp, mouse.Position.X, mouse.Position.Y, SilkKeyMap.ToMikoButton(button)));
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
    {
        _primaryMouse = mouse;
        _messages.Enqueue(SilkInputMessage.Pointer(SilkInputMessageKind.PointerMove, position.X, position.Y));
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
    {
        // Silk 滚轮单位换算为像素增量（保持原行为：每格 40px，方向反转）
        float deltaY = scrollWheel.Y * -40f;
        float deltaX = scrollWheel.X * -40f;
        _messages.Enqueue(SilkInputMessage.Scroll(mouse.Position.X, mouse.Position.Y, deltaX, deltaY));
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        var mikoKey = SilkKeyMap.ToMikoKey(key);
        var mods = SilkKeyMap.GetModifiers(keyboard);
        _messages.Enqueue(SilkInputMessage.Keyboard(SilkInputMessageKind.KeyDown, mikoKey, mods));

        // 全局键消费判定原本在 OnKeyDown 返回值里——现已异步入队，无法在此得知是否被消费。
        // 仍按可重复键启动重复定时；若该键被全局处理器消费，控制器的 RepeatKey 对其为 no-op。
        if (MikoInteractionController.IsRepeatableKey(mikoKey))
        {
            _heldKey = mikoKey;
            _keyRepeatStarted = false;
            _keyHoldTimer.Restart();
        }
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        var mikoKey = SilkKeyMap.ToMikoKey(key);
        if (_heldKey == mikoKey)
        {
            _heldKey = null;
            _keyHoldTimer.Stop();
        }
        _messages.Enqueue(SilkInputMessage.Keyboard(SilkInputMessageKind.KeyUp, mikoKey, SilkKeyMap.GetModifiers(keyboard)));
    }

    private void OnKeyChar(IKeyboard keyboard, char character)
    {
        _messages.Enqueue(SilkInputMessage.TextInput(character.ToString()));
    }

    /// <summary>主线程按节拍把 RepeatKey 入队（判定逻辑保持在窗口层）。</summary>
    private void PumpKeyRepeat()
    {
        if (_heldKey == null) return;

        var elapsed = _keyHoldTimer.ElapsedMilliseconds;
        if (!_keyRepeatStarted)
        {
            if (elapsed >= KeyRepeatDelayMs)
            {
                _keyRepeatStarted = true;
                _keyHoldTimer.Restart();
                _messages.Enqueue(SilkInputMessage.Keyboard(SilkInputMessageKind.RepeatKey, _heldKey.Value));
            }
        }
        else if (elapsed >= KeyRepeatIntervalMs)
        {
            _keyHoldTimer.Restart();
            _messages.Enqueue(SilkInputMessage.Keyboard(SilkInputMessageKind.RepeatKey, _heldKey.Value));
        }
    }

    // ---------------------------------------------------------------------
    // 光标（引擎→窗口，反向通道）
    // ---------------------------------------------------------------------

    /// <summary>渲染线程触发（命中测试在渲染帧内）：只记录最新光标，由主线程应用。</summary>
    private void OnCursorChanged(Cursor cursor)
    {
        _pendingCursor = SilkKeyMap.ToStandardCursor(cursor);
    }

    /// <summary>主线程：应用渲染线程请求的最新光标（GLFW 光标须在主线程设置）。</summary>
    private void ApplyPendingCursor()
    {
        var desired = _pendingCursor;
        if (desired == _appliedCursor || _primaryMouse == null) return;
        _appliedCursor = desired;
        _primaryMouse.Cursor.Type = CursorType.Standard;
        _primaryMouse.Cursor.StandardCursor = desired;
    }

}
