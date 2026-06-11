using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Layout;
using Miko.Routing;
using SkiaSharp;

namespace Miko.Platform;

/// <summary>
/// 平台无关的交互控制器。封装了所有与具体窗口/输入框架无关的交互逻辑：
/// 命中测试、焦点管理、点击/选择/下拉、滚动条与滑块拖拽、文本输入编辑、
/// 事件分发、光标解析、路由重建与热重载标记。
/// <para>
/// 各平台实现层（桌面 Silk.NET、Android、iOS）只负责拥有窗口/GL/原生输入，
/// 并将归一化后的指针/键盘事件转发到本控制器。
/// </para>
/// </summary>
public sealed class MikoInteractionController
{
    private readonly MikoAppOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly MikoEngine _engine;
    private readonly HotReloadService _hotReloadService;
    private readonly ILogger<MikoInteractionController> _logger;
    private readonly EventDispatcher _eventDispatcher;

    private Router? _router;
    private NavigationManager? _navigationManager;
    private RouteView? _routeView;

    private System.Numerics.Vector2? _mouseDownPosition;
    private Cursor _currentCursor = Cursor.Default;

    // volatile because hot reload (UpdateApplication) sets this from a background thread,
    // while the render loop reads it on the render thread. Without volatile the render
    // thread may never observe the change due to CPU caching.
    private volatile bool _needsRebuild;

    private Element? _focusedElement;
    private InputElement? _draggingRange;
    private bool _isDragging;
    private MikoEngine.ScrollbarHitResult? _draggingScrollbar;

    public MikoInteractionController(
        IOptions<MikoAppOptions> options,
        IServiceProvider serviceProvider,
        MikoEngine engine,
        EventDispatcher eventDispatcher,
        HotReloadService hotReloadService,
        ILogger<MikoInteractionController> logger)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _engine = engine;
        _eventDispatcher = eventDispatcher;
        _hotReloadService = hotReloadService;
        _logger = logger;

        if (_options.RouteAssemblies != null || _options.RouteConfigurator != null)
        {
            _router = new Router();
            if (_options.RouteAssemblies != null)
                _router.ScanAssemblies(_options.RouteAssemblies);
            _options.RouteConfigurator?.Invoke(_router);
            _navigationManager = serviceProvider.GetRequiredService<NavigationManager>();
            _routeView = new RouteView(_router, _navigationManager, _options.DefaultLayout, _serviceProvider);
            _navigationManager.LocationChanged += _ => _needsRebuild = true;
        }

