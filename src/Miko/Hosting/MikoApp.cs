using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Core;
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

    private IWindow? _window;
    private GL? _gl;
    private GRContext? _grContext;
    private int _width;
    private int _height;

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

        // Initial render with a temporary surface to satisfy Initialize signature
        using var tempSurface = SKSurface.Create(new SKImageInfo(_width, _height));
        var root = _config.RootComponentFactory?.Invoke() ?? throw new InvalidOperationException("No root component configured.");
        _engine.Initialize(root, _config.StyleSheets, tempSurface.Canvas, _width, _height);
    }

    private void OnRender(double _)
    {
        if (_grContext == null || _gl == null) return;

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

    private void OnClose()
    {
        _grContext?.Dispose();
        _gl?.Dispose();
    }
}
