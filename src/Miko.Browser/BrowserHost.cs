using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Miko.Common;
using Miko.Events;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Browser.Native;
using Miko.Platform;
using Miko.Routing;

namespace Miko.Browser;

/// <summary>Starts a Miko application in the browser's WebAssembly runtime.</summary>
public static class BrowserHostExtensions
{
    /// <summary>
    /// Attaches the application to a canvas and starts on-demand rendering. The canvas defaults to
    /// <c>#miko-app</c>; its CSS size is the logical viewport and its backing store follows the device pixel ratio.
    /// <para>The application's HTML must load <c>_content/Miko.Browser/miko-browser.js</c> as a module.</para>
    /// </summary>
    [SupportedOSPlatform("browser")]
    public static void RunBrowser(this MikoAppContext context, string canvasId = "miko-app")
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(canvasId);
        BrowserHost.Start(context, canvasId);
    }
}

/// <summary>Single-threaded Canvas host. Browser events and frames both run on the WebAssembly main thread.</summary>
[SupportedOSPlatform("browser")]
internal sealed class BrowserHost
{
    private static BrowserHost? s_current;

    private readonly MikoAppContext _context;
    private readonly MikoInteractionController _controller;
    private readonly MikoDispatcher? _dispatcher;
    private readonly NavigationManager? _navigation;
    private readonly BrowserInputMethod _inputMethod = new();
    private readonly string _canvasId;

    private RenderSurface? _surface;
    private byte[]? _pixels;
    private int _pixelWidth;
    private int _pixelHeight;
    private float _scale = 1;
    private bool _initialized;
    private bool _needsPresent = true;
    private double _lastTimestamp;
    private double _keyboardInset;
    private bool _browserNavigationInProgress;

    private BrowserHost(MikoAppContext context, string canvasId)
    {
        _context = context;
        _controller = context.Controller;
        _canvasId = canvasId;
        _dispatcher = context.Services.GetService<MikoDispatcher>();
        _navigation = context.Services.GetService<NavigationManager>();
    }

    internal static BrowserHost Current => s_current
        ?? throw new InvalidOperationException("No Miko browser application is running. Call RunBrowser from Main first.");

    internal static void Start(MikoAppContext context, string canvasId)
    {
        if (s_current != null)
            throw new InvalidOperationException("Only one Miko browser application can run in a WebAssembly runtime.");

        var host = new BrowserHost(context, canvasId);
        s_current = host;
        host.Start();
    }

    private void Start()
    {
        if (_context.Services.GetService<PlatformInfo>() is { } platform)
            platform.Platform = HostPlatform.Browser;

        _controller.Engine.GraphicsDevice = GraphicsDevice.Software;
        _controller.AttachInputMethod(_inputMethod);
        _controller.CursorChanged += cursor => BrowserInterop.SetCursor(cursor switch
        {
            Cursor.Pointer => "pointer",
            Cursor.Text => "text",
            Cursor.Wait => "wait",
            Cursor.NotAllowed => "not-allowed",
            Cursor.Move => "move",
            Cursor.Help => "help",
            Cursor.None => "none",
            _ => "default",
        });
        if (_dispatcher != null) _dispatcher.Posted += BrowserInterop.RequestFrame;

        BrowserInterop.Initialize(_canvasId, _context.Options.Title);
        if (_navigation != null)
        {
            var path = BrowserNativeInterop.GetLocationPath();
            if (path.StartsWith('/') && path != _navigation.CurrentPath)
                _navigation.NavigateTo(path, NavigationDirection.Root);
            _navigation.LocationChanged += OnLocationChanged;
        }
        BrowserInterop.RequestFrame();
    }

    private void OnLocationChanged(NavigationEventArgs args)
    {
        // A hash change initiated by browser Back/Forward has already changed browser history.
        // Only a Miko-initiated back (e.g. IonBackButton) should call history.back().
        if (_browserNavigationInProgress) return;
        if (args.Direction == NavigationDirection.Back) BrowserNativeInterop.NavigateBack();
        else BrowserNativeInterop.SetLocationPath(args.ToPath);
    }

    internal void Navigate(string path)
    {
        if (_navigation == null || !path.StartsWith('/') || path == _navigation.CurrentPath) return;
        var direction = _navigation.CanGoBack && _navigation.History[^2] == path
            ? NavigationDirection.Back
            : NavigationDirection.Root;
        _browserNavigationInProgress = true;
        try
        {
            _navigation.NavigateTo(path, direction);
        }
        finally
        {
            _browserNavigationInProgress = false;
        }
        Changed();
    }

