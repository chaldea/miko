using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Platform;
using Miko.Styling;

namespace Miko.Components.Reader;

/// <summary>Shared reader chrome and lifecycle. Instance methods and parameter changes are UI-thread APIs.</summary>
public abstract partial class ReaderComponentBase : ComponentBase, IDisposable
{
    [Parameter] public IReaderSource? Source { get; set; }
    /// <summary>Zero-based directory entry. Supports @bind-Index; local text page numbers are separate.</summary>
    [Parameter] public int Index { get; set; }
    [Parameter] public EventCallback<int> IndexChanged { get; set; }
    [Parameter] public EventCallback<Exception> Error { get; set; }
    [Parameter] public bool ShowToolbar { get; set; } = true;
    /// <summary>Start with unobtrusive chapter/progress labels; a tap reveals the overlay controls.</summary>
    [Parameter] public bool Immersive { get; set; } = true;
    [Parameter] public EventCallback BackRequested { get; set; }
    /// <summary>In-app shading, 0–0.75. Does not change the device's system brightness.</summary>
    [Parameter] public float Dimness { get; set; }
    [Parameter] public ReaderTexts Texts { get; set; } = new();
    [Parameter] public string? Class { get; set; }
    [Parameter] public Style? Style { get; set; }
    [Inject] public MikoEngine? Engine { get; set; }
    [Inject] public MikoInteractionController? Input { get; set; }
    [Inject] public TimeProvider? Clock { get; set; }
    [Inject] public IPlatformService? PlatformService { get; set; }

    public ReaderController Controller { get; } = new();
    public ReaderSnapshot Snapshot => Controller.Snapshot;
    public int CurrentIndex => Snapshot.Index;
    public bool IsContentsOpen => _activePanel == ReaderPanel.Contents;
    public bool IsSettingsOpen => _activePanel == ReaderPanel.Settings;
    public bool IsAppearanceOpen => _activePanel == ReaderPanel.Appearance;
    public bool AreControlsVisible { get; private set; }
    private protected readonly ReaderImageView Images = new();
    private protected readonly CanvasElement Viewport = new ReaderCanvas { Class = "miko-reader-viewport" };
    private IReaderSource? _lastSource;
    private ReaderSnapshot? _applied;
    private int _lastIndex, _lastSpread, _directoryStart;
    private int? _reportedIndex;
    private bool _built, _disposed, _pressed, _dragged;
    private float _downX, _downY, _lastX, _lastY, _tapX, _tapY;
    private long? _lastTap;
    private string? _viewError;
    private ITimer? _tapTimer;
    private int _tapGeneration;
    private IDisposable? _backRegistration;
    private protected abstract string ComponentClass { get; }
    private protected virtual int SpreadSize => 1;
    private protected virtual ReaderDirection ReadingDirection => ReaderDirection.LeftToRight;
    private protected virtual ReaderOrientation PageOrientation => ReaderOrientation.Horizontal;
    private protected virtual Color PaperColor => Color.FromHex("#202124");
    private protected virtual Color InkColor => Color.White;
    private protected virtual string PageLabel => "";
    private protected virtual string CurrentPageText => $"{CurrentIndex + 1} / {Snapshot.Document?.Entries.Count ?? 0}";
    private protected virtual float ReadingProgress => Snapshot.Document is { Entries.Count: > 1 } doc ? (float)CurrentIndex / (doc.Entries.Count - 1) : 0;
    private protected virtual bool IsNight => true;
    private protected virtual void ToggleNight() { }
    private protected virtual void BuildAppearance(DivElement panel) { }
    private protected virtual Task SeekAsync(float progress) => GoToAsync((int)Math.Round(progress * Math.Max(0, (Snapshot.Document?.Entries.Count ?? 0) - 1)));
    private protected virtual bool CanPrevious => Snapshot.Index > 0;
    private protected virtual bool CanNext => Snapshot.Document is { } doc && Snapshot.Index + SpreadSize < doc.Entries.Count;

