using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Hosting;
using Miko.Layout;
using Miko.Platform.Resources;
using Miko.Platform.Video;
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
    private readonly MikoSynchronizationContext _syncContext;

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

    // Serializes input handling against the render frame. On hosts that render on a
    // dedicated thread (Android GLThread, iOS CADisplayLink) the render thread walks the
    // DOM during layout (LayoutEngine.ComputeStyles enumerates Element.Children) while
    // input events on the UI thread mutate the DOM synchronously (a click runs the
    // component handler, which calls StateHasChanged and rewrites Children). Without this
    // lock the two race and throw "Collection was modified; enumeration operation may not
    // execute". The desktop (Silk) host pumps input and render on one thread, so it never
    // hit this — but the lock is harmless there (uncontended, re-entrant).
    private readonly object _sync = new();

    public MikoInteractionController(
        IOptions<MikoAppOptions> options,
        IServiceProvider serviceProvider,
        MikoEngine engine,
        EventDispatcher eventDispatcher,
        MikoDispatcher dispatcher,
        HotReloadService hotReloadService,
        ILogger<MikoInteractionController> logger)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _engine = engine;
        _eventDispatcher = eventDispatcher;
        _hotReloadService = hotReloadService;
        _logger = logger;
        _syncContext = new MikoSynchronizationContext(dispatcher);

        // 视频后端为可选服务：注册了平台后端（如桌面 FFmpegVideoBackend）时注入引擎，
        // 否则 <video> 元素仅显示背景/poster。
        _engine.VideoBackend = serviceProvider.GetService<IVideoBackend>();

        // 图片加载器：优先用 DI 注册的实现（应用可经 UseImageLoading 自定义）；
        // 否则注入内置 ResourceManager，复用 DI 中的 HttpClient（若已注册）与入口程序集解析 res://。
        _engine.ImageLoader = serviceProvider.GetService<IImageLoader>();

        // 语法高亮器：默认注册内置实现；应用可重新注册 ISyntaxHighlighter 覆盖
        // （如自定义配色、新增语言或接入完整语法分析，见 ISSUE-098）。
        if (serviceProvider.GetService<Highlight.ISyntaxHighlighter>() is { } syntaxHighlighter)
        {
            _engine.SyntaxHighlighter = syntaxHighlighter;
        }

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

    /// <summary>
    /// 是否有需要在下一帧呈现的视觉工作（DOM 重建请求，或引擎侧存在脏区域/动画/视频/
    /// 排队的回调/布局输入变化）。平台宿主可在其为 false 时跳过帧生产（不交换缓冲）
    /// 并短暂休眠——稳态下做到零分配、零 GPU 提交的空闲（ISSUE-096）。
    /// 输入事件不经此判断：宿主每轮循环都应先排空输入队列，输入处理会把工作产生出来。
    /// </summary>
    public bool HasPendingWork
    {
        get { lock (_sync) { return _needsRebuild || _engine.HasPendingVisualWork; } }
    }

    /// <summary>
    /// 请求在下一帧重建整个 DOM 树（在锁保护下由 <see cref="RenderFrame"/> 执行）。
    /// 供平台宿主在影响整棵树的外部状态变化后调用——例如模拟器切换设备/平台后，
    /// 让 Ionic 组件以新的 mode 重新渲染。
    /// </summary>
    public void RequestRebuild() => _needsRebuild = true;

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
        // Install the sync context before BuildRoot so that OnInitializedAsync
        // continuations are captured and posted back to the dispatcher (render thread).
        var prevCtx = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_syncContext);
        Element root;
        try { root = BuildRoot(); }
        finally { SynchronizationContext.SetSynchronizationContext(prevCtx); }
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
        // Install the sync context so OnInitializedAsync continuations post to the dispatcher.
        var prevCtx = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_syncContext);
        Element root;
        try { root = BuildRoot(); }
        finally { SynchronizationContext.SetSynchronizationContext(prevCtx); }
        _engine.Initialize(root, _options.StyleSheets, canvas, width, height);
        _logger.LogInformation("[HotReload] DOM rebuilt and initialized, next frame will render new content");
    }

    /// <summary>推进动画（每帧调用）。</summary>
    public void Update(float deltaTime)
    {
        _engine.AnimationManager.Update(deltaTime);
    }

    /// <summary>
    /// 在输入/渲染锁的保护下执行一帧：先按需重建 DOM 树，再推进动画，最后调用
    /// <paramref name="render"/> 绘制。平台宿主应通过本方法渲染，而非直接调用
    /// <see cref="Rebuild"/>/<see cref="Update"/>/<c>Engine.Render</c>，以确保输入处理
    /// 引发的 DOM 变更不会与布局/渲染对 DOM 的遍历并发执行。
    /// </summary>
    public void RenderFrame(SKCanvas canvas, float width, float height, float deltaTime, Action<SKCanvas> render)
    {
        lock (_sync)
        {
            if (_needsRebuild)
                Rebuild(canvas, width, height);

            Update(deltaTime);
            render(canvas);
        }
    }

    /// <summary>视口尺寸变化。</summary>
    public void SetViewportSize(float width, float height)
    {
        lock (_sync)
        {
            _engine.SetViewportSize(width, height);
        }
    }

    /// <summary>
    /// 安全区边距变化（逻辑像素）。平台宿主从系统状态栏/导航栏 inset 换算为逻辑像素后调用，
    /// 使根内容内缩到安全区内，避免被系统 UI 遮盖。
    /// </summary>
    public void SetSafeAreaInsets(float left, float top, float right, float bottom)
    {
        lock (_sync)
        {
            _engine.SetSafeAreaInsets(new SafeAreaInsets(left, top, right, bottom));
        }
    }

    // ---------------------------------------------------------------------
    // 指针输入（坐标为已根据像素密度换算后的逻辑坐标）
    // ---------------------------------------------------------------------

    public void OnPointerDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left) return;
        lock (_sync)
        {
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
    }

    public void OnPointerUp(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left) return;
        lock (_sync)
        {
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
                SetFocusCore(null);
                return;
            }

            HandleClick(target, position.X, position.Y);
        }
    }

    public void OnPointerMove(float x, float y)
    {
        lock (_sync)
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
    }

    /// <summary>
    /// 滚动。<paramref name="deltaX"/>/<paramref name="deltaY"/> 为像素增量
    /// （平台实现层负责将滚轮单位换算为像素）。
    /// </summary>
    public void OnScroll(float x, float y, float deltaX, float deltaY)
    {
        lock (_sync)
        {
            _logger.LogTrace("OnScroll: pos=({PosX}, {PosY}), delta=({DeltaX}, {DeltaY})", x, y, deltaX, deltaY);
            _engine.ScrollBy(x, y, deltaX, deltaY);
        }
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

        DispatchWithSyncContext(target, EventTypes.Click, args);

        if (target is InputElement inputElement)
        {
            HandleInputClick(inputElement);
        }
        else if (target is TextAreaElement textAreaElement)
        {
            CloseAllSelects();
            SetFocusCore(textAreaElement);
            textAreaElement.MoveCursorToEnd();
        }
        else if (target is SelectElement selectElement2)
        {
            HandleSelectClick(selectElement2);
        }
        else
        {
            CloseAllSelects();
            SetFocusCore(null);
        }
    }

    private void HandleInputClick(InputElement inputElement)
    {
        CloseAllSelects();

        switch (inputElement.Type)
        {
            case InputType.Text:
            case InputType.Password:
                SetFocusCore(inputElement);
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
        SetFocusCore(selectElement);
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
        lock (_sync)
        {
            SetFocusCore(newFocus);
        }
    }

    /// <summary>
    /// Wraps event dispatcher invocation with the MikoSynchronizationContext so async
    /// event handlers resume on the render thread.
    /// </summary>
    private void DispatchWithSyncContext<T>(Element target, string eventType, T args) where T : MikoEventArgs
    {
        var prevContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_syncContext);
        try
        {
            _eventDispatcher.Dispatch(target, eventType, args);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prevContext);
        }
    }

    private void SetFocusCore(Element? newFocus)
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
            DispatchWithSyncContext(oldFocus, EventTypes.Blur, blurArgs);

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
            DispatchWithSyncContext(newFocus, EventTypes.Focus, focusArgs);
        }
    }

    // ---------------------------------------------------------------------
    // 键盘 / 文本输入
    // ---------------------------------------------------------------------

    /// <summary>键按下。返回 true 表示该按键已被全局处理器消费。</summary>
    public bool OnKeyDown(MikoKey key, MikoKeyModifiers mods)
    {
        lock (_sync)
        {
            foreach (var handler in _options.GlobalKeyDownHandlers)
            {
                if (handler(key)) return true;
            }

            if (_focusedElement is not ITextEditable editable || !editable.IsEditable) return false;
            var editableElement = (Element)_focusedElement;

            var keyName = key.ToString();
            bool ctrl = mods.HasFlag(MikoKeyModifiers.Control);

            var keyArgs = new KeyboardEventArgs
            {
                Target = editableElement,
                Key = keyName,
                CtrlKey = ctrl,
                ShiftKey = mods.HasFlag(MikoKeyModifiers.Shift),
                AltKey = mods.HasFlag(MikoKeyModifiers.Alt),
                Bubbles = true
            };
            DispatchWithSyncContext(editableElement, EventTypes.KeyDown, keyArgs);

            ProcessKeyAction(key);
            return false;
        }
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
        lock (_sync)
        {
            ProcessKeyAction(key);
        }
    }

    private void ProcessKeyAction(MikoKey key)
    {
        if (_focusedElement is not ITextEditable editable || !editable.IsEditable) return;
        var element = (Element)_focusedElement;

        switch (key)
        {
            case MikoKey.Backspace:
                if (editable.Backspace())
                    DispatchInputEvent(element, editable);
                break;
            case MikoKey.Delete:
                if (editable.Delete())
                    DispatchInputEvent(element, editable);
                break;
            case MikoKey.Left:
                if (editable.CursorPosition > 0)
                {
                    editable.CursorPosition--;
                    element.IsDirty = true;
                }
                break;
            case MikoKey.Right:
                if (editable.CursorPosition < (editable.Value ?? string.Empty).Length)
                {
                    editable.CursorPosition++;
                    element.IsDirty = true;
                }
                break;
            case MikoKey.Home:
                editable.CursorPosition = 0;
                element.IsDirty = true;
                break;
            case MikoKey.End:
                editable.MoveCursorToEnd();
                element.IsDirty = true;
                break;
            case MikoKey.Enter:
                // 多行控件（textarea）回车插入换行；单行控件不处理（可用于表单提交，由上层处理）。
                if (editable.IsMultiline)
                {
                    editable.InsertText("\n");
                    DispatchInputEvent(element, editable);
                }
                break;
        }
    }

    /// <summary>文本输入（已组合的字符）。控制字符会被忽略。</summary>
    public void OnTextInput(string text)
    {
        lock (_sync)
        {
            if (_focusedElement is not ITextEditable editable || !editable.IsEditable) return;
            var element = (Element)_focusedElement;

            foreach (var character in text)
            {
                if (char.IsControl(character)) continue;
                editable.InsertText(character.ToString());
            }
            DispatchInputEvent(element, editable);
        }
    }

    private void DispatchInputEvent(Element element, ITextEditable editable)
    {
        var inputArgs = new InputEventArgs
        {
            Target = element,
            Data = editable.Value ?? string.Empty,
            Bubbles = true
        };
        DispatchWithSyncContext(element, EventTypes.Input, inputArgs);
    }

    private void DispatchChange(Element element)
    {
        var changeArgs = new ChangeEventArgs
        {
            Target = element,
            Bubbles = true
        };
        DispatchWithSyncContext(element, EventTypes.Change, changeArgs);
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