    internal void RenderFrame(double timestamp, double width, double height, double scale)
    {
        if (width <= 0 || height <= 0 || scale <= 0) return;
        // Sizing is also how a CSS-only resize invalidates an otherwise idle host.
        EnsureSurface(width, height, (float)scale);
        if (!_needsPresent && _initialized && !_controller.HasPendingWork) return;

        var surface = _surface!;
        if (!_initialized)
        {
            _context.RegisterFonts();
            _controller.Initialize(surface.Context, (float)width, (float)height);
            _controller.RunPostInitHooks();
            _initialized = true;
            _lastTimestamp = timestamp;
        }

        var delta = _lastTimestamp == 0 ? 0 : (timestamp - _lastTimestamp) / 1000.0;
        _lastTimestamp = timestamp;
        _controller.RenderFrame(surface.Context, (float)width, (float)height, (float)delta, canvas =>
        {
            canvas.Clear(_controller.Engine.GetRootBackgroundColor() ?? Color.White);
            canvas.Save();
            canvas.Scale(_scale, _scale);
            _controller.Engine.Render(canvas);
            canvas.Restore();
        });
        surface.Flush();
        surface.ReadRgba8888(_pixels!);
        BrowserInterop.Present(_pixels!, _pixelWidth, _pixelHeight);
        _needsPresent = false;

        if (_controller.HasPendingWork) BrowserInterop.RequestFrame();
    }

    private void EnsureSurface(double width, double height, float scale)
    {
        var pixelWidth = Math.Max(1, (int)Math.Round(width * scale));
        var pixelHeight = Math.Max(1, (int)Math.Round(height * scale));
        if (_surface != null && pixelWidth == _pixelWidth && pixelHeight == _pixelHeight && scale == _scale) return;

        _surface?.Dispose();
        _pixelWidth = pixelWidth;
        _pixelHeight = pixelHeight;
        _scale = scale;
        _pixels = new byte[checked(pixelWidth * pixelHeight * 4)];
        _surface = _controller.Engine.Graphics.CreateSurface(pixelWidth, pixelHeight)
            ?? throw new InvalidOperationException($"Could not create a {pixelWidth}×{pixelHeight} browser surface.");
        _needsPresent = true;
    }

    private void Changed()
    {
        _needsPresent = true;
        BrowserInterop.RequestFrame();
    }

    internal void PointerDown(double x, double y, int button, int pointerType, int pointerId)
    {
        _controller.OnPointerDown((float)x, (float)y, Button(button), PointerType(pointerType), pointerId);
        Changed();
    }

    internal void PointerUp(double x, double y, int button, int pointerType, int pointerId)
    {
        _controller.OnPointerUp((float)x, (float)y, Button(button), PointerType(pointerType), pointerId);
        Changed();
    }

    internal void PointerMove(double x, double y)
    {
        _controller.OnPointerMove((float)x, (float)y);
        Changed();
    }

    internal void PointerCancel(double x, double y, int pointerId)
    {
        _controller.OnPointerCancel((float)x, (float)y, pointerId);
        Changed();
    }

    internal void Wheel(double x, double y, double deltaX, double deltaY)
    {
        _controller.OnScroll((float)x, (float)y, (float)deltaX, (float)deltaY);
        Changed();
    }

    internal bool KeyDown(string code, int modifiers, bool repeat)
    {
        var key = BrowserKeyMap.ToMikoKey(code);
        if (key == MikoKey.Unknown) return false;
        var handled = repeat
            ? Repeat(key)
            : _controller.OnKeyDown(key, (MikoKeyModifiers)modifiers);
        Changed();
        return handled || BrowserKeyMap.IsEditingOrNavigationKey(key);
    }

    private bool Repeat(MikoKey key)
    {
        _controller.RepeatKey(key);
        return BrowserKeyMap.IsEditingOrNavigationKey(key);
    }

    internal void KeyUp(string code, int modifiers)
    {
        var key = BrowserKeyMap.ToMikoKey(code);
        if (key == MikoKey.Unknown) return;
        _controller.OnKeyUp(key, (MikoKeyModifiers)modifiers);
        Changed();
    }

    internal void TextInput(string text)
    {
        _controller.OnTextInput(text);
        Changed();
    }

    internal void DeleteSurroundingText(int before, int after)
    {
        _controller.DeleteSurroundingText(before, after);
        Changed();
    }

    internal void CompositionStart()
    {
        _controller.OnTextCompositionStart();
        Changed();
    }

