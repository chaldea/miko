using System.Globalization;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Styling;

namespace Miko.Components.Reader;

public abstract partial class ReaderComponentBase
{
    private enum ReaderPanel { None, Contents, Appearance, Settings }
    private ReaderPanel _activePanel;
    private readonly ReaderIcons _icons = new();
    private DivElement _root = new();
    private readonly DivElement _stage = new(), _top = new(), _toolbar = new(), _navigation = new(), _actions = new();
    private readonly SpanElement _title = new() { Class = "miko-reader-title" };
    private readonly SpanElement _chapter = new() { Class = "miko-reader-chapter" };
    private readonly SpanElement _position = new() { Class = "miko-reader-position" };
    private readonly SpanElement _progressText = new() { Class = "miko-reader-progress-text" };
    private readonly DivElement _status = new() { Class = "miko-reader-status" };
    private readonly SpanElement _message = new();
    private readonly DivElement _scrim = new() { Class = "miko-reader-scrim" };
    private readonly DivElement _panel = new() { Class = "miko-reader-panel" };
    private readonly DivElement _panelBody = new() { Class = "miko-reader-panel-body" };
    private ButtonElement _previous = null!, _next = null!, _retry = null!;
    private ReaderSlider _progress = null!;
    private readonly List<(ButtonElement Button, string Icon, Func<string> Text, Func<bool> Active)> _tools = [];
    private protected Color PanelColor => IsNight ? Color.FromHex("#25272b") : Color.FromHex("#fbfaf7");
    private protected Color PanelInk => IsNight ? Color.FromHex("#e2e4e8") : Color.FromHex("#313832");
    private protected Color SubtleInk => IsNight ? Color.FromHex("#a4a7ac") : Color.FromHex("#858b84");
    private protected Color Accent => IsNight ? Color.FromHex("#b4d5b9") : Color.FromHex("#557961");
    private protected Color ControlColor => IsNight ? Color.FromHex("#36393e") : Color.FromHex("#efefea");

    private void BuildChrome()
    {
        _top.Class = "miko-reader-top"; _toolbar.Class = "miko-reader-toolbar";
        _progress = new("miko-reader-progress", () => Accent, () => ControlColor,
            value => _progressText.TextContent = $"{Math.Round(value * 100)}%", value => _ = SeekAsync(value), Repaint);
        _previous = IconButton("chevron-back-outline", "miko-reader-previous", () => _ = PreviousAsync());
        _next = IconButton("chevron-forward-outline", "miko-reader-next", () => _ = NextAsync());
        _retry = Button(Texts.Retry, "miko-reader-retry", () => _ = RetryAsync());
        var back = IconButton("arrow-back-outline", "miko-reader-back", () => { if (BackRequested.HasDelegate) _ = BackRequested.InvokeAsync(); else HideControls(); });
        var hide = IconButton("close-outline", "miko-reader-hide", HideControls);
        _top.Children.AddRange([back, _title, hide]);
        _navigation.Children.AddRange([_previous, _progress.Element, _progressText, _next]);
        AddTool("list-outline", "miko-reader-contents", () => Texts.Contents, () => IsContentsOpen, ToggleContents);
        AddTool("sunny-outline", "miko-reader-appearance", () => Texts.Appearance, () => IsAppearanceOpen, ToggleAppearance);
        AddTool("moon-outline", "miko-reader-night", () => IsNight ? Texts.Light : Texts.Dark, () => IsNight, () => { ToggleNight(); SettingsChanged(); });
        AddTool("options-outline", "miko-reader-settings", () => Texts.Settings, () => IsSettingsOpen, ToggleSettings);
        _toolbar.Children.AddRange([_navigation, _actions]);
        _status.Children.AddRange([_message, _retry]);
        _stage.Children.Add(Viewport);
        _root.Children.AddRange([_stage, _chapter, _position, _status, _top, _scrim, _panel, _toolbar]);
        _scrim.OnClick = e => { e.StopPropagation(); ClosePanel(); };
        _scrim.OnPointerDown = e => e.PreventDefault();
        _panel.OnClick = e => e.StopPropagation();
    }

