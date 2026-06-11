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
/// 基于 Silk.NET 的桌面（Windows/Linux/macOS）宿主。拥有窗口、OpenGL 上下文与原生输入，
/// 驱动渲染循环，并把归一化后的输入事件转发给 <see cref="MikoInteractionController"/>。
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
    private int _width;
    private int _height;

    // 按键重复（属于窗口层关注点）
    private Key? _heldKey;
    private IKeyboard? _heldKeyboard;
    private readonly Stopwatch _keyHoldTimer = new();
    private bool _keyRepeatStarted;
    private const long KeyRepeatDelayMs = 500;
    private const long KeyRepeatIntervalMs = 33;

    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;
    private StandardCursor _currentCursor = StandardCursor.Default;
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
            API = GraphicsAPI.Default
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.Resize += OnResize;
        _window.Closing += OnClose;

        _logger.LogInformation("Starting Miko application: {Title}", _context.Options.Title);
        _window.Run();
        _window.Dispose();
    }

    private void OnLoad()
    {
        _gl = _window!.CreateOpenGL();

        var grInterface = GRGlInterface.Create(name =>
        {
            if (_window!.GLContext!.TryGetProcAddress(name, out var addr))
                return addr;
            return IntPtr.Zero;
        });

        _grContext = GRContext.CreateGl(grInterface);
        _logger.LogInformation("OpenGL context initialized");

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

        _context.RegisterFonts();

        using var tempSurface = SKSurface.Create(new SKImageInfo(_width, _height));
        _controller.Initialize(tempSurface.Canvas, _width, _height);
        _controller.RunPostInitHooks();

        _frameTimer.Start();
    }

    private void OnUpdate(double _)
    {
        if (_heldKey == null || _heldKeyboard == null) return;

        var elapsed = _keyHoldTimer.ElapsedMilliseconds;
        if (!_keyRepeatStarted)
        {
            if (elapsed >= KeyRepeatDelayMs)
            {
                _keyRepeatStarted = true;
                _keyHoldTimer.Restart();
                _controller.RepeatKey(SilkKeyMap.ToMikoKey(_heldKey.Value));
            }
        }
        else
        {
            if (elapsed >= KeyRepeatIntervalMs)
            {
                _keyHoldTimer.Restart();
                _controller.RepeatKey(SilkKeyMap.ToMikoKey(_heldKey.Value));
            }
        }
    }

    private void OnRender(double _)
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

        if (_controller.NeedsRebuild)
        {
            _controller.Rebuild(canvas, _width, _height);
        }

        _controller.Update(deltaTime);

        canvas.Clear(SKColors.White);
        _controller.Engine.Render(canvas);
        canvas.Flush();
        _grContext.Flush();
    }

    private void OnResize(Vector2D<int> size)
    {
        _width = size.X;
        _height = size.Y;
        _gl?.Viewport(size);
        _controller.SetViewportSize(size.X, size.Y);
        _logger.LogDebug("Viewport resized to {Width}x{Height}", size.X, size.Y);
    }

    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        _controller.OnPointerDown(mouse.Position.X, mouse.Position.Y, SilkKeyMap.ToMikoButton(button));
    }

    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        _controller.OnPointerUp(mouse.Position.X, mouse.Position.Y, SilkKeyMap.ToMikoButton(button));
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
    {
        _primaryMouse = mouse;
        _controller.OnPointerMove(position.X, position.Y);
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
    {
        // Silk 滚轮单位换算为像素增量（保持原行为：每格 40px，方向反转）
        float deltaY = scrollWheel.Y * -40f;
        float deltaX = scrollWheel.X * -40f;
        _controller.OnScroll(mouse.Position.X, mouse.Position.Y, deltaX, deltaY);
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        var mikoKey = SilkKeyMap.ToMikoKey(key);
        var mods = SilkKeyMap.GetModifiers(keyboard);

        bool consumed = _controller.OnKeyDown(mikoKey, mods);
        if (consumed) return;

        if (MikoInteractionController.IsRepeatableKey(mikoKey))
        {
            _heldKey = key;
            _heldKeyboard = keyboard;
            _keyRepeatStarted = false;
            _keyHoldTimer.Restart();
        }
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        if (_heldKey == key)
        {
            _heldKey = null;
            _heldKeyboard = null;
            _keyHoldTimer.Stop();
        }
        _controller.OnKeyUp(SilkKeyMap.ToMikoKey(key), SilkKeyMap.GetModifiers(keyboard));
    }

    private void OnKeyChar(IKeyboard keyboard, char character)
    {
        _controller.OnTextInput(character.ToString());
    }

    private void OnCursorChanged(Cursor cursor)
    {
        var standardCursor = SilkKeyMap.ToStandardCursor(cursor);
        if (standardCursor == _currentCursor) return;
        _currentCursor = standardCursor;

        if (_primaryMouse == null) return;
        _primaryMouse.Cursor.Type = CursorType.Standard;
        _primaryMouse.Cursor.StandardCursor = standardCursor;
    }

    private void OnClose()
    {
        _inputContext?.Dispose();
        _grContext?.Dispose();
        _gl?.Dispose();
    }
}