        // Set up hot reload if enabled
        if (_options.EnableHotReload)
        {
            _logger.LogInformation("[HotReload] Hot reload enabled, registering rebuild callback");
            _hotReloadService.OnReload(() =>
            {
                _logger.LogInformation("[HotReload] Rebuild callback invoked - setting _needsRebuild flag");
                _needsRebuild = true;
            });
        }
    }

    /// <summary>底层引擎，平台实现层用其进行渲染。</summary>
    public MikoEngine Engine => _engine;

    /// <summary>当前光标解析结果发生变化时触发，平台实现层据此应用原生光标。</summary>
    public event Action<Cursor>? CursorChanged;

    /// <summary>热重载/路由变更后是否需要重建 DOM 树。</summary>
    public bool NeedsRebuild => _needsRebuild;

    // ---------------------------------------------------------------------
    // 生命周期
    // ---------------------------------------------------------------------

    /// <summary>构建根元素（路由优先，否则使用根组件工厂）。</summary>
    public Element BuildRoot()
    {
        if (_routeView != null && _navigationManager != null)
            return _routeView.Render(_navigationManager.CurrentPath);

        return _options.RootComponentFactory?.Invoke()
            ?? throw new InvalidOperationException("No root component or router configured.");
    }

    /// <summary>初始化引擎并构建首帧 DOM 树。</summary>
    public void Initialize(SKCanvas canvas, float width, float height)
    {
        var root = BuildRoot();
        _engine.Initialize(root, _options.StyleSheets, canvas, width, height);
    }

    /// <summary>执行所有 PostInit 钩子（如 DevTools 桥接初始化）。</summary>
    public void RunPostInitHooks()
    {
        foreach (var hook in _options.PostInitHooks)
            hook(_serviceProvider);
    }

    /// <summary>当 <see cref="NeedsRebuild"/> 为真时重建 DOM 树。</summary>
    public void Rebuild(SKCanvas canvas, float width, float height)
    {
        _logger.LogInformation("[HotReload] Render loop detected _needsRebuild flag, rebuilding DOM tree");
        _needsRebuild = false;
        var root = BuildRoot();
        _engine.Initialize(root, _options.StyleSheets, canvas, width, height);
        _logger.LogInformation("[HotReload] DOM rebuilt and initialized, next frame will render new content");
    }

    /// <summary>推进动画（每帧调用）。</summary>
    public void Update(float deltaTime)
    {
        _engine.AnimationManager.Update(deltaTime);
    }

    /// <summary>视口尺寸变化。</summary>
    public void SetViewportSize(float width, float height)
    {
        _engine.SetViewportSize(width, height);
    }

    // ---------------------------------------------------------------------
    // 指针输入（坐标为已根据像素密度换算后的逻辑坐标）
    // ---------------------------------------------------------------------

    public void OnPointerDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left) return;
        _mouseDownPosition = new System.Numerics.Vector2(x, y);

        // Check scrollbar hit first
        var scrollbarHit = _engine.HitTestScrollbar(x, y);
        if (scrollbarHit != null)
        {
            _logger.LogTrace("Scrollbar hit: type={HitType}, element={Tag}#{Id}, thumbOffset={Offset}, pos=({X}, {Y})",
                scrollbarHit.HitType, scrollbarHit.Box.Element.TagName, scrollbarHit.Box.Element.Id ?? "",
                scrollbarHit.ThumbOffset, x, y);
            _draggingScrollbar = scrollbarHit;
            _isDragging = true;
            if (scrollbarHit.HitType == MikoEngine.ScrollbarHitType.VerticalThumb ||
                scrollbarHit.HitType == MikoEngine.ScrollbarHitType.HorizontalThumb)
            {
                _mouseDownPosition = null; // suppress click handling
            }
            return;
        }

        var target = _engine.HitTest(x, y);
        if (target is InputElement { Type: InputType.Range } rangeInput)
        {
            _isDragging = true;
            _draggingRange = rangeInput;
            UpdateRangeValue(rangeInput, x);
        }
    }

    public void OnPointerUp(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left) return;

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
                    _logger.LogTrace("Scrollbar track click: type={HitType}, element={Tag}#{Id}, pos=({X}, {Y})",
                        hit.HitType, hit.Box.Element.TagName, hit.Box.Element.Id ?? "", x, y);
                    _engine.ScrollTrackClick(hit.Box, hit.HitType, x, y);
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

    public void OnPointerMove(float x, float y)
    {
        if (_isDragging && _draggingScrollbar != null)
        {
            var hit = _draggingScrollbar;
            if (hit.HitType == MikoEngine.ScrollbarHitType.VerticalThumb)
            {
                _logger.LogTrace("Scrollbar thumb drag: vertical, element={Tag}#{Id}, mouseY={Y}",
                    hit.Box.Element.TagName, hit.Box.Element.Id ?? "", y);
                _engine.DragVerticalThumb(hit.Box, y, hit.ThumbOffset);
            }
            else if (hit.HitType == MikoEngine.ScrollbarHitType.HorizontalThumb)
            {
                _logger.LogTrace("Scrollbar thumb drag: horizontal, element={Tag}#{Id}, mouseX={X}",
                    hit.Box.Element.TagName, hit.Box.Element.Id ?? "", x);
                _engine.DragHorizontalThumb(hit.Box, x, hit.ThumbOffset);
            }
            return;
        }

        if (_isDragging && _draggingRange != null)
        {
            UpdateRangeValue(_draggingRange, x);
            return;
        }

        UpdateCursor(x, y);
    }

    /// <summary>
    /// 滚动。<paramref name="deltaX"/>/<paramref name="deltaY"/> 为像素增量
    /// （平台实现层负责将滚轮单位换算为像素）。
    /// </summary>
    public void OnScroll(float x, float y, float deltaX, float deltaY)
    {
        _logger.LogTrace("OnScroll: pos=({PosX}, {PosY}), delta=({DeltaX}, {DeltaY})", x, y, deltaX, deltaY);
        _engine.ScrollBy(x, y, deltaX, deltaY);
    }

    /// <summary>
    /// 解析指针下元素的 CSS 光标并在变化时通过 <see cref="CursorChanged"/> 通知平台层。
    /// </summary>
    private void UpdateCursor(float x, float y)
    {
        var target = _engine.HitTest(x, y);
        var cursor = ResolveCursor(target);

        if (cursor == _currentCursor) return;
        _currentCursor = cursor;
        CursorChanged?.Invoke(cursor);
    }

    /// <summary>
    /// 从悬停元素向上查找首个具有显式（非默认）光标的元素，近似 CSS 光标继承。
    /// 回退到 <see cref="Cursor.Default"/>。
    /// </summary>
    private static Cursor ResolveCursor(Element? element)
    {
        for (var current = element; current != null; current = current.Parent)
        {
            var computed = current.LayoutBox?.ComputedStyle;
            if (computed != null && computed.Cursor != Cursor.Default)
                return computed.Cursor;
        }
        return Cursor.Default;
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
            Button = MouseButton.Left,
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

    /// <summary>设置当前焦点元素，触发 blur/focus 事件。</summary>
    public void SetFocus(Element? newFocus)
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

    // ---------------------------------------------------------------------
    // 键盘 / 文本输入
    // ---------------------------------------------------------------------

    /// <summary>键按下。返回 true 表示该按键已被全局处理器消费。</summary>
    public bool OnKeyDown(MikoKey key, MikoKeyModifiers mods)
    {
        foreach (var handler in _options.GlobalKeyDownHandlers)
        {
            if (handler(key)) return true;
        }

        if (_focusedElement is not InputElement input) return false;
        if (input.Type != InputType.Text && input.Type != InputType.Password) return false;

        var keyName = key.ToString();
        bool ctrl = mods.HasFlag(MikoKeyModifiers.Control);

        var keyArgs = new KeyboardEventArgs
        {
            Target = input,
            Key = keyName,
            CtrlKey = ctrl,
            ShiftKey = mods.HasFlag(MikoKeyModifiers.Shift),
            AltKey = mods.HasFlag(MikoKeyModifiers.Alt),
            Bubbles = true
        };
        _eventDispatcher.Dispatch(input, EventTypes.KeyDown, keyArgs);

        ProcessKeyAction(key);
        return false;
    }

    /// <summary>键释放（当前无状态需要清理，保留以备扩展与对称性）。</summary>
    public void OnKeyUp(MikoKey key, MikoKeyModifiers mods)
    {
    }

    /// <summary>该按键在按住时是否应触发重复（用于平台层的按键重复定时）。</summary>
    public static bool IsRepeatableKey(MikoKey key) => key is
        MikoKey.Backspace or MikoKey.Delete or MikoKey.Left or MikoKey.Right or MikoKey.Home or MikoKey.End;

    /// <summary>由平台层在按键重复时调用，重新执行编辑动作。</summary>
    public void RepeatKey(MikoKey key)
    {
        ProcessKeyAction(key);
    }

    private void ProcessKeyAction(MikoKey key)
    {
        if (_focusedElement is not InputElement input) return;
        if (input.Type != InputType.Text && input.Type != InputType.Password) return;

        switch (key)
        {
            case MikoKey.Backspace:
                if (input.Backspace())
                    DispatchInputEvent(input);
                break;
            case MikoKey.Delete:
                if (input.Delete())
                    DispatchInputEvent(input);
                break;
            case MikoKey.Left:
                if (input.CursorPosition > 0)
                {
                    input.CursorPosition--;
                    input.IsDirty = true;
                }
                break;
            case MikoKey.Right:
                if (input.CursorPosition < (input.Value ?? string.Empty).Length)
                {
                    input.CursorPosition++;
                    input.IsDirty = true;
                }
                break;
            case MikoKey.Home:
                input.CursorPosition = 0;
                input.IsDirty = true;
                break;
            case MikoKey.End:
                input.MoveCursorToEnd();
                input.IsDirty = true;
                break;
        }
    }

    /// <summary>文本输入（已组合的字符）。控制字符会被忽略。</summary>
    public void OnTextInput(string text)
    {
        if (_focusedElement is not InputElement input) return;
        if (input.Type != InputType.Text && input.Type != InputType.Password) return;

        foreach (var character in text)
        {
            if (char.IsControl(character)) continue;
            input.InsertText(character.ToString());
        }
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

    // ---------------------------------------------------------------------
    // 辅助：滑块、滚动偏移、布局盒查找
    // ---------------------------------------------------------------------

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

    private LayoutBox? FindLayoutBoxForElement(Element element)
    {
        var layout = _engine.GetCurrentLayout();
        if (layout == null) return null;
        return FindLayoutBoxRecursive(layout, element);
    }

    private static LayoutBox? FindLayoutBoxRecursive(LayoutBox box, Element element)
    {
        if (box.Element == element) return box;
        foreach (var child in box.Children)
        {
            var found = FindLayoutBoxRecursive(child, element);
            if (found != null) return found;
        }
        return null;
    }
}