    internal void CompositionUpdate(string text)
    {
        _controller.OnTextCompositionUpdate(text);
        Changed();
    }

    internal void CompositionEnd(string? text)
    {
        _controller.OnTextCompositionEnd(text);
        Changed();
    }

    internal void SetVisible(bool visible)
    {
        if (visible) _controller.Resume();
        else _controller.Suspend();
        BrowserNativeEvents.OnVisibilityChanged(visible);
        Changed();
    }

    internal void SetKeyboardInset(double height)
    {
        var previous = _keyboardInset;
        _keyboardInset = height;
        _controller.SetKeyboardInset((float)height);
        if (previous != height) BrowserNativeEvents.OnKeyboardInsetChanged(previous, height);
        Changed();
    }

    internal void SetSafeAreaInsets(double left, double top, double right, double bottom)
    {
        _controller.SetSafeAreaInsets((float)left, (float)top, (float)right, (float)bottom);
        Changed();
    }

    private static MouseButton Button(int button) => button switch
    {
        1 => MouseButton.Middle,
        2 => MouseButton.Right,
        _ => MouseButton.Left,
    };

    private static PointerType PointerType(int pointerType) => pointerType switch
    {
        1 => Miko.Events.PointerType.Touch,
        2 => Miko.Events.PointerType.Pen,
        _ => Miko.Events.PointerType.Mouse,
    };
}

/// <summary>Exports called by miko-browser.js. Not intended for application code.</summary>
[SupportedOSPlatform("browser")]
public static partial class BrowserCallbacks
{
    [JSExport] public static void RenderFrame(double timestamp, double width, double height, double scale)
        => BrowserHost.Current.RenderFrame(timestamp, width, height, scale);
    [JSExport] public static void PointerDown(double x, double y, int button, int pointerType, int pointerId)
        => BrowserHost.Current.PointerDown(x, y, button, pointerType, pointerId);
    [JSExport] public static void PointerUp(double x, double y, int button, int pointerType, int pointerId)
        => BrowserHost.Current.PointerUp(x, y, button, pointerType, pointerId);
    [JSExport] public static void PointerMove(double x, double y) => BrowserHost.Current.PointerMove(x, y);
    [JSExport] public static void PointerCancel(double x, double y, int pointerId)
        => BrowserHost.Current.PointerCancel(x, y, pointerId);
    [JSExport] public static void Wheel(double x, double y, double deltaX, double deltaY)
        => BrowserHost.Current.Wheel(x, y, deltaX, deltaY);
    [JSExport] public static bool KeyDown(string code, int modifiers, bool repeat)
        => BrowserHost.Current.KeyDown(code, modifiers, repeat);
    [JSExport] public static void KeyUp(string code, int modifiers) => BrowserHost.Current.KeyUp(code, modifiers);
    [JSExport] public static void TextInput(string text) => BrowserHost.Current.TextInput(text);
    [JSExport] public static void DeleteSurroundingText(int before, int after)
        => BrowserHost.Current.DeleteSurroundingText(before, after);
    [JSExport] public static void CompositionStart() => BrowserHost.Current.CompositionStart();
    [JSExport] public static void CompositionUpdate(string text) => BrowserHost.Current.CompositionUpdate(text);
    [JSExport] public static void CompositionEnd(string? text) => BrowserHost.Current.CompositionEnd(text);
    [JSExport] public static void SetVisible(bool visible) => BrowserHost.Current.SetVisible(visible);
    [JSExport] public static void SetKeyboardInset(double height) => BrowserHost.Current.SetKeyboardInset(height);
    [JSExport] public static void SetSafeAreaInsets(double left, double top, double right, double bottom)
        => BrowserHost.Current.SetSafeAreaInsets(left, top, right, bottom);
    [JSExport] public static void Navigate(string path) => BrowserHost.Current.Navigate(path);
}

[SupportedOSPlatform("browser")]
internal sealed class BrowserInputMethod : InputMethodBase
{
    public override void SetState(InputMethodState? state)
    {
        if (state == null)
        {
            BrowserInterop.HideKeyboard();
            return;
        }

        var type = state.InputType switch
        {
            InputMethodType.Password => "password",
            InputMethodType.Number => "number",
            InputMethodType.Multiline => "multiline",
            _ => "text",
        };
        var rect = state.CursorRect;
        BrowserInterop.SetInputMethodState(state.Text, state.CursorPosition, state.IsMultiline, type,
            rect.X, rect.Y, rect.Width, rect.Height);
    }

    public override void ShowKeyboard() => BrowserInterop.ShowKeyboard();
}
