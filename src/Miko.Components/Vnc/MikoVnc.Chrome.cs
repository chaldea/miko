using System.Globalization;
using Miko.Common;
using Miko.Components.Vnc.Protocol;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Graphics;
using Miko.Platform;
using Miko.Styling;

namespace Miko.Components.Vnc;

// The view's elements: the remote screen, the status overlay, the toolbar, the keys bar, the settings panel,
// the connect form and the password dialog.
public sealed partial class MikoVnc
{
    private static readonly Color Accent = Color.FromHex("#3d7bff");
    private static readonly Color BarColor = Color.FromRgba(24, 26, 30, .86f);
    private static readonly Color ChipColor = Color.FromRgba(255, 255, 255, .12f);
    private static readonly Color MutedText = Color.FromRgba(255, 255, 255, .62f);
    private static readonly Color CardText = Color.FromHex("#1f2329");
    private static readonly Color CardMuted = Color.FromHex("#6b7280");
    private static readonly Color ErrorColor = Color.FromHex("#e5484d");
    private static readonly TimeSpan HintDuration = TimeSpan.FromSeconds(2);

    private readonly DivElement _root = new() { Class = "miko-vnc" };
    private readonly VncScreen _screen = new() { Class = "miko-vnc-screen" };
    private readonly DivElement _status = new() { Class = "miko-vnc-status" };
    private readonly CanvasElement _statusIcon = new();
    private readonly SpanElement _statusText = new() { Class = "miko-vnc-status-text" };
    private readonly ButtonElement _statusButton = new() { Class = "miko-vnc-status-button" };
    private readonly DivElement _top = new() { Class = "miko-vnc-top" };
    private readonly DivElement _toolbar = new() { Class = "miko-vnc-toolbar" };
    private readonly SpanElement _title = new() { Class = "miko-vnc-title" };
    private readonly ButtonElement _handle = new() { Class = "miko-vnc-handle" };
    private readonly DivElement _keysBar = new() { Class = "miko-vnc-keys" };
    private readonly DivElement _settings = new() { Class = "miko-vnc-settings" };
    private readonly DivElement _form = new() { Class = "miko-vnc-connect" };
    private readonly InputElement _address = new() { Class = "miko-vnc-address", Type = InputType.Text };
    private readonly InputElement _formPassword = new() { Class = "miko-vnc-form-password", Type = InputType.Password };
    private readonly SpanElement _formError = new() { Class = "miko-vnc-form-error" };
    private readonly DivElement _dialog = new() { Class = "miko-vnc-password" };
    private readonly SpanElement _dialogSubtitle = new();
    private readonly InputElement _dialogInput = new() { Class = "miko-vnc-password-input", Type = InputType.Password };
    private readonly SpanElement _dialogError = new() { Class = "miko-vnc-password-error" };
    private readonly DivElement _hintLayer = new();
    private readonly SpanElement _hint = new() { Class = "miko-vnc-hint" };
    private readonly List<(Element Element, Func<VncTexts, string> Text)> _labels = new();
    private readonly List<(ButtonElement Button, uint Keysym)> _stickyButtons = new();
    private ButtonElement _keyboardButton = null!;
    private Chips _settingsQuality = null!, _formQuality = null!, _scalingChips = null!, _viewOnlyChips = null!;
    private const float CompactWidth = 560;
    private readonly SpanElement _settingsTitle = new() { Class = "miko-vnc-settings-title" };
    private bool _toolbarCollapsed, _keysRequested, _settingsOpen, _compact;
    private IDisposable? _passwordBack, _settingsBack;

    /// <summary>The toolbar is up (not collapsed to its handle).</summary>
    public bool IsToolbarExpanded => !_toolbarCollapsed;

    /// <summary>The keys bar (Esc, Tab, Ctrl, Alt, Win, arrows, F-keys) is shown.</summary>
    public bool IsKeysBarVisible => AcceptsInput && (_keysRequested || (IsTouchPlatform && IsKeyboardActive));

    public bool IsSettingsOpen => _settingsOpen;

    /// <summary>The password dialog is up.</summary>
    public bool IsPasswordDialogOpen => _passwordPrompt != null;

