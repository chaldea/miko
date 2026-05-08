using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Core;
using Miko.Events;
using Miko.Fonts;
using Miko.Routing;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace Miko.Hosting;

public class MikoApp
{
    private readonly MikoAppConfiguration _config;
    private readonly ILogger<MikoApp> _logger;
    private readonly MikoEngine _engine = new();

    private Router? _router;
    private NavigationManager? _navigationManager;
    private RouteView? _routeView;

    private IWindow? _window;
    private IInputContext? _inputContext;
    private readonly EventDispatcher _eventDispatcher = new();
    private System.Numerics.Vector2? _mouseDownPosition;
    private GL? _gl;
    private GRContext? _grContext;
    private int _width;
    private int _height;
    private bool _needsRebuild;

    internal MikoApp(MikoAppConfiguration config)
    {
        _config = config;
        _width = config.Width;
        _height = config.Height;

        ILoggerFactory loggerFactory = config.LoggingConfiguration != null
            ? LoggerFactory.Create(config.LoggingConfiguration)
            : NullLoggerFactory.Instance;

        _logger = loggerFactory.CreateLogger<MikoApp>();
        _engine.SetLogger(loggerFactory.CreateLogger<MikoEngine>());

        RegisterFonts(config);

        if (config.RouteAssemblies != null)
        {
            _router = new Router();
            _router.ScanAssemblies(config.RouteAssemblies);
            _navigationManager = new NavigationManager();
            _routeView = new RouteView(_router, _navigationManager, config.DefaultLayout);
            _navigationManager.LocationChanged += _ => _needsRebuild = true;
        }
    }

    private static void RegisterFonts(MikoAppConfiguration config)
    {
        foreach (var font in config.Fonts)
        {
            using var stream = font.Assembly.GetManifestResourceStream(font.ResourceName);
            if (stream == null)
                throw new InvalidOperationException($"Embedded resource not found: {font.ResourceName}");

            FontManager.Instance.RegisterFont(font.FamilyName, stream);
        }
    }

    public static MikoAppBuilder CreateBuilder() => new();

    public void Run()
    {
        var options = WindowOptions.Default with
        {
            Title = _config.Title,
            Size = new Vector2D<int>(_config.Width, _config.Height),
            API = GraphicsAPI.Default
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Resize += OnResize;
        _window.Closing += OnClose;

        _logger.LogInformation("Starting Miko application: {Title}", _config.Title);
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
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
        }

        using var tempSurface = SKSurface.Create(new SKImageInfo(_width, _height));
        var root = BuildRoot();
        _engine.Initialize(root, _config.StyleSheets, tempSurface.Canvas, _width, _height);
    }

    private Element BuildRoot()
    {
        if (_routeView != null && _navigationManager != null)
            return _routeView.Render(_navigationManager.CurrentPath);

        return _config.RootComponentFactory?.Invoke()
            ?? throw new InvalidOperationException("No root component or router configured.");
    }

    private void OnRender(double _)
    {
        if (_grContext == null || _gl == null) return;

        if (_needsRebuild)
        {
            _needsRebuild = false;
            var root = BuildRoot();
            using var tempSurface = SKSurface.Create(new SKImageInfo(_width, _height));
            _engine.Initialize(root, _config.StyleSheets, tempSurface.Canvas, _width, _height);
        }

        int fboId = _gl.GetInteger(GLEnum.FramebufferBinding);
        var fbInfo = new GRGlFramebufferInfo((uint)fboId, 0x8058); // GL_RGBA8
        var target = new GRBackendRenderTarget(_width, _height, 0, 8, fbInfo);

        using var surface = SKSurface.Create(_grContext, target, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        _engine.Render(canvas);
        canvas.Flush();
        _grContext.Flush();
    }

    private void OnResize(Vector2D<int> size)
    {
        _width = size.X;
        _height = size.Y;
        _gl?.Viewport(size);
        _engine.SetViewportSize(size.X, size.Y);
        _logger.LogDebug("Viewport resized to {Width}x{Height}", size.X, size.Y);
    }

    private void OnMouseDown(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        if (button != Silk.NET.Input.MouseButton.Left) return;
        _mouseDownPosition = mouse.Position;
    }

    private void OnMouseUp(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        if (button != Silk.NET.Input.MouseButton.Left) return;
        if (_mouseDownPosition == null) return;

        var position = _mouseDownPosition.Value;
        _mouseDownPosition = null;

        var target = _engine.HitTest(position.X, position.Y);
        if (target == null) return;

        var args = new MouseEventArgs
        {
            Target = target,
            X = position.X,
            Y = position.Y,
            Button = Events.MouseButton.Left,
            Bubbles = true
        };

        _eventDispatcher.Dispatch(target, EventTypes.Click, args);
    }

    private void OnClose()
    {
        _inputContext?.Dispose();
        _grContext?.Dispose();
        _gl?.Dispose();
    }
}
