using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
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

    private Element? _focusedElement;
    private InputElement? _draggingRange;
    private bool _isDragging;
    private MikoEngine.ScrollbarHitResult? _draggingScrollbar;

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
            mouse.MouseMove += OnMouseMove;
            mouse.Scroll += OnMouseScroll;
        }

        foreach (var keyboard in _inputContext.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyChar += OnKeyChar;
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

        // Check scrollbar hit first
        var scrollbarHit = _engine.HitTestScrollbar(mouse.Position.X, mouse.Position.Y);
        if (scrollbarHit != null)
        {
            _draggingScrollbar = scrollbarHit;
            _isDragging = true;
            if (scrollbarHit.HitType == MikoEngine.ScrollbarHitType.VerticalThumb ||
                scrollbarHit.HitType == MikoEngine.ScrollbarHitType.HorizontalThumb)
            {
                _mouseDownPosition = null; // suppress click handling
            }
            return;
        }

        var target = _engine.HitTest(mouse.Position.X, mouse.Position.Y);
        if (target is InputElement { Type: InputType.Range } rangeInput)
        {
            _isDragging = true;
            _draggingRange = rangeInput;
            UpdateRangeValue(rangeInput, mouse.Position.X);
        }
    }

    private void OnMouseUp(IMouse mouse, Silk.NET.Input.MouseButton button)
    {
        if (button != Silk.NET.Input.MouseButton.Left) return;

        if (_isDragging)
        {
            _isDragging = false;

            // Handle scrollbar track click (not thumb drag)
            if (_draggingScrollbar != null)
            {
                var hit = _draggingScrollbar;
                _draggingScrollbar = null;
                if (hit.HitType == MikoEngine.ScrollbarHitType.VerticalTrack ||
                    hit.HitType == MikoEngine.ScrollbarHitType.HorizontalTrack)
                {
                    _engine.ScrollTrackClick(hit.Box, hit.HitType, mouse.Position.X, mouse.Position.Y);
                }
            }

            _draggingRange = null;
            _mouseDownPosition = null;
            return;
        }

        if (_mouseDownPosition == null) return;

        var position = _mouseDownPosition.Value;
        _mouseDownPosition = null;

        var target = _engine.HitTest(position.X, position.Y);
        if (target == null)
        {
            SetFocus(null);
            return;
        }

        HandleClick(target, position.X, position.Y);
    }

    private void OnMouseMove(IMouse mouse, System.Numerics.Vector2 position)
    {
        if (_isDragging && _draggingScrollbar != null)
        {
            var hit = _draggingScrollbar;
            if (hit.HitType == MikoEngine.ScrollbarHitType.VerticalThumb)
                _engine.DragVerticalThumb(hit.Box, position.Y, hit.ThumbOffset);
            else if (hit.HitType == MikoEngine.ScrollbarHitType.HorizontalThumb)
                _engine.DragHorizontalThumb(hit.Box, position.X, hit.ThumbOffset);
            return;
        }

        if (_isDragging && _draggingRange != null)
        {
            UpdateRangeValue(_draggingRange, position.X);
        }
    }

    private void HandleClick(Element target, float x, float y)
    {
        // Check if click is on an open select's dropdown area first
        var dropdownHit = HitTestSelectDropdown(x, y);
        if (dropdownHit.HasValue)
        {
            var (selectElement, optionIndex) = dropdownHit.Value;
            HandleOptionClick(selectElement, optionIndex);
            return;
        }

        var args = new MouseEventArgs
        {
            Target = target,
            X = x,
            Y = y,
            Button = Events.MouseButton.Left,
            Bubbles = true
        };

        _eventDispatcher.Dispatch(target, EventTypes.Click, args);

        if (target is InputElement inputElement)
        {
            HandleInputClick(inputElement);
        }
        else if (target is SelectElement selectElement2)
        {
            HandleSelectClick(selectElement2);
        }
        else
        {
            CloseAllSelects();
            SetFocus(null);
        }
    }

    private void HandleInputClick(InputElement inputElement)
    {
        CloseAllSelects();

        switch (inputElement.Type)
        {
            case InputType.Text:
            case InputType.Password:
                SetFocus(inputElement);
                inputElement.MoveCursorToEnd();
                break;
            case InputType.Checkbox:
                inputElement.Checked = !inputElement.Checked;
                inputElement.IsDirty = true;
                DispatchChange(inputElement);
                break;
            case InputType.Radio:
                inputElement.Checked = true;
                inputElement.IsDirty = true;
                DispatchChange(inputElement);
                break;
        }
    }

    private void HandleSelectClick(SelectElement selectElement)
    {
        SetFocus(selectElement);
        selectElement.Toggle();
    }

    private void HandleOptionClick(SelectElement selectElement, int optionIndex)
    {
        if (optionIndex >= 0)
        {
            bool changed = selectElement.SelectOption(optionIndex);
            if (changed)
            {
                DispatchChange(selectElement);
            }
        }
        else
        {
            selectElement.Close();
        }
    }

    private (SelectElement select, int optionIndex)? HitTestSelectDropdown(float x, float y)
    {
        var root = _engine.GetRoot();
        if (root == null) return null;
        return HitTestSelectDropdownRecursive(root, x, y);
    }

    private (SelectElement select, int optionIndex)? HitTestSelectDropdownRecursive(Element element, float x, float y)
    {
        if (element is SelectElement { IsOpen: true } sel)
        {
            var layoutBox = FindLayoutBoxForElement(sel);
            if (layoutBox != null)
            {
                var scrollOffset = GetAccumulatedScrollOffset(sel);
                var borderBox = layoutBox.BoxModel.BorderBox;
                float left = borderBox.Left - scrollOffset.x;
                float right = borderBox.Right - scrollOffset.x;
                float bottom = borderBox.Bottom - scrollOffset.y;

                var options = sel.GetAllOptions();
                float fontSize = layoutBox.ComputedStyle.FontSize.Value;
                float optionHeight = fontSize + 8;
                float dropdownTop = bottom;
                float dropdownBottom = dropdownTop + options.Count * optionHeight;

                if (x >= left && x <= right &&
                    y >= dropdownTop && y <= dropdownBottom)
                {
                    int index = (int)((y - dropdownTop) / optionHeight);
                    index = Math.Clamp(index, 0, options.Count - 1);
                    return (sel, index);
                }
            }
        }

        foreach (var child in element.Children)
        {
            var result = HitTestSelectDropdownRecursive(child, x, y);
            if (result != null) return result;
        }
        return null;
    }

    private void CloseAllSelects()
    {
        var root = _engine.GetRoot();
        if (root == null) return;
        CloseSelectsRecursive(root);
    }

    private static void CloseSelectsRecursive(Element element)
    {
        if (element is SelectElement { IsOpen: true } sel)
        {
            sel.Close();
        }
        foreach (var child in element.Children)
        {
            CloseSelectsRecursive(child);
        }
    }

    private void SetFocus(Element? newFocus)
    {
        if (_focusedElement == newFocus) return;

        var oldFocus = _focusedElement;

        if (oldFocus != null)
        {
            oldFocus.ClearState(ElementState.Focus);
            var blurArgs = new FocusEventArgs
            {
                Target = oldFocus,
                RelatedTarget = newFocus,
                Bubbles = false
            };
            _eventDispatcher.Dispatch(oldFocus, EventTypes.Blur, blurArgs);

            if (oldFocus is SelectElement sel)
                sel.HandleBlur();
        }

        _focusedElement = newFocus;

        if (newFocus != null)
        {
            newFocus.SetState(ElementState.Focus);
            var focusArgs = new FocusEventArgs
            {
                Target = newFocus,
                RelatedTarget = oldFocus,
                Bubbles = false
            };
            _eventDispatcher.Dispatch(newFocus, EventTypes.Focus, focusArgs);
        }
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        if (_focusedElement is not InputElement input) return;
        if (input.Type != InputType.Text && input.Type != InputType.Password) return;

        var keyName = key.ToString();
        bool ctrl = keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight);

        var keyArgs = new KeyboardEventArgs
        {
            Target = input,
            Key = keyName,
            CtrlKey = ctrl,
            Bubbles = true
        };
        _eventDispatcher.Dispatch(input, EventTypes.KeyDown, keyArgs);

        switch (key)
        {
            case Key.Backspace:
                if (input.Backspace())
                    DispatchInputEvent(input);
                break;
            case Key.Delete:
                if (input.Delete())
                    DispatchInputEvent(input);
                break;
            case Key.Left:
                if (input.CursorPosition > 0)
                {
                    input.CursorPosition--;
                    input.IsDirty = true;
                }
                break;
            case Key.Right:
                if (input.CursorPosition < (input.Value ?? string.Empty).Length)
                {
                    input.CursorPosition++;
                    input.IsDirty = true;
                }
                break;
            case Key.Home:
                input.CursorPosition = 0;
                input.IsDirty = true;
                break;
            case Key.End:
                input.MoveCursorToEnd();
                input.IsDirty = true;
                break;
        }
    }

    private void OnKeyChar(IKeyboard keyboard, char character)
    {
        if (_focusedElement is not InputElement input) return;
        if (input.Type != InputType.Text && input.Type != InputType.Password) return;
        if (char.IsControl(character)) return;

        input.InsertText(character.ToString());
        DispatchInputEvent(input);
    }

    private void DispatchInputEvent(InputElement input)
    {
        var inputArgs = new InputEventArgs
        {
            Target = input,
            Data = input.Value ?? string.Empty,
            Bubbles = true
        };
        _eventDispatcher.Dispatch(input, EventTypes.Input, inputArgs);
    }

    private void DispatchChange(Element element)
    {
        var changeArgs = new ChangeEventArgs
        {
            Target = element,
            Bubbles = true
        };
        _eventDispatcher.Dispatch(element, EventTypes.Change, changeArgs);
    }

    private void UpdateRangeValue(InputElement rangeInput, float mouseX)
    {
        var layoutBox = FindLayoutBoxForElement(rangeInput);
        if (layoutBox == null) return;

        var scrollOffset = GetAccumulatedScrollOffset(rangeInput);
        var contentRect = layoutBox.BoxModel.Content;
        float adjustedLeft = contentRect.Left - scrollOffset.x;
        float adjustedRight = contentRect.Right - scrollOffset.x;
        float height = contentRect.Height;

        float thumbRadius = Math.Min(height / 2 - 2, 8);
        float trackLeft = adjustedLeft + thumbRadius;
        float trackRight = adjustedRight - thumbRadius;
        float trackWidth = trackRight - trackLeft;

        if (trackWidth <= 0) return;

        float percentage = Math.Clamp((mouseX - trackLeft) / trackWidth, 0f, 1f);
        float newValue = rangeInput.Min + (rangeInput.Max - rangeInput.Min) * percentage;

        if (Math.Abs(rangeInput.NumericValue - newValue) > 0.01f)
        {
            rangeInput.NumericValue = newValue;
            rangeInput.IsDirty = true;
            DispatchChange(rangeInput);
        }
    }

    private (float x, float y) GetAccumulatedScrollOffset(Element element)
    {
        float scrollX = 0, scrollY = 0;
        var current = element.Parent;
        while (current != null)
        {
            var box = FindLayoutBoxForElement(current);
            if (box != null)
            {
                scrollX += box.ScrollLeft;
                scrollY += box.ScrollTop;
            }
            current = current.Parent;
        }
        return (scrollX, scrollY);
    }

    private Layout.LayoutBox? FindLayoutBoxForElement(Element element)
    {
        var layout = _engine.GetCurrentLayout();
        if (layout == null) return null;
        return FindLayoutBoxRecursive(layout, element);
    }

    private static Layout.LayoutBox? FindLayoutBoxRecursive(Layout.LayoutBox box, Element element)
    {
        if (box.Element == element) return box;
        foreach (var child in box.Children)
        {
            var found = FindLayoutBoxRecursive(child, element);
            if (found != null) return found;
        }
        return null;
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
    {
        float deltaY = scrollWheel.Y * -40f;
        float deltaX = scrollWheel.X * -40f;
        _logger.LogTrace("OnMouseScroll: wheel=({WheelX}, {WheelY}), pos=({PosX}, {PosY}), delta=({DeltaX}, {DeltaY})",
            scrollWheel.X, scrollWheel.Y, mouse.Position.X, mouse.Position.Y, deltaX, deltaY);
        _engine.ScrollBy(mouse.Position.X, mouse.Position.Y, deltaX, deltaY);
    }

    private void OnClose()
    {
        _inputContext?.Dispose();
        _grContext?.Dispose();
        _gl?.Dispose();
    }
}