    private void BuildChrome()
    {
        // Remote screen.
        _screen.Style = Fill();
        _screen.Paint = PaintScreen;
        _screen.OnPointerDown = OnScreenPointerDown;
        _screen.OnPointerMove = OnScreenPointerMove;
        _screen.OnPointerUp = OnScreenPointerUp;
        _screen.OnPointerCancel = OnScreenPointerCancel;
        _screen.OnWheel = OnScreenWheel;
        // Clicks are handled from the pointer events; this one must not reach the page.
        _screen.OnClick = e => { e.PreventDefault(); e.StopPropagation(); };
        _screen.OnKeyDown = OnScreenKeyDown;
        _screen.OnKeyUp = OnScreenKeyUp;
        _screen.OnBlur = _ => OnScreenBlur();
        _screen.TextTyped = OnScreenText;
        _root.Children.Add(_screen);

        BuildStatus();
        BuildToolbar();
        BuildKeysBar();
        BuildSettings();
        BuildConnectForm();
        BuildPasswordDialog();

        _hintLayer.Style = new Style
        {
            Display = Display.None, Position = Position.Absolute, Left = Length.Px(0), Right = Length.Px(0), Bottom = Length.Px(72),
            JustifyContent = JustifyContent.Center, PointerEvents = PointerEvents.None, ZIndex = 6,
        };
        _hint.Style = new Style
        {
            Display = Display.Block, MaxWidth = Length.Percent(86), PaddingLeft = Length.Px(14), PaddingRight = Length.Px(14),
            PaddingTop = Length.Px(7), PaddingBottom = Length.Px(7), BorderRadius = 16, BackgroundColor = Color.FromRgba(0, 0, 0, .72f),
            Color = Color.White, FontSize = Length.Px(13), TextAlign = TextAlign.Center,
        };
        _hintLayer.Children.Add(_hint);
        _root.Children.Add(_hintLayer);
        ApplyTexts();
    }

    private void PaintScreen(IDrawingContext context, RectF bounds)
    {
        _view.Paint(context, bounds, _icons);
        // A phone-wide view has no room for the title next to the buttons (the settings panel shows it).
        bool compact = bounds.Width < CompactWidth;
        if (compact != _compact && Engine is { } e)
        {
            _compact = compact;
            e.Dispatcher.Post(() => { if (!_disposed) ApplyState(); });
            e.RequestRepaint();
        }
        if (_pendingActualSize && _view.ViewWidth > 0)
        {
            _pendingActualSize = false;
            ActualSize();
        }
        // The first frame in the tree starts the connection (Engine and layout exist by now).
        if (_autoConnectPending && Engine is { } engine)
        {
            _autoConnectPending = false;
            engine.Dispatcher.Post(() => { if (!_disposed && State != VncState.Connected) Connect(); });
            engine.RequestRepaint();
        }
    }

    // ---- status --------------------------------------------------------------------