    protected override void OnInitialized()
    {
        Controller.Changed += OnChanged;
        AreControlsVisible = !Immersive;
        BuildChrome();
        _backRegistration = PlatformService?.Back.Register(HandleBack, BackButtonPriority.Overlay);
        Viewport.Paint = Paint;
        Viewport.OnPointerDown = PointerDown;
        Viewport.OnPointerMove = PointerMove;
        Viewport.OnPointerUp = PointerUp;
        Viewport.OnPointerCancel = _ => { _pressed = false; CancelTap(); };
        Viewport.OnWheel = Wheel;
        Viewport.OnKeyDown = KeyDown;
    }

    protected override void OnParametersSet()
    {
        ApplyParameters();
        Dimness = float.IsFinite(Dimness) ? Math.Clamp(Dimness, 0, .75f) : 0;
        ApplyLayout();
        bool changed = !_built || !ReferenceEquals(Source, _lastSource);
        bool indexChanged = _built && Index != _lastIndex;
        bool spreadChanged = _built && SpreadSize != _lastSpread;
        _lastSource = Source; _lastIndex = Index; _lastSpread = SpreadSize; _built = true;
        if (changed) { _reportedIndex = null; _ = Controller.OpenAsync(Source, Math.Max(0, Index), SpreadSize); }
        else if (indexChanged || spreadChanged) _ = Controller.GoToAsync(indexChanged ? Math.Max(0, Index) : CurrentIndex, SpreadSize);
        Refresh();
        BuildPanel();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddElement(0, _root);
    private protected virtual void ApplyParameters() { }
    private protected abstract void SetContent(ReaderSnapshot snapshot);
    private protected abstract void Draw(IDrawingContext context, RectF box);
    private protected abstract void BuildSettings(DivElement panel);
    private protected virtual bool Drag(float dx, float dy) { if (!Images.CanPan) return false; Images.Pan(dx, dy); return true; }
    private protected virtual void DoubleTap() { }
    private protected virtual bool Scroll(float delta) { if (!Images.CanPan) return false; Images.Pan(0, -delta); return true; }

    public virtual Task NextAsync() => Controller.NextAsync();
    public virtual Task PreviousAsync() => Controller.PreviousAsync();
    public virtual Task GoToAsync(int index) => Controller.GoToAsync(index, SpreadSize);
    public Task RetryAsync() { _viewError = null; _applied = null; return Controller.RetryAsync(); }
    public void ToggleContents() { _directoryStart = CurrentIndex / 50 * 50; TogglePanel(ReaderPanel.Contents); }
    public void ToggleSettings() => TogglePanel(ReaderPanel.Settings);
    public void ToggleAppearance() => TogglePanel(ReaderPanel.Appearance);
    public void ShowControls() { if (!ShowToolbar || _disposed) return; CancelTap(); AreControlsVisible = true; ApplyLayout(); UpdateStatus(); Repaint(); }
    public void HideControls() { CancelTap(); _activePanel = ReaderPanel.None; AreControlsVisible = false; ApplyLayout(); UpdateStatus(); Repaint(); }
    public void ToggleControls() { if (AreControlsVisible) HideControls(); else ShowControls(); }
    public void ClosePanel() { _activePanel = ReaderPanel.None; BuildPanel(); ApplyLayout(); UpdateStatus(); Repaint(); }
    private bool HandleBack()
    {
        if (_disposed) return false;
        if (_activePanel != ReaderPanel.None) { ClosePanel(); return true; }
        if (AreControlsVisible) { HideControls(); return true; }
        if (BackRequested.HasDelegate) { _ = BackRequested.InvokeAsync(); return true; }
        return false;
    }

    private void OnChanged()
    {
        if (_disposed) return;
        if (Engine is { } engine) engine.Dispatcher.Post(() => { if (!_disposed) Refresh(); });
        else Refresh();
    }

    private void Refresh()
    {
        var snapshot = Snapshot;
        if (!ReferenceEquals(_applied, snapshot))
        {
            _applied = snapshot;
            _viewError = null;
            SetContent(snapshot);
            if (snapshot.State == ReaderState.Ready && _reportedIndex != snapshot.Index)
            { _reportedIndex = snapshot.Index; _ = IndexChanged.InvokeAsync(snapshot.Index); }
            if (snapshot.Error != null) _ = Error.InvokeAsync(snapshot.Error);
            if (IsContentsOpen) BuildPanel();
        }
        _title.TextContent = snapshot.Document?.Title ?? "";
        _retry.TextContent = Texts.Retry;
        UpdatePosition();
        UpdateStatus();
        Repaint();
    }

    private void UpdatePosition()
    {
        var snapshot = Snapshot;
        int count = snapshot.Document?.Entries.Count ?? 0;
        _position.TextContent = count == 0 ? "0 / 0" : CurrentPageText;
        _chapter.TextContent = count == 0 ? "" : snapshot.Document!.Entries[snapshot.Index].Title;
        _progress.Sync(ReadingProgress);
        _progressText.TextContent = $"{Math.Round(_progress.Value * 100)}%";
        SetDisabled(_previous, snapshot.State != ReaderState.Ready || !CanPrevious);
        SetDisabled(_next, snapshot.State != ReaderState.Ready || !CanNext);
    }

    private void UpdateStatus()
    {
        var snapshot = Snapshot;
        string? message = _viewError ?? snapshot.State switch
        {
            ReaderState.Idle => Texts.NoSource,
            ReaderState.Loading => Texts.Loading,
            ReaderState.Error => snapshot.Error?.Message,
            _ => snapshot.Content.Count == 0 ? Texts.Empty : null,
        };
        _message.TextContent = message ?? "";
        _status.Style!.Display = message == null ? Display.None : Display.Flex;
        _retry.Style!.Display = snapshot.State == ReaderState.Error || _viewError != null ? Display.Block : Display.None;
        Engine?.InvalidateElement(_status);
        Engine?.InvalidateElement(_retry);
    }

    private void Paint(IDrawingContext context, RectF box)
    {
        context.FillRect(box, PaperColor);
        if (Snapshot.State != ReaderState.Ready) return;
        Draw(context, box);
        if (Dimness > 0) context.FillRect(box, Color.FromRgba(0, 0, 0, Dimness));
        if (Images.Error is { } error && _viewError != error)
        {
            _viewError = error;
            UpdateStatus();
            _ = Error.InvokeAsync(new InvalidDataException(error));
            Repaint();
        }
        UpdatePosition();
    }

    private void PointerDown(PointerEventArgs e)
    {
        if (!e.IsPrimary || e.Button != MouseButton.Left) return;
        e.PreventDefault();
        Input?.SetFocus(Viewport);
        _pressed = true; _dragged = false;
        _downX = _lastX = e.X; _downY = _lastY = e.Y;
    }

    private void PointerMove(PointerEventArgs e)
    {
        if (!_pressed) return;
        e.PreventDefault();
        if (Math.Abs(e.X - _downX) + Math.Abs(e.Y - _downY) > 8) { _dragged = true; CancelTap(); }
        if (_dragged) Drag(e.X - _lastX, e.Y - _lastY);
        _lastX = e.X; _lastY = e.Y;
        Repaint();
    }

    private void PointerUp(PointerEventArgs e)
    {
        if (!_pressed) return;
        _pressed = false;
        e.PreventDefault();
        float dx = e.X - _downX, dy = e.Y - _downY;
        if (_dragged || Math.Abs(dx) + Math.Abs(dy) > 8)
        {
            CancelTap();
            if (!Drag(0, 0))
            {
                float distance = PageOrientation == ReaderOrientation.Horizontal ? dx : dy;
                if (Math.Abs(distance) > 40)
                {
                    bool next = distance < 0;
                    if (PageOrientation == ReaderOrientation.Horizontal && ReadingDirection == ReaderDirection.RightToLeft) next = !next;
                    _ = next ? NextAsync() : PreviousAsync();
                }
            }
        }
        else
        {
            var clock = Clock ?? TimeProvider.System;
            long now = clock.GetTimestamp();
            if (_lastTap is { } previous && clock.GetElapsedTime(previous, now).TotalMilliseconds <= 350 && Math.Abs(e.X - _tapX) + Math.Abs(e.Y - _tapY) < 24)
            { CancelTap(); DoubleTap(); }
            else
            {
                CancelTap(); _lastTap = now; _tapX = e.X; _tapY = e.Y;
                int generation = ++_tapGeneration;
                _tapTimer = clock.CreateTimer(_ => Post(() =>
                {
                    if (generation != _tapGeneration) return;
                    CancelTap(); ToggleControls();
                }), null, TimeSpan.FromMilliseconds(350), Timeout.InfiniteTimeSpan);
            }
        }
        Repaint();
    }

    private void Wheel(WheelEventArgs e)
    {
        e.PreventDefault(); e.StopPropagation();
        if (Scroll(e.DeltaY)) { Repaint(); return; }
        // A wheel is continuous only in scroll mode; paged readers use buttons, keys or swipes.
    }

    private void KeyDown(KeyboardEventArgs e)
    {
        if (e.CtrlKey || e.AltKey || e.MetaKey) return;
        if (e.Key == "Escape") { if (HandleBack()) e.PreventDefault(); return; }
        bool? next = e.Key switch
        {
            "PageDown" or "Space" => true, "PageUp" => false,
            "Down" when PageOrientation == ReaderOrientation.Vertical => true,
            "Up" when PageOrientation == ReaderOrientation.Vertical => false,
            "Right" when PageOrientation == ReaderOrientation.Horizontal => ReadingDirection == ReaderDirection.LeftToRight,
            "Left" when PageOrientation == ReaderOrientation.Horizontal => ReadingDirection == ReaderDirection.RightToLeft,
            _ => null,
        };
        if (next.HasValue) { e.PreventDefault(); _ = next.Value ? NextAsync() : PreviousAsync(); }
    }

    private void CancelTap() { ++_tapGeneration; _lastTap = null; _tapTimer?.Dispose(); _tapTimer = null; }
    private void Post(Action action)
    {
        if (Engine is { } engine) engine.Dispatcher.Post(() => { if (!_disposed) action(); });
        else if (!_disposed) action();
    }
    private protected void Repaint() => Engine?.RequestRepaint();
    private static void SetDisabled(Element element, bool disabled)
    {
        if (disabled) element.SetState(ElementState.Disabled);
        else element.ClearState(ElementState.Disabled);
    }
    protected override void OnDispose() => Dispose();
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelTap(); _backRegistration?.Dispose(); _icons.Dispose();
        Controller.Changed -= OnChanged;
        Controller.Dispose(); Images.Dispose();
        SetContent(ReaderSnapshot.Empty);
        if (Viewport.HasState(ElementState.Focus)) Input?.SetFocus(null);
        Viewport.Paint = null;
    }
}

// Miko dispatches physical keys to ITextEditable targets. This canvas consumes navigation keys,
// ignores text input and never requests the soft keyboard (the same keyboard-target pattern as VNC).
internal sealed class ReaderCanvas : CanvasElement, ITextEditable
{
    public string? Value { get => string.Empty; set { } }
    public int CursorPosition { get => 0; set { } }
    public bool IsEditable => true;
    public bool IsMultiline => false;
    public void InsertText(string text) { }
    public bool Backspace() => false;
    public bool Delete() => false;
    public void MoveCursorToEnd() { }
}
