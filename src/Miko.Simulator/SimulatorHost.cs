using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Common;
using Miko.Core;
using Miko.Events;
using Miko.Hosting;
using Miko.Platform;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Miko.Simulator;

/// <summary>
/// 设备模拟器宿主。窗口分为两部分：
/// <list type="bullet">
///   <item>左侧——被模拟应用的画面，渲染到一块**独立的**、按设备分辨率的离屏画布上，
///   再带边框合成到窗口（应用看到的视口尺寸 = 设备逻辑分辨率，与真机一致）。</item>
///   <item>右侧——模拟器设置面板，本身用**另一个 Miko 引擎**布局/渲染（满足"模拟器本身也使用 Miko 引擎"）。</item>
/// </list>
/// <para>
/// 线程模型沿用单线程 <see cref="IView.Run"/>（输入与渲染同线程），因此应用 DOM 与面板 DOM
/// 都只被这一个线程触碰，无需跨线程同步。
/// </para>
/// </summary>
public sealed class SimulatorHost
{
    // 设备画面四周留白与面板宽度。
    private const int PanelWidth = 300;
    private const int DeviceMargin = 32;
    private const float BezelThickness = 14f;
    private const float BezelRadius = 28f;

    private readonly MikoAppContext _appContext;
    private readonly MikoInteractionController _appController;
    private readonly SimulatorOptions _options;
    private readonly ILogger _logger;

    // The app's mutable host-platform singleton (if registered). Updated as the user selects a
    // different device so platform-dependent UI (e.g. Ionic's md/ios mode) switches with it.
    private readonly PlatformInfo? _platformInfo;

    // 面板引擎（与应用引擎相互独立）。
    private readonly MikoEngine _panelEngine = new();
    private readonly EventDispatcher _panelDispatcher = new();

    private IWindow? _window;
    private IInputContext? _inputContext;
    private GL? _gl;
    private GRContext? _grContext;

    // 应用画面离屏 GPU 画布（按设备物理像素分辨率），跨帧保留。
    private SKSurface? _appSurface;

    private int _windowWidth;
    private int _windowHeight;

    // 当前模拟状态。
    private DeviceProfile _device;
    private Orientation _orientation = Orientation.Portrait;
    private bool _safeAreaEnabled = true;
    private bool _appInitialized;

    // 设备画面在窗口中的屏幕矩形（用于输入坐标映射），每帧合成时更新。
    private SKRect _deviceScreenRect;

    private readonly Stopwatch _frameTimer = new();
    private float _lastFrameTime;

    // 面板交互拖拽状态。
    private bool _panelDragging;
    private MikoEngine.ScrollbarHitResult? _panelDraggingScrollbar;

    // 应用区指针按下记录（用于点击判定与拖拽转发）。
    private bool _appPointerActive;

    // 触屏指针光标（半透明圆圈，模拟触摸交互）。
    private ICursor? _touchCursor;
    private IMouse? _primaryMouse;
    private bool _isTouchCursorActive;

    private volatile bool _panelNeedsRebuild = true;

    // 跨线程调用队列：MCP 等后台线程通过 InvokeOnRenderThread 投递操作，
    // 在渲染线程每帧开头排空执行，保证 DOM/GL 只被渲染线程触碰。
    private readonly System.Collections.Concurrent.ConcurrentQueue<Action> _renderThreadQueue = new();

    // 公共访问器，供SimulatorService使用。
    public DeviceProfile CurrentDevice => _device;
    public IReadOnlyList<DeviceProfile> AvailableDevices => _options.Devices;
    public Orientation CurrentOrientation => _orientation;
    public bool SafeAreaEnabled => _safeAreaEnabled;
    public SKSurface? AppSurface => _appSurface;

    /// <summary>应用交互控制器，供调试服务转发模拟输入（点击/滚动等）。</summary>
    public MikoInteractionController AppController => _appController;

    /// <summary>当前设备的像素密度（逻辑坐标 → 物理像素的缩放系数）。</summary>
    public float DeviceScale => _device.Scale;