    private void BuildStatus()
    {
        _status.Style = new Style
        {
            Display = Display.None, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0), Width = Length.Percent(100),
            Height = Length.Percent(100), FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center,
            JustifyContent = JustifyContent.Center, PointerEvents = PointerEvents.None, ZIndex = 1, FontSize = Length.Px(14),
        };
        _statusIcon.Style = new Style { Display = Display.Block, Width = Length.Px(56), Height = Length.Px(56), FlexShrink = 0 };
        _statusIcon.Paint = PaintStatusIcon;
        _statusText.Style = new Style
        {
            Display = Display.Block, MarginTop = Length.Px(8), PaddingLeft = Length.Px(24), PaddingRight = Length.Px(24),
            TextAlign = TextAlign.Center, Color = Color.FromRgba(255, 255, 255, .9f),
        };
        _statusButton.Style = PillButton(Color.FromRgba(255, 255, 255, .18f), Color.White);
        _statusButton.Style.MarginTop = Length.Px(14);
        _statusButton.Style.PointerEvents = PointerEvents.Auto;
        _statusButton.OnClick = e =>
        {
            e.StopPropagation();
            if (State is VncState.Connecting or VncState.Authenticating) Disconnect();
            else Connect();
        };
        _status.Children.Add(_statusIcon);
        _status.Children.Add(_statusText);
        _status.Children.Add(_statusButton);
        _root.Children.Add(_status);
    }

    private void PaintStatusIcon(IDrawingContext context, RectF bounds)
    {
        float cx = bounds.X + bounds.Width / 2, cy = bounds.Y + bounds.Height / 2;
        if (State is VncState.Connecting or VncState.Authenticating)
        {
            context.StrokeArc(new RectF(cx - 16, cy - 16, 32, 32), (Environment.TickCount64 % 1000) * .36f, 270, Color.White, 3);
            Repaint(); // the spinner turns while connecting, and only then
        }
        else if (State == VncState.Disconnected)
        {
            _icons.Draw(context, cx, cy, ErrorMessage != null ? "alert-circle-outline" : "desktop-outline", 40, Color.FromRgba(255, 255, 255, .85f));
        }
    }

    // ---- toolbar -------------------------------------------------------------------

    private void BuildToolbar()
    {
        // The bar floats centred over the top edge; around it, taps reach the remote screen.
        _top.Style = new Style
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center, Position = Position.Absolute,
            Top = Length.Px(0), Left = Length.Px(0), Width = Length.Percent(100), PointerEvents = PointerEvents.None, ZIndex = 2,
        };
        _toolbar.Style = new Style
        {
            Display = Display.Flex, AlignItems = AlignItems.Center, Height = Length.Px(42), MaxWidth = Length.Percent(100),
            PaddingLeft = Length.Px(10), PaddingRight = Length.Px(2), BackgroundColor = BarColor, Color = Color.White,
            BorderBottomLeftRadius = Length.Px(12), BorderBottomRightRadius = Length.Px(12), PointerEvents = PointerEvents.Auto, BoxSizing = BoxSizing.BorderBox,
        };
        _toolbar.OnClick = e => e.StopPropagation();
        _title.Style = new Style
        {
            Display = Display.Block, FlexShrink = 1, MinWidth = Length.Px(0), MaxWidth = Length.Px(220), MarginRight = Length.Px(6),
            FontSize = Length.Px(12), Color = MutedText, WhiteSpace = WhiteSpace.Nowrap, Overflow = Overflow.Hidden,
            TextOverflow = TextOverflow.Ellipsis,
        };
        _toolbar.Children.Add(_title);

        _keyboardButton = IconButton("miko-vnc-keyboard", () => "keypad-outline", ToggleKeyboard, active: () => IsKeyboardActive);
        _toolbar.Children.Add(_keyboardButton);
        _toolbar.Children.Add(IconButton("miko-vnc-keys-button", () => "key-outline", ToggleKeysBar, active: () => IsKeysBarVisible));
        _toolbar.Children.Add(IconButton("miko-vnc-zoom-out", () => "remove-outline", ZoomOut, enabled: () => _view.Zoom > 1.01f));
        _toolbar.Children.Add(IconButton("miko-vnc-zoom-in", () => "add-outline", ZoomIn, enabled: () => _view.Zoom < VncView.MaxZoom - .01f));
        _toolbar.Children.Add(IconButton("miko-vnc-settings-button", () => "settings-outline", ToggleSettings, active: () => _settingsOpen));
        _toolbar.Children.Add(IconButton("miko-vnc-fullscreen", () => IsFullscreen ? "contract" : "expand", ToggleFullscreen));
        _toolbar.Children.Add(IconButton("miko-vnc-disconnect", () => "power-outline", Disconnect));
        _toolbar.Children.Add(IconButton("miko-vnc-collapse", () => "chevron-up-outline", () => SetToolbarCollapsed(true)));
        _top.Children.Add(_toolbar);

        // Collapsed: a small tab at the top edge brings the bar back.
        _handle.Style = new Style
        {
            Display = Display.None, Position = Position.Relative, Width = Length.Px(56), Height = Length.Px(18), Padding = Length.Px(0),
            BorderWidth = Length.Px(0), BackgroundColor = BarColor, BorderBottomLeftRadius = Length.Px(9), BorderBottomRightRadius = Length.Px(9),
            PointerEvents = PointerEvents.Auto,
        };
        _handle.Children.Add(new CanvasElement { Style = Fill(), Paint = (c, r) => _icons.Draw(c, r, "chevron-down-outline", 14, Color.White) });
        _handle.OnPointerDown = e => { e.PreventDefault(); e.StopPropagation(); };
        _handle.OnClick = e => { e.StopPropagation(); SetToolbarCollapsed(false); };
        _top.Children.Add(_handle);
        _root.Children.Add(_top);
    }

    private void SetToolbarCollapsed(bool collapsed)
    {
        _toolbarCollapsed = collapsed;
        if (collapsed) CloseSettings();
        ApplyState();
        Repaint();
    }

    private void ToggleKeysBar()
    {
        _keysRequested = !IsKeysBarVisible;
        ApplyState();
    }

    private void UpdateToolbarText()
    {
        if (Desktop is not { } d) { _title.TextContent = ""; _settingsTitle.TextContent = ""; return; }
        var text = $"{d.Name}  {d.Width.ToString(CultureInfo.InvariantCulture)}×{d.Height.ToString(CultureInfo.InvariantCulture)}";
        if (State == VncState.Connected) text += " · " + FormatRate(BytesPerSecond);
        if (_title.TextContent != text) _title.TextContent = text;
        if (_settingsTitle.TextContent != text) _settingsTitle.TextContent = text;
    }

    // An icon button that never takes focus from the remote screen (the soft keyboard stays up).
    private ButtonElement IconButton(string className, Func<string> icon, Action action, Func<bool>? active = null, Func<bool>? enabled = null)
    {
        var button = new ButtonElement
        {
            Class = className,
            Style = new Style
            {
                Display = Display.Block, Position = Position.Relative, Width = Length.Px(38), Height = Length.Px(42), FlexShrink = 0,
                Padding = Length.Px(0), BorderWidth = Length.Px(0), BackgroundColor = Color.Transparent,
            },
        };
        button.Children.Add(new CanvasElement
        {
            Style = Fill(),
            Paint = (c, r) =>
            {
                bool on = enabled?.Invoke() ?? true;
                var color = !on ? Color.FromRgba(255, 255, 255, .35f) : active?.Invoke() == true ? Accent : Color.White;
                _icons.Draw(c, r, icon(), 20, color);
            },
        });
        button.OnPointerDown = e => { e.PreventDefault(); e.StopPropagation(); };
        button.OnClick = e =>
        {
            e.StopPropagation();
            if (enabled?.Invoke() == false) return;
            action();
            Repaint();
        };
        return button;
    }

    // ---- keys bar ------------------------------------------------------------------

    private void BuildKeysBar()
    {
        _keysBar.Style = new Style
        {
            Display = Display.None, Position = Position.Absolute, Left = Length.Px(0), Right = Length.Px(0), Bottom = Length.Px(0),
            Height = Length.Px(46), AlignItems = AlignItems.Center, PaddingLeft = Length.Px(4), PaddingRight = Length.Px(4),
            BackgroundColor = BarColor, OverflowX = Overflow.Auto, WhiteSpace = WhiteSpace.Nowrap, ZIndex = 2, BoxSizing = BoxSizing.BorderBox,
        };
        _keysBar.OnClick = e => e.StopPropagation();
        AddKey("Esc", () => TapKey(VncKeys.Escape));
        AddKey("Tab", () => TapKey(VncKeys.Tab));
        AddSticky("Ctrl", VncKeys.ControlLeft);
        AddSticky("Alt", VncKeys.AltLeft);
        AddSticky("Shift", VncKeys.ShiftLeft);
        AddSticky("Win", VncKeys.SuperLeft);
        AddKey("←", () => TapKey(VncKeys.Left));
        AddKey("↑", () => TapKey(VncKeys.Up));
        AddKey("↓", () => TapKey(VncKeys.Down));
        AddKey("→", () => TapKey(VncKeys.Right));
        AddKey("Del", () => TapKey(VncKeys.Delete));
        AddKey("Home", () => TapKey(VncKeys.Home));
        AddKey("End", () => TapKey(VncKeys.End));
        AddKey("PgUp", () => TapKey(VncKeys.PageUp));
        AddKey("PgDn", () => TapKey(VncKeys.PageDown));
        AddKey("Ctrl+Alt+Del", SendCtrlAltDel);
        for (int i = 0; i < 12; i++)
        {
            uint keysym = VncKeys.F1 + (uint)i;
            AddKey("F" + (i + 1).ToString(CultureInfo.InvariantCulture), () => TapKey(keysym));
        }
        _root.Children.Add(_keysBar);
    }

    private ButtonElement AddKey(string label, Action action)
    {
        var button = new ButtonElement { Class = "miko-vnc-key", TextContent = label, Style = KeyStyle() };
        button.OnPointerDown = e => { e.PreventDefault(); e.StopPropagation(); };
        button.OnClick = e =>
        {
            e.StopPropagation();
            action();
            Repaint();
        };
        _keysBar.Children.Add(button);
        return button;
    }

    private void AddSticky(string label, uint keysym)
    {
        var button = AddKey(label, () => ToggleSticky(keysym));
        _stickyButtons.Add((button, keysym));
    }

    private static Style KeyStyle() => new()
    {
        Display = Display.InlineBlock, Height = Length.Px(34), MinWidth = Length.Px(42), MarginLeft = Length.Px(3), MarginRight = Length.Px(3),
        PaddingLeft = Length.Px(10), PaddingRight = Length.Px(10), BorderWidth = Length.Px(0), BorderRadius = 7, BackgroundColor = ChipColor,
        Color = Color.White, FontSize = Length.Px(13), FlexShrink = 0,
    };

    private void UpdateKeysBar()
    {
        foreach (var (button, keysym) in _stickyButtons)
        {
            bool on = _sticky.Contains(keysym);
            ChangeStyle(button, s => s.BackgroundColor = on ? Accent : ChipColor);
        }
        Repaint();
    }

    // ---- settings ------------------------------------------------------------------

    private void BuildSettings()
    {
        _settings.Style = new Style
        {
            Display = Display.None, Position = Position.Absolute, Top = Length.Px(48), Right = Length.Px(8), Width = Length.Px(280),
            MaxWidth = Length.Percent(92), FlexDirection = FlexDirection.Column, Padding = Length.Px(12), BorderRadius = 12,
            BackgroundColor = Color.FromRgba(24, 26, 30, .94f), Color = Color.White, FontSize = Length.Px(13), ZIndex = 3,
            BoxSizing = BoxSizing.BorderBox,
        };
        _settings.OnClick = e => e.StopPropagation();
        _settings.OnPointerDown = e => e.StopPropagation();
        _settingsTitle.Style = new Style
        {
            Display = Display.Block, MarginBottom = Length.Px(4), FontSize = Length.Px(12), Color = MutedText, WhiteSpace = WhiteSpace.Nowrap,
            Overflow = Overflow.Hidden, TextOverflow = TextOverflow.Ellipsis,
        };
        _settings.Children.Add(_settingsTitle);

        _settingsQuality = new Chips(
            [t => t.Lossless, t => t.High, t => t.Balanced, t => t.Low],
            i => SetQuality((VncQuality)i), () => (int)CurrentQuality, dark: true);
        AddSettingsRow(t => t.Quality, _settingsQuality.Element);
        _scalingChips = new Chips([t => t.Fit, t => t.ActualSize],
            i => { if (i == 0) FitToScreen(); else ActualSize(); UpdateSettings(); },
            () => Math.Abs(_view.Zoom - 1) < .01f ? 0 : Math.Abs(_view.Zoom - _view.ActualSizeZoom) < .01f ? 1 : -1, dark: true);
        AddSettingsRow(t => t.Scaling, _scalingChips.Element);
        _viewOnlyChips = new Chips([t => t.Off, t => t.On], i => SetViewOnly(i == 1), () => IsViewOnly ? 1 : 0, dark: true);
        AddSettingsRow(t => t.ViewOnly, _viewOnlyChips.Element);

        var refresh = new ButtonElement { Class = "miko-vnc-refresh", Style = PillButton(ChipColor, Color.White) };
        refresh.Style.MarginTop = Length.Px(10);
        refresh.Style.AlignSelf = AlignSelf.FlexStart;
        refresh.OnClick = e => { e.StopPropagation(); RefreshScreen(); };
        _labels.Add((refresh, t => t.Refresh));
        _settings.Children.Add(refresh);
        _root.Children.Add(_settings);
    }

    private void AddSettingsRow(Func<VncTexts, string> label, Element control)
    {
        var caption = new SpanElement
        {
            Style = new Style { Display = Display.Block, MarginTop = Length.Px(6), MarginBottom = Length.Px(6), Color = MutedText, FontSize = Length.Px(12) },
        };
        _labels.Add((caption, label));
        _settings.Children.Add(caption);
        _settings.Children.Add(control);
    }

    private void ToggleSettings()
    {
        if (_settingsOpen) CloseSettings();
        else OpenSettings();
    }

    private void OpenSettings()
    {
        if (_settingsOpen || State != VncState.Connected) return;
        _settingsOpen = true;
        UpdateSettings();
        _settingsBack = PlatformService?.Back.Register(() =>
        {
            if (_disposed || !_settingsOpen) return false;
            CloseSettings();
            return true;
        }, BackButtonPriority.Overlay);
        ApplyState();
    }

    private void CloseSettings()
    {
        if (!_settingsOpen) return;
        _settingsOpen = false;
        _settingsBack?.Dispose();
        _settingsBack = null;
        ApplyState();
        Repaint();
    }

    private void UpdateSettings()
    {
        _settingsQuality?.Update(Texts);
        _formQuality?.Update(Texts);
        _scalingChips?.Update(Texts);
        _viewOnlyChips?.Update(Texts);
        Repaint();
    }

    // ---- connect form --------------------------------------------------------------

    private void BuildConnectForm()
    {
        _form.Style = Backdrop(Color.FromHex("#101216"));
        var card = Card();
        card.Children.Add(Label(t => t.ConnectTitle, new Style { Display = Display.Block, FontSize = Length.Px(20), FontWeight = FontWeight.Bold, Color = CardText }));
        card.Children.Add(Label(t => t.ConnectDescription, new Style { Display = Display.Block, MarginTop = Length.Px(4), MarginBottom = Length.Px(14), FontSize = Length.Px(13), Color = CardMuted }));
        card.Children.Add(Label(t => t.Address, FieldLabel()));
        _address.Style = Field();
        _address.OnKeyDown = e => { if (e.Code == MikoKey.Enter) { e.PreventDefault(); SubmitConnectForm(); } };
        card.Children.Add(_address);
        card.Children.Add(Label(t => t.Password, FieldLabel()));
        _formPassword.Style = Field();
        _formPassword.OnKeyDown = e => { if (e.Code == MikoKey.Enter) { e.PreventDefault(); SubmitConnectForm(); } };
        card.Children.Add(_formPassword);
        card.Children.Add(Label(t => t.Quality, FieldLabel()));
        _formQuality = new Chips([t => t.Lossless, t => t.High, t => t.Balanced, t => t.Low],
            i => SetQuality((VncQuality)i), () => (int)CurrentQuality, dark: false);
        card.Children.Add(_formQuality.Element);
        _formError.Style = new Style { Display = Display.None, MarginTop = Length.Px(12), FontSize = Length.Px(13), Color = ErrorColor };
        card.Children.Add(_formError);
        var connect = new ButtonElement { Class = "miko-vnc-connect-button", Style = PillButton(Accent, Color.White) };
        connect.Style.MarginTop = Length.Px(18);
        connect.Style.Height = Length.Px(42);
        connect.Style.JustifyContent = JustifyContent.Center;
        connect.OnClick = e => { e.StopPropagation(); SubmitConnectForm(); };
        _labels.Add((connect, t => t.Connect));
        card.Children.Add(connect);
        _form.Children.Add(card);
        _root.Children.Add(_form);
    }

    private void SubmitConnectForm()
    {
        if (!VncAddress.TryParse(_address.Value, out var host, out var port))
        {
            _formError.TextContent = Texts.InvalidAddress;
            ChangeStyle(_formError, s => s.Display = Display.Block);
            Repaint();
            return;
        }
        string? password = string.IsNullOrEmpty(_formPassword.Value) ? null : _formPassword.Value;
        Input?.SetFocus(null);
        Connect(host, port, password);
    }

    // ---- password dialog -----------------------------------------------------------

    private void BuildPasswordDialog()
    {
        _dialog.Style = Backdrop(Color.FromRgba(0, 0, 0, .55f));
        _dialog.Style.ZIndex = 5;
        var card = Card();
        card.Children.Add(new CanvasElement
        {
            Style = new Style { Display = Display.Block, Width = Length.Px(40), Height = Length.Px(40), MarginBottom = Length.Px(8) },
            Paint = (c, r) => _icons.Draw(c, r, "lock-closed-outline", 30, Accent),
        });
        card.Children.Add(Label(t => t.PasswordTitle, new Style { Display = Display.Block, FontSize = Length.Px(18), FontWeight = FontWeight.Bold, Color = CardText }));
        _dialogSubtitle.Style = new Style { Display = Display.Block, MarginTop = Length.Px(4), MarginBottom = Length.Px(14), FontSize = Length.Px(13), Color = CardMuted };
        card.Children.Add(_dialogSubtitle);
        _dialogInput.Style = Field();
        _dialogInput.OnKeyDown = e => { if (e.Code == MikoKey.Enter) { e.PreventDefault(); SubmitPassword(); } };
        card.Children.Add(_dialogInput);
        _dialogError.Style = new Style { Display = Display.None, MarginTop = Length.Px(10), FontSize = Length.Px(13), Color = ErrorColor };
        card.Children.Add(_dialogError);

        var actions = new DivElement { Style = new Style { Display = Display.Flex, JustifyContent = JustifyContent.FlexEnd, MarginTop = Length.Px(18) } };
        var cancel = new ButtonElement { Class = "miko-vnc-password-cancel", Style = PillButton(Color.FromHex("#eef0f3"), CardText) };
        cancel.OnClick = e => { e.StopPropagation(); CancelPassword(); };
        _labels.Add((cancel, t => t.Cancel));
        var ok = new ButtonElement { Class = "miko-vnc-password-ok", Style = PillButton(Accent, Color.White) };
        ok.Style.MarginLeft = Length.Px(10);
        ok.OnClick = e => { e.StopPropagation(); SubmitPassword(); };
        _labels.Add((ok, t => t.OK));
        actions.Children.Add(cancel);
        actions.Children.Add(ok);
        card.Children.Add(actions);
        _dialog.Children.Add(card);
        _root.Children.Add(_dialog);
    }

    private void OpenPasswordDialog(string server, string? error)
    {
        _dialogSubtitle.TextContent = string.Format(CultureInfo.CurrentCulture, Texts.PasswordPrompt, server);
        _dialogError.TextContent = error ?? "";
        ChangeStyle(_dialogError, s => s.Display = error != null ? Display.Block : Display.None);
        _dialogInput.Value = "";
        _dialogInput.CursorPosition = 0;
        _passwordBack ??= PlatformService?.Back.Register(() =>
        {
            if (_disposed || _passwordPrompt == null) return false;
            CancelPassword();
            return true;
        }, BackButtonPriority.Overlay);
        ApplyState();
        // Ready to type: focus the field (a phone brings up its keyboard).
        if (Input is { } input)
        {
            input.SetFocus(_dialogInput);
            input.RequestInputMethodShow();
        }
        Repaint();
    }

    private void SubmitPassword()
    {
        if (_passwordPrompt is not { } prompt) return;
        string password = _dialogInput.Value ?? "";
        _passwordPrompt = null;
        ClosePasswordDialog();
        SetState(VncState.Connecting, null);
        prompt.TrySetResult(password);
    }

    private void CancelPassword()
    {
        if (_passwordPrompt is not { } prompt) return;
        _passwordPrompt = null;
        ClosePasswordDialog();
        prompt.TrySetResult(null);
    }

    private void ClosePasswordDialog()
    {
        _passwordBack?.Dispose();
        _passwordBack = null;
        _dialogInput.Value = "";
        if (Input is { } input && _dialogInput.HasState(ElementState.Focus)) input.SetFocus(null);
        ApplyState();
    }

    // ---- hint ----------------------------------------------------------------------

    private void ShowHint(string text, TimeSpan? duration = null)
    {
        if (_disposed) return;
        Hint = text;
        _hint.TextContent = text;
        ChangeStyle(_hintLayer, s => s.Display = Display.Flex);
        if (_hintTimer == null)
        {
            var clock = Clock ?? TimeProvider.System;
            using (ExecutionContext.SuppressFlow())
                _hintTimer = clock.CreateTimer(static s => { var v = (MikoVnc)s!; v.PostToRenderThread(v.HideHint); }, this,
                    Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }
        _hintTimer.Change(duration ?? HintDuration, Timeout.InfiniteTimeSpan);
        Repaint();
    }

    private void HideHint()
    {
        if (Hint == null) return;
        Hint = null;
        ChangeStyle(_hintLayer, s => s.Display = Display.None);
        Repaint();
    }

    // ---- fullscreen ----------------------------------------------------------------

    /// <summary>
    /// Fills the screen with the remote desktop. On a phone: landscape, system bars hidden, back leaves it.
    /// On the desktop Escape stays a key for the remote side; the toolbar button leaves fullscreen.
    /// </summary>
    public void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        if (IsFullscreen) AcquireFullscreenPlatform();
        else ReleaseFullscreenPlatform();
        ApplyLayout();
        Repaint();
        FullscreenChanged?.Invoke();
    }

    private void AcquireFullscreenPlatform()
    {
        if (_fullscreenPlatform is not null || PlatformService is not { } platform) return;
        _fullscreenPlatform = platform;
        platform.Orientation.Lock(ScreenOrientation.Landscape);
        platform.Orientation.KeepAwake();
        platform.SystemBars.Hide();
        if (IsTouchPlatform)
            _fullscreenBack = platform.Back.Register(() =>
            {
                if (_disposed || !IsFullscreen) return false;
                ToggleFullscreen();
                return true;
            }, BackButtonPriority.Overlay);
    }

    private void ReleaseFullscreenPlatform()
    {
        if (_fullscreenPlatform is not { } platform) return;
        _fullscreenPlatform = null;
        _fullscreenBack?.Dispose();
        _fullscreenBack = null;
        platform.Orientation.Unlock();
        platform.Orientation.AllowSleep();
        platform.SystemBars.Show();
    }

    // ---- state → elements ----------------------------------------------------------

    private void ApplyLayout()
    {
        var root = new Style
        {
            Display = Display.Block, Position = Position.Relative, Width = Length.Percent(100), Height = Length.Percent(100),
            MinHeight = Length.Px(0), Overflow = Overflow.Hidden, BackgroundColor = Color.Black, Color = Color.White,
            FontSize = Length.Px(14), BoxSizing = BoxSizing.BorderBox,
        };
        if (Style != null) root.Merge(Style);
        if (IsFullscreen)
        {
            root.Position = Position.Fixed;
            root.Top = Length.Px(0);
            root.Left = Length.Px(0);
            root.Width = Length.Vw(100);
            root.Height = Length.Vh(100);
            root.Margin = Length.Px(0);
            root.ZIndex = 10000;
        }
        _root.Style = root;
        // Fullscreen covers the cutout and the home indicator: keep the bars clear of them.
        ChangeStyle(_top, s => s.PaddingTop = IsFullscreen ? Length.SafeAreaInsetTop : Length.Px(0));
        ChangeStyle(_keysBar, s =>
        {
            s.PaddingBottom = IsFullscreen ? Length.SafeAreaInsetBottom : Length.Px(0);
            s.Height = IsFullscreen ? Length.SafeAreaInsetBottom + Length.Px(46) : Length.Px(46);
        });
    }

    private void ApplyState()
    {
        if (!_built && State == VncState.Idle && _root.Children.Count == 0) return;
        bool connected = State == VncState.Connected;
        bool form = Host == null && State is VncState.Idle or VncState.Disconnected;
        bool status = !form && State is VncState.Connecting or VncState.Disconnected;

        ChangeStyle(_status, s =>
        {
            s.Display = status ? Display.Flex : Display.None;
            s.BackgroundColor = State == VncState.Disconnected && _view.Framebuffer == null ? Color.FromHex("#101216") : Color.Transparent;
        });
        _statusText.TextContent = State switch
        {
            VncState.Connecting => string.Format(CultureInfo.CurrentCulture, Texts.Connecting, VncAddress.Format(_targetHost ?? "", _targetPort)),
            VncState.Disconnected => ErrorMessage ?? Texts.Disconnected,
            _ => "",
        };
        _statusButton.TextContent = State is VncState.Connecting or VncState.Authenticating ? Texts.Cancel
            : State == VncState.Disconnected ? Texts.Reconnect : Texts.Connect;

        ChangeStyle(_form, s => s.Display = form ? Display.Flex : Display.None);
        if (form)
        {
            if (string.IsNullOrEmpty(_address.Value) && _targetHost != null) _address.Value = VncAddress.Format(_targetHost, _targetPort);
            _formError.TextContent = ErrorMessage ?? "";
            ChangeStyle(_formError, s => s.Display = ErrorMessage != null ? Display.Block : Display.None);
        }
        ChangeStyle(_dialog, s => s.Display = _passwordPrompt != null ? Display.Flex : Display.None);

        bool bar = connected && ShowToolbar;
        ChangeStyle(_toolbar, s => s.Display = bar && !_toolbarCollapsed ? Display.Flex : Display.None);
        ChangeStyle(_handle, s => s.Display = bar && _toolbarCollapsed ? Display.Block : Display.None);
        ChangeStyle(_keyboardButton, s => s.Display = IsTouchPlatform && !IsViewOnly ? Display.Block : Display.None);
        ChangeStyle(_title, s => s.Display = _compact ? Display.None : Display.Block);
        ChangeStyle(_toolbar, s => s.PaddingLeft = Length.Px(_compact ? 2 : 10));
        ChangeStyle(_settingsTitle, s => s.Display = _compact ? Display.Block : Display.None);
        ChangeStyle(_keysBar, s => s.Display = connected && IsKeysBarVisible ? Display.Flex : Display.None);
        ChangeStyle(_settings, s => s.Display = connected && _settingsOpen && !_toolbarCollapsed ? Display.Flex : Display.None);
        UpdateToolbarText();
        ApplyScreenCursor();
    }

    // With a mouse the remote pointer is drawn where the local one is: hide the system cursor over the screen.
    private void ApplyScreenCursor()
    {
        bool own = State == VncState.Connected && _view.HasCursorShape && !IsTouchPlatform;
        var cursor = own ? Cursor.None : Cursor.Default;
        if (_screen.Style?.Cursor?.Value is { } current && current == cursor) return;
        ChangeStyle(_screen, s => s.Cursor = cursor);
    }

    private void ApplyTexts()
    {
        foreach (var (element, text) in _labels) element.TextContent = text(Texts);
        _address.Placeholder = Texts.AddressPlaceholder;
        _formPassword.Placeholder = Texts.PasswordPlaceholder;
        _dialogInput.Placeholder = Texts.Password;
        UpdateSettings();
        if (_built) ApplyState();
    }

    // ---- element helpers -------------------------------------------------------------

    private SpanElement Label(Func<VncTexts, string> text, Style style)
    {
        var label = new SpanElement { Style = style };
        _labels.Add((label, text));
        return label;
    }

    private static Style FieldLabel() => new()
    {
        Display = Display.Block, MarginTop = Length.Px(12), MarginBottom = Length.Px(6), FontSize = Length.Px(12), Color = CardMuted,
    };

    private static Style Field() => new()
    {
        Display = Display.Block, Width = Length.Percent(100), Height = Length.Px(40), PaddingLeft = Length.Px(12), PaddingRight = Length.Px(12),
        BorderWidth = Length.Px(1), BorderStyle = BorderStyle.Solid, BorderColor = Color.FromHex("#d5d9e0"), BorderRadius = 8,
        BackgroundColor = Color.White, Color = CardText, FontSize = Length.Px(15), BoxSizing = BoxSizing.BorderBox,
    };

    private static Style Backdrop(Color color) => new()
    {
        Display = Display.None, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0), Width = Length.Percent(100),
        Height = Length.Percent(100), AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center, BackgroundColor = color,
        ZIndex = 4, PaddingLeft = Length.Px(16), PaddingRight = Length.Px(16), BoxSizing = BoxSizing.BorderBox,
    };

    private static DivElement Card() => new()
    {
        Style = new Style
        {
            Display = Display.Flex, FlexDirection = FlexDirection.Column, Width = Length.Px(340), MaxWidth = Length.Percent(100), Padding = Length.Px(22),
            BorderRadius = 14, BackgroundColor = Color.White, Color = CardText, BoxSizing = BoxSizing.BorderBox,
        },
    };

    private static Style PillButton(Color background, Color text) => new()
    {
        Display = Display.Flex, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center, Height = Length.Px(34),
        PaddingLeft = Length.Px(18), PaddingRight = Length.Px(18), BorderWidth = Length.Px(0), BorderRadius = 17,
        BackgroundColor = background, Color = text, FontSize = Length.Px(14),
    };

    private static Style Fill() => new()
    {
        Display = Display.Block, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0),
        Width = Length.Percent(100), Height = Length.Percent(100),
    };

    private static void ChangeStyle(Element element, Action<Style> update)
    {
        var style = new Style();
        if (element.Style != null) style.Merge(element.Style);
        update(style);
        element.Style = style;
    }

    /// <summary>A row of mutually exclusive options (segmented control).</summary>
    private sealed class Chips
    {
        private readonly List<(ButtonElement Button, Func<VncTexts, string> Text)> _items = new();
        private readonly Func<int> _selected;
        private readonly bool _dark;

        public Chips(IReadOnlyList<Func<VncTexts, string>> labels, Action<int> select, Func<int> selected, bool dark)
        {
            _selected = selected;
            _dark = dark;
            Element = new DivElement { Style = new Style { Display = Display.Flex, FlexWrap = FlexWrap.Wrap } };
            for (int i = 0; i < labels.Count; i++)
            {
                int index = i;
                var button = new ButtonElement { Class = "miko-vnc-chip" };
                button.OnPointerDown = e => { e.PreventDefault(); e.StopPropagation(); };
                button.OnClick = e => { e.StopPropagation(); select(index); };
                _items.Add((button, labels[i]));
                Element.Children.Add(button);
            }
        }

        public DivElement Element { get; }

        public void Update(VncTexts texts)
        {
            int selected = _selected();
            for (int i = 0; i < _items.Count; i++)
            {
                var (button, text) = _items[i];
                button.TextContent = text(texts);
                bool on = i == selected;
                button.Style = new Style
                {
                    Display = Display.Block, Height = Length.Px(30), PaddingLeft = Length.Px(12), PaddingRight = Length.Px(12),
                    MarginRight = Length.Px(6), MarginBottom = Length.Px(4), BorderWidth = Length.Px(0), BorderRadius = 15, FontSize = Length.Px(13),
                    BackgroundColor = on ? Accent : _dark ? ChipColor : Color.FromHex("#eef0f3"),
                    Color = on ? Color.White : _dark ? Color.White : CardText,
                };
            }
        }
    }
}