    private void AddTool(string icon, string name, Func<string> text, Func<bool> active, Action action)
    {
        var button = Button("", name, action);
        _tools.Add((button, icon, text, active));
        _actions.Children.Add(button);
    }

    private void TogglePanel(ReaderPanel panel)
    {
        CancelTap();
        AreControlsVisible = true;
        _activePanel = _activePanel == panel ? ReaderPanel.None : panel;
        ApplyLayout(); BuildPanel(); UpdateStatus(); Repaint();
    }

    private void ApplyLayout()
    {
        // A parent changing its sole child can adopt this component's children into its retained root.
        // Follow that live parent before changing root styles (theme, size, safe-area background).
        if (_stage.Parent is DivElement liveRoot) _root = liveRoot;
        bool chrome = ShowToolbar && AreControlsVisible;
        bool panel = _activePanel != ReaderPanel.None;
        _root.Class = $"miko-reader {ComponentClass} {Class}";
        var root = new Style();
        if (Style != null) root.Merge(Style);
        root.Merge(new Style { Display = Display.Flex, FlexDirection = FlexDirection.Column, Position = Position.Relative, Width = Length.Percent(100), Height = Length.Px(600), MinHeight = Length.Px(0), BackgroundColor = PaperColor, Color = InkColor, FontSize = Length.Px(14), Overflow = Overflow.Hidden });
        _root.Style = root;
        _stage.Style = new() { Display = Display.Block, Position = Position.Relative, FlexGrow = 1, Height = Length.Px(0), MinHeight = Length.Px(0), Overflow = Overflow.Hidden };
        // Overlay controls never resize the reading surface, so opening settings cannot repaginate the book.
        Viewport.Style = new() { Display = Display.Block, Position = Position.Absolute, Top = Length.SafeAreaInsetTop + Length.Px(28), Bottom = Length.SafeAreaInsetBottom + Length.Px(24), Left = Length.SafeAreaInsetLeft, Right = Length.SafeAreaInsetRight };
        _chapter.Style = new() { Position = Position.Absolute, Top = Length.SafeAreaInsetTop + Length.Px(12), Left = Length.SafeAreaInsetLeft + Length.Px(24), Right = Length.SafeAreaInsetRight + Length.Px(24), FontSize = Length.Px(11), Color = SubtleInk, WhiteSpace = WhiteSpace.Nowrap, Overflow = Overflow.Hidden, PointerEvents = PointerEvents.None };
        _position.Style = new() { Position = Position.Absolute, Bottom = Length.SafeAreaInsetBottom + Length.Px(10), Right = Length.SafeAreaInsetRight + Length.Px(24), FontSize = Length.Px(11), Color = SubtleInk, PointerEvents = PointerEvents.None };
        _top.Style = new() { Display = chrome && !panel ? Display.Flex : Display.None, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0), Right = Length.Px(0), Height = Length.SafeAreaInsetTop + Length.Px(56), PaddingTop = Length.SafeAreaInsetTop, PaddingLeft = Length.SafeAreaInsetLeft + Length.Px(8), PaddingRight = Length.SafeAreaInsetRight + Length.Px(8), AlignItems = AlignItems.Center, BackgroundColor = PanelColor, Color = PanelInk, BoxSizing = BoxSizing.BorderBox };
        _title.Style = new() { FlexGrow = 1, FlexShrink = 1, MinWidth = Length.Px(0), FontSize = Length.Px(15), FontWeight = FontWeight.Medium, TextAlign = TextAlign.Center, WhiteSpace = WhiteSpace.Nowrap, Overflow = Overflow.Hidden };
        _toolbar.Style = new() { Display = chrome || panel ? Display.Flex : Display.None, Position = Position.Absolute, Bottom = Length.Px(0), Left = Length.Px(0), Right = Length.Px(0), Height = Length.SafeAreaInsetBottom + Length.Px(126), PaddingBottom = Length.SafeAreaInsetBottom, PaddingLeft = Length.SafeAreaInsetLeft + Length.Px(12), PaddingRight = Length.SafeAreaInsetRight + Length.Px(12), FlexDirection = FlexDirection.Column, BackgroundColor = PanelColor, Color = PanelInk, BoxSizing = BoxSizing.BorderBox };
        _navigation.Style = new() { Display = Display.Flex, Height = Length.Px(54), FlexShrink = 0, AlignItems = AlignItems.Center, Gap = Length.Px(4) };
        _progressText.Style = new() { Width = Length.Px(36), FontSize = Length.Px(11), Color = SubtleInk, TextAlign = TextAlign.Center };
        _actions.Style = new() { Display = Display.Flex, Height = Length.Px(64), FlexShrink = 0, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.SpaceAround };
        _status.Style = new() { Position = Position.Absolute, Top = Length.SafeAreaInsetTop + Length.Px(56), Bottom = Length.SafeAreaInsetBottom + Length.Px(126), Left = Length.Px(0), Right = Length.Px(0), Display = Display.None, FlexDirection = FlexDirection.Column, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center, Padding = Length.Px(24), Gap = Length.Px(12), BackgroundColor = PaperColor, Color = InkColor, BoxSizing = BoxSizing.BorderBox };
        _scrim.Style = new() { Display = panel ? Display.Block : Display.None, Position = Position.Absolute, Top = Length.Px(0), Left = Length.Px(0), Bottom = Length.Px(0), Right = Length.Px(0), BackgroundColor = Color.FromRgba(0, 0, 0, .15f) };
        _panel.Style = new() { Display = panel ? Display.Flex : Display.None, Position = Position.Absolute, Bottom = Length.SafeAreaInsetBottom + Length.Px(126), Left = Length.Px(0), Right = Length.Px(0), Height = Length.Px(IsContentsOpen ? 560 : IsAppearanceOpen ? 290 : 480), MaxHeight = Length.Percent(100) - Length.SafeAreaInsetTop - Length.SafeAreaInsetBottom - Length.Px(138), MaxWidth = Length.Px(560), MarginLeft = Length.Auto, MarginRight = Length.Auto, FlexDirection = FlexDirection.Column, BackgroundColor = PanelColor, Color = PanelInk, BorderTopLeftRadius = Length.Px(22), BorderTopRightRadius = Length.Px(22), PaddingLeft = Length.SafeAreaInsetLeft + Length.Px(20), PaddingRight = Length.SafeAreaInsetRight + Length.Px(20), BoxSizing = BoxSizing.BorderBox, Overflow = Overflow.Hidden };
        foreach (var (button, icon, text, active) in _tools)
        {
            button.Style = PlainButton();
            button.Style.FlexGrow = 1; button.Style.FlexBasis = Length.Px(0); button.Style.MinWidth = Length.Px(0);
            button.Style.Height = Length.Px(60); button.Style.Padding = Length.Px(0);
            button.Style.Display = Display.Flex; button.Style.FlexDirection = FlexDirection.Column; button.Style.AlignItems = AlignItems.Center; button.Style.JustifyContent = JustifyContent.Center;
            button.Children.Clear();
            button.Children.Add(new CanvasElement { Style = new() { Width = Length.Px(28), Height = Length.Px(28), PointerEvents = PointerEvents.None }, Paint = (c, r) => _icons.Draw(c, r, icon, active() ? Accent : SubtleInk) });
            button.Children.Add(new SpanElement { TextContent = text(), Style = new() { FontSize = Length.Px(11), MarginTop = Length.Px(3), Color = active() ? Accent : SubtleInk, PointerEvents = PointerEvents.None } });
        }
    }

    private protected void SettingsChanged()
    {
        ApplyParameters(); ApplyLayout(); BuildPanel(); UpdateStatus(); Repaint();
    }

    private void BuildPanel()
    {
        _panel.Children.Clear(); _panelBody.Children.Clear();
        if (_activePanel == ReaderPanel.None) return;
        var handle = new DivElement { Style = new() { Width = Length.Px(32), Height = Length.Px(4), MinHeight = Length.Px(4), MarginTop = Length.Px(10), MarginBottom = Length.Px(10), MarginLeft = Length.Auto, MarginRight = Length.Auto, BackgroundColor = ControlColor, BorderRadius = Length.Px(2), FlexShrink = 0 } };
        var heading = new DivElement { Style = new() { Display = Display.Flex, AlignItems = AlignItems.Center, Height = Length.Px(44), FlexShrink = 0, MarginBottom = Length.Px(10) } };
        heading.Children.Add(new SpanElement { TextContent = IsContentsOpen ? Texts.Contents : IsAppearanceOpen ? Texts.Appearance : Texts.Settings, Style = new() { FlexGrow = 1, FontSize = Length.Px(19), FontWeight = FontWeight.SemiBold } });
        var close = Button(Texts.Close, "miko-reader-panel-close", ClosePanel);
        close.Style = PlainButton(); close.Style.Color = Accent;
        heading.Children.Add(close);
        _panelBody.Style = new() { FlexGrow = 1, Height = Length.Px(0), MinHeight = Length.Px(0), OverflowY = Overflow.Auto, PaddingBottom = Length.Px(12) };
        _panel.Children.AddRange([handle, heading, _panelBody]);
        if (IsSettingsOpen) BuildSettings(_panelBody);
        else if (IsAppearanceOpen)
        {
            var label = new SpanElement { TextContent = Texts.Dimness, Style = new() { Display = Display.Block, FontSize = Length.Px(12), Color = SubtleInk } };
            _panelBody.Children.Add(label);
            var shade = new ReaderSlider("miko-reader-dimness", () => Accent, () => ControlColor, value => Dimness = value * .75f, _ => { }, Repaint);
            shade.Value = Dimness / .75f;
            _panelBody.Children.Add(shade.Element);
            BuildAppearance(_panelBody);
        }
        else if (Snapshot.Document is { } document)
        {
            _panelBody.Children.Add(new SpanElement { TextContent = document.Title, Style = new() { Display = Display.Block, FontSize = Length.Px(12), Color = SubtleInk, MarginBottom = Length.Px(12) } });
            if (_directoryStart > 0) _panelBody.Children.Add(Button(Texts.Previous, "miko-reader-directory-back", () => { _directoryStart = Math.Max(0, _directoryStart - 50); BuildPanel(); Repaint(); }));
            for (int i = _directoryStart; i < Math.Min(document.Entries.Count, _directoryStart + 50); i++)
            {
                int index = i;
                var button = Button("", "miko-reader-entry", () => { ClosePanel(); HideControls(); _ = GoToAsync(index); });
                button.Style = PlainButton(); button.Style.Width = Length.Percent(100); button.Style.MinHeight = Length.Px(56);
                button.Style.Display = Display.Flex; button.Style.AlignItems = AlignItems.Center; button.Style.TextAlign = TextAlign.Left;
                button.Style.BorderBottomWidth = Length.Px(1); button.Style.BorderBottomColor = ControlColor; button.Style.BorderBottomStyle = BorderStyle.Solid;
                button.Style.Color = i == CurrentIndex ? Accent : PanelInk;
                button.Children.Add(new SpanElement { TextContent = document.Entries[i].Title, Style = new() { FlexGrow = 1, MinWidth = Length.Px(0), FontSize = Length.Px(15), PointerEvents = PointerEvents.None } });
                button.Children.Add(new SpanElement { TextContent = i == CurrentIndex ? Texts.Current : (i + 1).ToString(CultureInfo.InvariantCulture), Style = new() { FontSize = Length.Px(11), Color = i == CurrentIndex ? Accent : SubtleInk, MarginLeft = Length.Px(12), PointerEvents = PointerEvents.None } });
                _panelBody.Children.Add(button);
            }
            if (_directoryStart + 50 < document.Entries.Count) _panelBody.Children.Add(Button(Texts.Next, "miko-reader-directory-next", () => { _directoryStart += 50; BuildPanel(); Repaint(); }));
        }
    }

    private Style PlainButton() => new() { Display = Display.Flex, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center, Padding = Length.Px(8), MinWidth = Length.Px(44), MinHeight = Length.Px(44), FlexShrink = 0, FontSize = Length.Px(14), BackgroundColor = Color.Transparent, Color = PanelInk, BorderWidth = Length.Px(0), BorderRadius = Length.Px(0) };
    private protected ButtonElement Button(string text, string className, Action action)
    {
        var style = PlainButton(); style.BackgroundColor = ControlColor; style.BorderRadius = Length.Px(22);
        var button = new ButtonElement { TextContent = text, Class = className, Style = style };
        button.OnClick = e => { e.StopPropagation(); if (!button.IsDisabled) action(); Repaint(); };
        return button;
    }
    private ButtonElement IconButton(string icon, string name, Action action)
    {
        var button = Button("", name, action);
        button.Style = PlainButton(); button.Style.Width = Length.Px(44); button.Style.Height = Length.Px(44); button.Style.Padding = Length.Px(0);
        button.Children.Add(new CanvasElement { Style = new() { Width = Length.Px(44), Height = Length.Px(44), PointerEvents = PointerEvents.None }, Paint = (c, r) => _icons.Draw(c, r, icon, button.IsDisabled ? SubtleInk : PanelInk) });
        return button;
    }

    private protected void Choice(DivElement panel, string label, string[] choices, int selected, Action<int> change)
    {
        panel.Children.Add(new SpanElement { TextContent = label, Style = new() { Display = Display.Block, FontSize = Length.Px(12), Color = SubtleInk, MarginTop = Length.Px(4), MarginBottom = Length.Px(8) } });
        var row = new DivElement { Style = new() { Display = Display.Flex, Gap = Length.Px(6), MarginBottom = Length.Px(16) } };
        for (int i = 0; i < choices.Length; i++)
        {
            int choice = i;
            var button = Button(choices[i], "miko-reader-choice", () => { change(choice); SettingsChanged(); });
            button.Style!.FlexGrow = 1; button.Style.FlexShrink = 1; button.Style.FlexBasis = Length.Px(0); button.Style.MinWidth = Length.Px(0); button.Style.FontSize = Length.Px(12);
            button.Style.BackgroundColor = i == selected ? Accent : ControlColor;
            button.Style.Color = i == selected ? (IsNight ? Color.FromHex("#203127") : Color.White) : PanelInk;
            row.Children.Add(button);
        }
        panel.Children.Add(row);
    }

    private protected void Stepper(DivElement panel, string label, float value, Action<float> change, float step, float min, float max)
    {
        var row = new DivElement { Style = new() { Display = Display.Flex, AlignItems = AlignItems.Center, Gap = Length.Px(8), MarginBottom = Length.Px(12) } };
        row.Children.Add(new SpanElement { TextContent = label, Style = new() { FlexGrow = 1, FontSize = Length.Px(13) } });
        row.Children.Add(Button("−", "miko-reader-decrease", () => { change(Math.Clamp(value - step, min, max)); SettingsChanged(); }));
        row.Children.Add(new SpanElement { TextContent = value.ToString("0.#", CultureInfo.InvariantCulture), Style = new() { Width = Length.Px(32), FontSize = Length.Px(13), TextAlign = TextAlign.Center } });
        row.Children.Add(Button("+", "miko-reader-increase", () => { change(Math.Clamp(value + step, min, max)); SettingsChanged(); }));
        panel.Children.Add(row);
    }

    private protected void ThemeSwatches(DivElement panel, int selected, Action<int> change)
    {
        Color[] colors = [Color.FromHex("#faf9f6"), Color.FromHex("#f1e5ce"), Color.FromHex("#dce8d0"), Color.FromHex("#1c2025")];
        string[] names = [Texts.Light, Texts.Sepia, Texts.EyeCare, Texts.Dark];
        var row = new DivElement { Style = new() { Display = Display.Flex, Gap = Length.Px(8), MarginTop = Length.Px(12), MarginBottom = Length.Px(16) } };
        for (int i = 0; i < colors.Length; i++)
        {
            int choice = i;
            var button = Button(names[i], "miko-reader-theme", () => { change(choice); SettingsChanged(); });
            button.Style!.FlexGrow = 1; button.Style.FlexShrink = 1; button.Style.FlexBasis = Length.Px(0); button.Style.MinWidth = Length.Px(0); button.Style.Height = Length.Px(52);
            button.Style.FontSize = Length.Px(12); button.Style.BackgroundColor = colors[i]; button.Style.Color = i == 3 ? Color.White : Color.FromHex("#30362f");
            button.Style.BorderWidth = Length.Px(i == selected ? 2 : 1); button.Style.BorderStyle = BorderStyle.Solid; button.Style.BorderColor = i == selected ? Accent : ControlColor;
            row.Children.Add(button);
        }
        panel.Children.Add(row);
    }
}
