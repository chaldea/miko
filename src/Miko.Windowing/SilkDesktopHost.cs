using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Common;
using Miko.Hosting;
using Miko.Platform;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace Miko.Windowing;

/// <summary>
/// 基于 Silk.NET 的桌面（Windows/Linux/macOS）宿主。
/// <para>
/// 线程模型：主线程只泵原生消息（<see cref="IView.DoEvents"/>），把输入/缩放事件**入队**；
/// 一个专用**渲染线程**持有 GL 上下文，每帧排空队列、转发输入给
/// <see cref="MikoInteractionController"/>，再渲染并交换缓冲。
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
    private GL? _gl;
    private GRContext? _grContext;

    // 仅渲染线程读写（队列消费时更新），无需同步。
    private int _width;
    private int _height;

    // 主线程 → 渲染线程的输入/缩放消息队列。
    private readonly ConcurrentQueue<InputMessage> _messages = new();

    // 渲染线程 → 主线程的“最新光标”反向通道（只关心最新值，故用单字段而非队列）。
    private volatile StandardCursor _pendingCursor = StandardCursor.Default;
    private StandardCursor _appliedCursor = StandardCursor.Default;

    private Thread? _renderThread;
    private volatile bool _stopRequested;

    // 首帧（以及初始化/图形重建后的帧）必须呈现一次，即使引擎报告无待办工作——
    // 否则窗口在后端缓冲尚未绘制过时一直保持空白（ISSUE-096 空闲跳过逻辑的兜底）。
    private volatile bool _needsPresent = true;

    // 渲染线程帧计时。
    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;

    // 按键重复（主线程按节拍入队 RepeatKey 消息；判定逻辑保持在窗口层）。
    private Key? _heldKey;
    private bool _keyRepeatStarted;
    private readonly Stopwatch _keyHoldTimer = new();
    private const long KeyRepeatDelayMs = 500;
    private const long KeyRepeatIntervalMs = 33;

    private IMouse? _primaryMouse;

    public SilkDesktopHost(MikoAppContext context, ILogger<SilkDesktopHost>? logger = null)
    {
        _context = context;
        _controller = context.Controller;
        _logger = logger ?? NullLogger<SilkDesktopHost>.Instance;
        _width = context.Options.Width;
        _height = context.Options.Height;

        _controller.CursorChanged += OnCursorChanged;
    }

    public void Run()
    {
        var options = WindowOptions.Default with
        {
            Title = _context.Options.Title,
            Size = new Vector2D<int>(_context.Options.Width, _context.Options.Height),
            API = GraphicsAPI.Default,
            // 我们自行管理 GL 上下文与缓冲交换（上下文要转移到渲染线程）。
            IsContextControlDisabled = true,
            ShouldSwapAutomatically = false,
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Resize += OnResize;

        _logger.LogInformation("Starting Miko application: {Title}", _context.Options.Title);

        // 不用 _window.Run()（它在单线程上泵消息+渲染）。改为手动：
        // 主线程 Initialize + 泵消息，渲染在独立线程。
        _window.Initialize();

        // 主线程释放 GL 上下文，交给渲染线程 MakeCurrent。
        _window.GLContext?.Clear();

        _renderThread = new Thread(RenderLoop)
        {
            IsBackground = true,
            Name = "miko-render",
        };
        _renderThread.Start();

        // 主线程消息泵：处理输入/缩放/关闭，并应用渲染线程请求的光标。
        // 拖动/缩放时 DoEvents 会阻塞在模态循环里——但渲染在另一线程，画面不冻结。
        while (!_window.IsClosing)
        {
            _window.DoEvents();
            PumpKeyRepeat();
            ApplyPendingCursor();
            Thread.Sleep(1);
        }

        // 关闭：停止并等待渲染线程（它会在自己线程内释放 GL 资源）。
        _stopRequested = true;
        _renderThread.Join();

        _window.DoEvents();
        _inputContext?.Dispose();
        _window.Reset();
        _window.Dispose();
    }

    /// <summary>
    /// 主线程：创建输入上下文并订阅原生输入。GL/引擎初始化不在这里——它们需要在
    /// 持有 GL 上下文的渲染线程进行（见 <see cref="RenderLoop"/> 的 InitGraphics）。
    /// </summary>
    private void OnLoad()
    {
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

    private void RenderLoop()
    {
        InitGraphics();
        _frameTimer.Start();

        while (!_stopRequested)
        {
            // 1. 排空输入队列（DOM 在此变更，与随后的渲染同线程）。
            DrainMessages();

            // 2. 稳态空闲检测：无任何视觉工作时跳过帧生产与缓冲交换，
            //    避免每秒 60 次全量重绘造成的 GC 锯齿（ISSUE-096）。
            //    输入事件在 DrainMessages 中已处理，会把新的工作产生出来。
            if (!_needsPresent && !_controller.HasPendingWork)
            {
                Thread.Sleep(1);
                continue;
            }

            // 3. 渲染一帧。
            RenderFrame();
            _needsPresent = false;

            // 4. 交换缓冲（我们接管了自动交换）。
            _window!.GLContext?.SwapBuffers();
        }

        // 在持有上下文的本线程释放 GL 资源与视频会话。
        _controller.Engine.DisposeVideoSessions();
        _grContext?.Dispose();
        _gl?.Dispose();
        _window!.GLContext?.Clear();
    }

    private void InitGraphics()
    {
        _window!.GLContext?.MakeCurrent();

        _gl = _window.CreateOpenGL();

        var grInterface = GRGlInterface.Create(name =>
            _window.GLContext!.TryGetProcAddress(name, out var addr) ? addr : IntPtr.Zero);

        _grContext = GRContext.CreateGl(grInterface);
        _logger.LogInformation("OpenGL context initialized on render thread");

        // 把 GPU 上下文交给引擎，供视频帧源把解码 GPU 资源零拷贝包装为图像。
        _controller.Engine.GraphicsContext = _grContext;

        _context.RegisterFonts();

        using var tempSurface = SKSurface.Create(new SKImageInfo(_width, _height));
        _controller.Initialize(tempSurface.Canvas, _width, _height);
        _controller.RunPostInitHooks();
    }

    private void RenderFrame()
    {
        if (_grContext == null || _gl == null) return;

        float currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        float deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        int fboId = _gl.GetInteger(GLEnum.FramebufferBinding);
        var fbInfo = new GRGlFramebufferInfo((uint)fboId, 0x8058); // GL_RGBA8
        var target = new GRBackendRenderTarget(_width, _height, 0, 8, fbInfo);

        using var surface = SKSurface.Create(_grContext, target, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        var canvas = surface.Canvas;

        // RenderFrame 内部仍走 _sync（对桌面已不竞争，因为输入也在本线程消费），
        // 与移动端保持一致。
        _controller.RenderFrame(canvas, _width, _height, deltaTime, c =>
        {
            c.Clear(SKColors.White);
            _controller.Engine.Render(c);
            c.Flush();
        });
        _grContext.Flush();
    }

    /// <summary>渲染线程：排空主线程投递的输入/缩放消息并转发给控制器。</summary>
    private void DrainMessages()
    {
        while (_messages.TryDequeue(out var msg))
        {
            switch (msg.Kind)
            {
                case MessageKind.PointerDown:
                    _controller.OnPointerDown(msg.X, msg.Y, msg.Button);
                    break;
                case MessageKind.PointerUp:
                    _controller.OnPointerUp(msg.X, msg.Y, msg.Button);
                    break;
                case MessageKind.PointerMove:
                    _controller.OnPointerMove(msg.X, msg.Y);
                    break;
                case MessageKind.Scroll:
                    _controller.OnScroll(msg.X, msg.Y, msg.DeltaX, msg.DeltaY);
                    break;
                case MessageKind.KeyDown:
                    _controller.OnKeyDown(msg.Key, msg.Modifiers);
                    break;
                case MessageKind.KeyUp:
                    _controller.OnKeyUp(msg.Key, msg.Modifiers);
                    break;
                case MessageKind.RepeatKey:
                    _controller.RepeatKey(msg.Key);
                    break;
                case MessageKind.TextInput:
                    _controller.OnTextInput(msg.Text!);
                    break;
                case MessageKind.Resize:
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
        _gl?.Viewport(new Vector2D<int>(width, height));
        _controller.SetViewportSize(width, height);
        _logger.LogDebug("Viewport resized to {Width}x{Height}", width, height);
    }

    // ---------------------------------------------------------------------
    // 主线程：原生输入回调 → 入队
    // ---------------------------------------------------------------------

    private void OnResize(Vector2D<int> size)
    {
        _messages.Enqueue(InputMessage.Resize(size.X, size.Y));
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        _messages.Enqueue(InputMessage.Pointer(MessageKind.PointerDown, mouse.Position.X, mouse.Position.Y, SilkKeyMap.ToMikoButton(button)));
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        _messages.Enqueue(InputMessage.Pointer(MessageKind.PointerUp, mouse.Position.X, mouse.Position.Y, SilkKeyMap.ToMikoButton(button)));
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
    {
        _primaryMouse = mouse;
        _messages.Enqueue(InputMessage.Pointer(MessageKind.PointerMove, position.X, position.Y, default));
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
    {
        // Silk 滚轮单位换算为像素增量（保持原行为：每格 40px，方向反转）
        float deltaY = scrollWheel.Y * -40f;
        float deltaX = scrollWheel.X * -40f;
        _messages.Enqueue(InputMessage.ScrollMsg(mouse.Position.X, mouse.Position.Y, deltaX, deltaY));
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        var mikoKey = SilkKeyMap.ToMikoKey(key);
        var mods = SilkKeyMap.GetModifiers(keyboard);
        _messages.Enqueue(InputMessage.Keyboard(MessageKind.KeyDown, mikoKey, mods));

        // 全局键消费判定原本在 OnKeyDown 返回值里——现已异步入队，无法在此得知是否被消费。
        // 仍按可重复键启动重复定时；若该键被全局处理器消费，控制器的 RepeatKey 对其为 no-op。
        if (MikoInteractionController.IsRepeatableKey(mikoKey))
        {
            _heldKey = key;
            _keyRepeatStarted = false;
            _keyHoldTimer.Restart();
        }
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        if (_heldKey == key)
        {
            _heldKey = null;
            _keyHoldTimer.Stop();
        }
        _messages.Enqueue(InputMessage.Keyboard(MessageKind.KeyUp, SilkKeyMap.ToMikoKey(key), SilkKeyMap.GetModifiers(keyboard)));
    }

    private void OnKeyChar(IKeyboard keyboard, char character)
    {
        _messages.Enqueue(InputMessage.TextInputMsg(character.ToString()));
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
                _messages.Enqueue(InputMessage.Repeat(SilkKeyMap.ToMikoKey(_heldKey.Value)));
            }
        }
        else if (elapsed >= KeyRepeatIntervalMs)
        {
            _keyHoldTimer.Restart();
            _messages.Enqueue(InputMessage.Repeat(SilkKeyMap.ToMikoKey(_heldKey.Value)));
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

    // ---------------------------------------------------------------------
    // 输入消息
    // ---------------------------------------------------------------------

    private enum MessageKind
    {
        PointerDown, PointerUp, PointerMove, Scroll,
        KeyDown, KeyUp, RepeatKey, TextInput, Resize,
    }

    /// <summary>主线程 → 渲染线程的输入/缩放消息（轻量值类型，避免每事件分配）。</summary>
    private readonly struct InputMessage
    {
        public readonly MessageKind Kind;
        public readonly float X, Y, DeltaX, DeltaY;
        public readonly Miko.Events.MouseButton Button;
        public readonly MikoKey Key;
        public readonly MikoKeyModifiers Modifiers;
        public readonly string? Text;

        private InputMessage(MessageKind kind, float x, float y, float dx, float dy,
            Miko.Events.MouseButton button, MikoKey key, MikoKeyModifiers mods, string? text)
        {
            Kind = kind; X = x; Y = y; DeltaX = dx; DeltaY = dy;
            Button = button; Key = key; Modifiers = mods; Text = text;
        }

        public static InputMessage Pointer(MessageKind kind, float x, float y, Miko.Events.MouseButton button)
            => new(kind, x, y, 0, 0, button, default, default, null);

        public static InputMessage ScrollMsg(float x, float y, float dx, float dy)
            => new(MessageKind.Scroll, x, y, dx, dy, default, default, default, null);

        public static InputMessage Keyboard(MessageKind kind, MikoKey key, MikoKeyModifiers mods)
            => new(kind, 0, 0, 0, 0, default, key, mods, null);

        public static InputMessage Repeat(MikoKey key)
            => new(MessageKind.RepeatKey, 0, 0, 0, 0, default, key, default, null);

        public static InputMessage TextInputMsg(string text)
            => new(MessageKind.TextInput, 0, 0, 0, 0, default, default, default, text);

        // Resize 复用 X/Y 携带宽/高。
        public static InputMessage Resize(int width, int height)
            => new(MessageKind.Resize, width, height, 0, 0, default, default, default, null);
    }
}