    /// <summary>
    /// 在渲染线程上执行 <paramref name="action"/> 并阻塞等待完成。
    /// 供 MCP 等后台线程安全地读取/修改 DOM，避免与渲染循环竞争。
    /// </summary>
    public void InvokeOnRenderThread(Action action)
    {
        using var done = new ManualResetEventSlim(false);
        Exception? error = null;
        _renderThreadQueue.Enqueue(() =>
        {
            try { action(); }
            catch (Exception ex) { error = ex; }
            finally { done.Set(); }
        });
        done.Wait();
        if (error != null) throw error;
    }

    /// <summary>在渲染线程上执行 <paramref name="func"/> 并返回其结果（阻塞等待）。</summary>
    public T InvokeOnRenderThread<T>(Func<T> func)
    {
        T result = default!;
        InvokeOnRenderThread(() => { result = func(); });
        return result;
    }

    // 渲染线程每帧开头排空队列。
    private void DrainRenderThreadQueue()
    {
        while (_renderThreadQueue.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception ex) { _logger.LogError(ex, "Error in render-thread queued action"); }
        }
    }

    public SimulatorHost(MikoAppContext appContext, SimulatorOptions options, ILogger<SimulatorHost>? logger = null)
    {
        _appContext = appContext;
        _appController = appContext.Controller;
        _options = options;
        _logger = logger ?? NullLogger<SimulatorHost>.Instance;
        _device = options.InitialDevice ?? options.Devices[0];
        _orientation = options.InitialOrientation;

        // The host platform is a mutable singleton; only present when registered (it is by
        // default — MikoAppBuilder.CreateDefault registers IPlatformInfo). Seed it from the
        // initial device BEFORE the app's first build so the starting mode matches the device.
        _platformInfo = _appContext.Services.GetService<IPlatformInfo>() as PlatformInfo;
        if (_platformInfo != null)
            _platformInfo.Platform = _device.Platform;
    }

    /// <summary>启动模拟器窗口并运行渲染循环（阻塞直到窗口关闭）。</summary>
    public void Run()
    {
        var (deviceW, deviceH) = CurrentLogicalSize();
        _windowWidth = deviceW + DeviceMargin * 2 + PanelWidth;
        _windowHeight = Math.Max(deviceH + DeviceMargin * 2, 480);

        var options = WindowOptions.Default with
        {
            Title = _options.Title ?? $"{_appContext.Options.Title} — Simulator",
            Size = new Vector2D<int>(_windowWidth, _windowHeight),
            API = GraphicsAPI.Default,
        };

        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Render += OnRender;
        _window.Resize += OnResize;
        _window.Closing += OnClose;

        _logger.LogInformation("Starting Miko simulator: {Title}", options.Title);
        _window.Run();
        _window.Dispose();
    }

    // 当前朝向下应用看到的逻辑视口尺寸。
    private (int width, int height) CurrentLogicalSize()
    {
        return _orientation == Orientation.Portrait
            ? (_device.LogicalWidth, _device.LogicalHeight)
            : (_device.LogicalHeight, _device.LogicalWidth);
    }

    // 当前朝向下的物理像素尺寸。
    private (int width, int height) CurrentPixelSize()
    {
        var (w, h) = CurrentLogicalSize();
        return ((int)MathF.Round(w * _device.Scale), (int)MathF.Round(h * _device.Scale));
    }

    // ---------------------------------------------------------------------
    // 生命周期
    // ---------------------------------------------------------------------

    private void OnLoad()
    {
        _gl = _window!.CreateOpenGL();

        var grInterface = GRGlInterface.Create(name =>
            _window!.GLContext!.TryGetProcAddress(name, out var addr) ? addr : IntPtr.Zero);
        _grContext = GRContext.CreateGl(grInterface);

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
            keyboard.KeyChar += OnKeyChar;
            keyboard.KeyDown += OnKeyDown;
        }

        CreateTouchCursor();

        // 应用引擎共享同一 GPU 上下文，供视频/图片等 GPU 资源使用。
        _appController.Engine.GraphicsContext = _grContext;
        _appContext.RegisterFonts();

        InitAppEngine();
        BuildPanel();
        _frameTimer.Start();
    }

    // 创建/重建按当前设备分辨率的离屏画布，并初始化应用引擎到该画布。
    private void InitAppEngine()
    {
        var (logicalW, logicalH) = CurrentLogicalSize();
        var (pixelW, pixelH) = CurrentPixelSize();

        _appSurface?.Dispose();

        // 使用 SKSurfaceProperties 指定像素几何信息，改善文字和图形的渲染质量。
        // 创建 GPU 支持的 Surface 时，指定 surface properties 以启用更好的子像素渲染。
        var surfaceProps = new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal);
        _appSurface = SKSurface.Create(_grContext, budgeted: true,
            new SKImageInfo(pixelW, pixelH, SKColorType.Rgba8888, SKAlphaType.Premul),
            0, surfaceProps);

        // 应用以逻辑坐标布局；离屏画布按 scale 放大渲染以保持清晰。
        if (!_appInitialized)
        {
            _appController.Initialize(_appSurface!.Canvas, logicalW, logicalH);
            _appController.RunPostInitHooks();
            _appInitialized = true;
        }
        else
        {
            _appController.SetViewportSize(logicalW, logicalH);
        }

        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        if (_safeAreaEnabled)
        {
            var sa = OrientedSafeArea();
            _appController.SetSafeAreaInsets(sa.Left, sa.Top, sa.Right, sa.Bottom);
        }
        else
        {
            _appController.SetSafeAreaInsets(0, 0, 0, 0);
        }
    }

    // 朝向变化时安全区需要相应旋转：竖屏的顶部刘海在横屏时移到一侧。
    private SafeAreaInsets OrientedSafeArea()
    {
        var s = _device.SafeArea;
        return _orientation == Orientation.Portrait
            ? s
            // 横屏（顺时针旋转设备）：原顶部 inset 落到左侧，原底部落到右侧。
            : new SafeAreaInsets(s.Top, 0, s.Bottom, 0);
    }

    /// <summary>
    /// 创建触屏指针光标：一个实心的灰色半透明圆形，模拟浏览器 DevTools 设备模式的触摸指针样式。
    /// </summary>
    private void CreateTouchCursor()
    {
        if (_inputContext == null || _inputContext.Mice.Count == 0) return;

        const int size = 32;           // 缩小位图尺寸
        const int radius = 12;         // 缩小圆半径
        const int centerX = size / 2;
        const int centerY = size / 2;

        // 生成 RGBA 位图：实心圆形，灰色半透明。
        var pixels = new byte[size * size * 4];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                int idx = (y * size + x) * 4;

                if (dist <= radius)
                {
                    // 圆内：实心灰色 (120, 120, 120)，半透明 alpha ~130。
                    // 边缘做抗锯齿渐变。
                    float alpha = dist < radius - 1f ? 1f : (radius - dist);
                    alpha = Math.Clamp(alpha, 0f, 1f);

                    pixels[idx + 0] = 120; // R
                    pixels[idx + 1] = 120; // G
                    pixels[idx + 2] = 120; // B
                    pixels[idx + 3] = (byte)(alpha * 130); // A (半透明)
                }
            }
        }

        try
        {
            var cursor = _primaryMouse!.Cursor;
            // Silk.NET ICursor 通过 Image 属性设置自定义位图，配合 HotspotX/Y 指定热点。
            cursor.Image = new RawImage(size, size, new Memory<byte>(pixels));
            cursor.HotspotX = centerX;
            cursor.HotspotY = centerY;
            // 缓存一个标志，表示我们已设置好自定义图像，随后只需切换 Type 即可。
            _touchCursor = cursor;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create touch cursor, falling back to default pointer");
        }
    }

    private void OnResize(Vector2D<int> size)
    {
        _windowWidth = size.X;
        _windowHeight = size.Y;

        // 最小化时窗口尺寸为 0×0。此时不要把 0 尺寸推给 GL/面板引擎——
        // 用 0 尺寸创建渲染目标会失败，且会污染布局状态。恢复时会再次收到 Resize。
        if (size.X == 0 || size.Y == 0) return;

        _gl?.Viewport(size);
        _panelEngine.SetViewportSize(PanelWidth, _windowHeight);
    }

    private void OnClose()
    {
        _appController.Engine.DisposeVideoSessions();
        _appSurface?.Dispose();
        // 自定义光标通过 Image 属性设置，无需手动释放 ICursor 本身。
        _inputContext?.Dispose();
        _grContext?.Dispose();
        _gl?.Dispose();
    }

    // ---------------------------------------------------------------------
    // 渲染
    // ---------------------------------------------------------------------

    private void OnRender(double _)
    {
        if (_grContext == null || _gl == null || _appSurface == null) return;

        // 排空跨线程调用队列（MCP 等后台线程投递的 DOM 读写操作），在任何渲染前于本线程执行。
        DrainRenderThreadQueue();

        // 最小化时窗口为 0×0。用 0 尺寸创建 GRBackendRenderTarget / SKSurface 会返回 null，
        // 解引用 Canvas 将抛异常并污染 GRContext，导致恢复后再也无法渲染（ISSUE-067 现象）。
        // 直接跳过本帧——恢复时尺寸恢复正常，渲染随之恢复。
        if (_windowWidth <= 0 || _windowHeight <= 0) return;

        // 必须在任何离屏渲染之前捕获窗口默认帧缓冲绑定：一旦渲染应用到离屏 SKSurface，
        // Skia 会绑定离屏 FBO 并保持绑定。若此后再读取 FramebufferBinding，拿到的将是离屏 FBO，
        // 合成内容就会画进用户看不到的离屏画布，窗口保持黑屏（ISSUE-067 现象）。
        int windowFbo = _gl.GetInteger(GLEnum.FramebufferBinding);

        float currentTime = (float)_frameTimer.Elapsed.TotalSeconds;
        float deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        // 1. 渲染应用到独立离屏画布（按 scale 放大）。
        RenderApp(deltaTime);

        // 2. 按需重建面板 DOM。
        if (_panelNeedsRebuild)
        {
            _panelNeedsRebuild = false;
            BuildPanel();
        }

        // 3. 合成到窗口默认帧缓冲。离屏渲染改动过 GL 状态，先让 GR 上下文重新同步。
        _grContext.ResetContext();
        var fbInfo = new GRGlFramebufferInfo((uint)windowFbo, 0x8058); // GL_RGBA8
        var target = new GRBackendRenderTarget(_windowWidth, _windowHeight, 0, 8, fbInfo);
        using var surface = SKSurface.Create(_grContext, target, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        var canvas = surface.Canvas;

        canvas.Clear(new SKColor(24, 25, 28));
        CompositeDevice(canvas);
        RenderPanel(canvas);

        canvas.Flush();
        _grContext.Flush();
    }

    // 把应用渲染进离屏画布。RenderFrame 持有输入/渲染锁，保证输入引发的 DOM 变更不与渲染竞争。
    private void RenderApp(float deltaTime)
    {
        var (logicalW, logicalH) = CurrentLogicalSize();
        var appCanvas = _appSurface!.Canvas;

        _appController.RenderFrame(appCanvas, logicalW, logicalH, deltaTime, c =>
        {
            var rootBg = _appController.Engine.GetRootBackgroundColor();
            c.Clear(rootBg?.ToSKColor() ?? SKColors.White);
            c.Save();
            c.Scale(_device.Scale);
            _appController.Engine.Render(c);
            c.Restore();
        });
        _appSurface.Flush();
    }

    // 把离屏应用画面带边框合成到窗口左侧，并记录设备屏幕矩形供输入映射。
    private void CompositeDevice(SKCanvas canvas)
    {
        var (logicalW, logicalH) = CurrentLogicalSize();

        // 设备屏幕在窗口中的位置（在面板左侧的区域内居中）。
        float availW = _windowWidth - PanelWidth;
        float screenLeft = MathF.Max(DeviceMargin, (availW - logicalW) / 2f);
        float screenTop = MathF.Max(DeviceMargin, (_windowHeight - logicalH) / 2f);
        _deviceScreenRect = SKRect.Create(screenLeft, screenTop, logicalW, logicalH);

        // 边框（圆角外壳）。
        var bezelRect = SKRect.Inflate(_deviceScreenRect, BezelThickness, BezelThickness);
        using (var bezelPaint = new SKPaint { Color = new SKColor(12, 12, 14), IsAntialias = true })
        {
            canvas.DrawRoundRect(bezelRect, BezelRadius, BezelRadius, bezelPaint);
        }

        // 把离屏画面缩放绘制到逻辑尺寸（离屏是物理像素分辨率）。
        using var image = _appSurface!.Snapshot();
        using (var clip = new SKRoundRect(_deviceScreenRect, BezelRadius / 2f))
        {
            canvas.Save();
            canvas.ClipRoundRect(clip, antialias: true);
            // 使用 Cubic 采样提供更好的缩小质量（从高分辨率缩小到显示尺寸）
            var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);
            canvas.DrawImage(image, _deviceScreenRect, sampling);
            canvas.Restore();
        }
    }

    private void RenderPanel(SKCanvas canvas)
    {
        canvas.Save();
        canvas.Translate(_windowWidth - PanelWidth, 0);
        canvas.ClipRect(SKRect.Create(0, 0, PanelWidth, _windowHeight));
        _panelEngine.Render(canvas);
        canvas.Restore();
    }

    private void BuildPanel()
    {
        var root = SimulatorPanel.Build(
            _options.Devices,
            _device,
            _orientation,
            _safeAreaEnabled,
            onSelectDevice: d => SelectDeviceInternal(d),
            onSetOrientation: o => SetOrientationInternal(o),
            onToggleSafeArea: ToggleSafeAreaInternal);

        var styleSheets = new List<Styling.StyleSheet> { SimulatorStyleSheet.Create() };
        using var tempSurface = SKSurface.Create(new SKImageInfo(Math.Max(1, PanelWidth), Math.Max(1, _windowHeight)));
        _panelEngine.Initialize(root, styleSheets, tempSurface.Canvas, PanelWidth, _windowHeight);
    }

    // ---------------------------------------------------------------------
    // 模拟状态变更
    // ---------------------------------------------------------------------

    // 公共方法，供SimulatorService调用。
    public void SelectDevice(DeviceProfile device) => SelectDeviceInternal(device);
    public void SetOrientation(Orientation orientation) => SetOrientationInternal(orientation);
    public void ToggleSafeArea(bool enabled) => ToggleSafeAreaInternal(enabled);

    private void SelectDeviceInternal(DeviceProfile device)
    {
        if (device.Name == _device.Name) return;
        var platformChanged = device.Platform != _device.Platform;
        _device = device;

        // Switch the app's host platform so mode-dependent UI (Ionic md/ios) follows the device,
        // then ask the controller to rebuild the DOM on the next frame with the new mode.
        if (platformChanged && _platformInfo != null)
        {
            _platformInfo.Platform = device.Platform;
            _appController.RequestRebuild();
        }

        InitAppEngine();
        _panelNeedsRebuild = true;
        _logger.LogInformation("Simulated device changed to {Device}", device);
    }

    private void SetOrientationInternal(Orientation orientation)
    {
        if (orientation == _orientation) return;
        _orientation = orientation;
        InitAppEngine();
        _panelNeedsRebuild = true;
        _logger.LogInformation("Orientation changed to {Orientation}", orientation);
    }

    private void ToggleSafeAreaInternal(bool enabled)
    {
        if (enabled == _safeAreaEnabled) return;
        _safeAreaEnabled = enabled;
        ApplySafeArea();
        _panelNeedsRebuild = true;
    }

    // ---------------------------------------------------------------------
    // 输入
    // ---------------------------------------------------------------------

    // 命中测试：判断窗口坐标落在设备画面区还是面板区。
    private bool InDevice(float x, float y) => _deviceScreenRect.Contains(x, y);
    private bool InPanel(float x) => x >= _windowWidth - PanelWidth;

    // 窗口坐标 → 应用逻辑坐标（设备画面以逻辑尺寸合成，故仅需平移）。
    private (float x, float y) ToAppCoords(float x, float y)
        => (x - _deviceScreenRect.Left, y - _deviceScreenRect.Top);

    // 窗口坐标 → 面板局部坐标。
    private (float x, float y) ToPanelCoords(float x, float y)
        => (x - (_windowWidth - PanelWidth), y);

    private void OnMouseDown(IMouse mouse, SilkMouseButton button)
    {
        float x = mouse.Position.X, y = mouse.Position.Y;
        var mikoButton = ToMikoButton(button);

        // 面板在上层，优先判断。面板区域的输入归面板，不穿透到设备层。
        if (InPanel(x) && button == SilkMouseButton.Left)
        {
            HandlePanelMouseDown(x, y);
            return;
        }

        // 面板之外，再判断设备画面。
        if (InDevice(x, y))
        {
            var (ax, ay) = ToAppCoords(x, y);
            _appPointerActive = true;
            _appController.OnPointerDown(ax, ay, mikoButton);
        }
    }

    private void OnMouseUp(IMouse mouse, SilkMouseButton button)
    {
        float x = mouse.Position.X, y = mouse.Position.Y;
        var mikoButton = ToMikoButton(button);

        // 如果正在进行应用区拖拽（_appPointerActive），优先完成拖拽，
        // 即使鼠标已经移出设备区域（拖拽跟随）。
        if (_appPointerActive)
        {
            _appPointerActive = false;
            var (ax, ay) = ToAppCoords(x, y);
            _appController.OnPointerUp(ax, ay, mikoButton);
            return;
        }

        // 面板拖拽或点击。
        if (button == SilkMouseButton.Left && (_panelDragging || InPanel(x)))
        {
            HandlePanelMouseUp(x, y);
        }
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
    {
        float x = position.X, y = position.Y;
        bool inPanel = InPanel(x);
        bool inDevice = !inPanel && InDevice(x, y); // 只有不在面板时才判断设备区

        // 鼠标进入/离开设备画面时切换光标：在设备区显示触屏圆圈，在面板或空白区恢复默认箭头。
        UpdateCursorForRegion(inDevice);

        // 面板拖拽（滚动条）优先。
        if (_panelDragging)
        {
            HandlePanelMouseMove(x, y);
            return;
        }

        // 应用区拖拽跟随（即使鼠标已移出设备区）。
        if (_appPointerActive)
        {
            var (ax, ay) = ToAppCoords(x, y);
            _appController.OnPointerMove(ax, ay);
            return;
        }

        // 鼠标在设备区但未按下：悬停移动事件转发给应用（用于 hover 等状态）。
        if (inDevice)
        {
            var (ax, ay) = ToAppCoords(x, y);
            _appController.OnPointerMove(ax, ay);
        }
    }

    /// <summary>根据鼠标是否在设备画面区域切换光标样式。</summary>
    private void UpdateCursorForRegion(bool inDeviceArea)
    {
        if (_primaryMouse == null || _touchCursor == null) return;

        if (inDeviceArea && !_isTouchCursorActive)
        {
            // 进入设备区：切换为触屏光标（Type = Custom 使用之前设置的 Image）。
            _primaryMouse.Cursor.Type = CursorType.Custom;
            _isTouchCursorActive = true;
        }
        else if (!inDeviceArea && _isTouchCursorActive)
        {
            // 离开设备区：恢复默认箭头。
            _primaryMouse.Cursor.Type = CursorType.Standard;
            _primaryMouse.Cursor.StandardCursor = StandardCursor.Default;
            _isTouchCursorActive = false;
        }
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
    {
        float x = mouse.Position.X, y = mouse.Position.Y;
        float deltaY = scrollWheel.Y * -40f;
        float deltaX = scrollWheel.X * -40f;

        // 面板在上层，滚动事件优先归面板。
        if (InPanel(x))
        {
            var (px, py) = ToPanelCoords(x, y);
            _panelEngine.ScrollBy(px, py, deltaX, deltaY);
        }
        else if (InDevice(x, y))
        {
            var (ax, ay) = ToAppCoords(x, y);
            _appController.OnScroll(ax, ay, deltaX, deltaY);
        }
    }

    private void OnKeyChar(IKeyboard keyboard, char character)
    {
        // 文本输入仅转发给应用（面板无文本输入控件）。
        _appController.OnTextInput(character.ToString());
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        var mikoKey = ToMikoKey(key);
        if (mikoKey == MikoKey.Unknown) return;
        _appController.OnKeyDown(mikoKey, GetModifiers(keyboard));
    }

    // 面板交互：复用引擎的滚动条命中与事件分发（与 DevTools 窗口同模式）。
    private void HandlePanelMouseDown(float windowX, float windowY)
    {
        var (px, py) = ToPanelCoords(windowX, windowY);

        var scrollbarHit = _panelEngine.HitTestScrollbar(px, py);
        if (scrollbarHit != null)
        {
            _panelDraggingScrollbar = scrollbarHit;
            _panelDragging = true;
            if (scrollbarHit.HitType is MikoEngine.ScrollbarHitType.VerticalThumb
                or MikoEngine.ScrollbarHitType.HorizontalThumb)
            {
                return;
            }
        }

        var target = _panelEngine.HitTest(px, py);
        if (target == null) return;

        var args = new MouseEventArgs
        {
            Target = target,
            X = px,
            Y = py,
            Button = Events.MouseButton.Left,
            Bubbles = true,
        };
        _panelDispatcher.Dispatch(target, EventTypes.Click, args);
    }

    private void HandlePanelMouseUp(float windowX, float windowY)
    {
        if (_panelDragging && _panelDraggingScrollbar != null)
        {
            var hit = _panelDraggingScrollbar;
            _panelDraggingScrollbar = null;
            _panelDragging = false;
            var (px, py) = ToPanelCoords(windowX, windowY);
            if (hit.HitType is MikoEngine.ScrollbarHitType.VerticalTrack
                or MikoEngine.ScrollbarHitType.HorizontalTrack)
            {
                _panelEngine.ScrollTrackClick(hit.Box, hit.HitType, px, py);
            }
            return;
        }
        _panelDragging = false;
    }

    private void HandlePanelMouseMove(float windowX, float windowY)
    {
        if (_panelDraggingScrollbar == null) return;
        var (px, py) = ToPanelCoords(windowX, windowY);
        var hit = _panelDraggingScrollbar;
        if (hit.HitType == MikoEngine.ScrollbarHitType.VerticalThumb)
            _panelEngine.DragVerticalThumb(hit.Box, py, hit.ThumbOffset);
        else if (hit.HitType == MikoEngine.ScrollbarHitType.HorizontalThumb)
            _panelEngine.DragHorizontalThumb(hit.Box, px, hit.ThumbOffset);
    }

    // ---------------------------------------------------------------------
    // Silk → Miko 类型映射（与 Miko.Windowing.SilkKeyMap 等价，此处内联以免依赖桌面宿主包）
    // ---------------------------------------------------------------------

    private static Events.MouseButton ToMikoButton(SilkMouseButton button) => button switch
    {
        SilkMouseButton.Middle => Events.MouseButton.Middle,
        SilkMouseButton.Right => Events.MouseButton.Right,
        _ => Events.MouseButton.Left,
    };

    private static MikoKey ToMikoKey(Key key) => key switch
    {
        Key.Backspace => MikoKey.Backspace,
        Key.Delete => MikoKey.Delete,
        Key.Left => MikoKey.Left,
        Key.Right => MikoKey.Right,
        Key.Home => MikoKey.Home,
        Key.End => MikoKey.End,
        Key.Enter or Key.KeypadEnter => MikoKey.Enter,
        Key.Tab => MikoKey.Tab,
        Key.Escape => MikoKey.Escape,
        Key.F1 => MikoKey.F1,
        Key.F2 => MikoKey.F2,
        Key.F3 => MikoKey.F3,
        Key.F4 => MikoKey.F4,
        Key.F5 => MikoKey.F5,
        Key.F6 => MikoKey.F6,
        Key.F7 => MikoKey.F7,
        Key.F8 => MikoKey.F8,
        Key.F9 => MikoKey.F9,
        Key.F10 => MikoKey.F10,
        Key.F11 => MikoKey.F11,
        Key.F12 => MikoKey.F12,
        _ => MikoKey.Unknown,
    };

    private static MikoKeyModifiers GetModifiers(IKeyboard keyboard)
    {
        var mods = MikoKeyModifiers.None;
        if (keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight))
            mods |= MikoKeyModifiers.Control;
        if (keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight))
            mods |= MikoKeyModifiers.Shift;
        if (keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight))
            mods |= MikoKeyModifiers.Alt;
        return mods;
    }
}
