using System.Globalization;
using Miko.Common;
using Miko.Core.DomElements;
using Miko.Platform;
using Miko.Styling;

namespace Miko.Components.Player;

// The settings panel: a translucent drawer on the right on phones, a pop-up above the settings
// button on the desktop — both over the picture, as mainstream players do.
public sealed partial class MikoPlayer
{
    private static readonly float[] Speeds = [.5f, .75f, 1, 1.25f, 1.5f, 2];
    private static readonly Color PanelColor = Color.FromRgba(22, 22, 24, .86f);
    private static readonly Color SubtleText = Color.FromRgba(255, 255, 255, .62f);
    private static readonly Color ChipColor = Color.FromRgba(255, 255, 255, .14f);
    private static readonly Color SwitchOffColor = Color.FromRgba(255, 255, 255, .3f);

    // The back-button slot held while the panel is open, and the presentation it was opened with.
    private IDisposable? _settingsBack;
    private bool _settingsDrawer;

    /// <summary>
    /// Phones present settings as a drawer on the right (touch-sized rows, the controls make way);
    /// the desktop as a pop-up above the settings button that leaves the controls up. The simulator
    /// reports the simulated phone's platform, so it previews the drawer.
    /// </summary>
    private bool UsesSettingsDrawer => (PlatformService?.Platform ?? PlatformInfo.Detect()) is HostPlatform.Android or HostPlatform.Ios;

    public void ToggleSettings()
    {
        if (IsSettingsOpen) CloseSettings();
        else OpenSettings();
        Repaint();
    }

    private void OpenSettings()
    {
        if (IsSettingsOpen) return;
        IsSettingsOpen = true;
        _settingsDrawer = UsesSettingsDrawer;
        BuildSettings();
        ApplySettingsStyle();
        CancelAutoHide();
        if (_settingsDrawer) SetControlsVisible(false);
        // Back (Android back, iOS edge swipe, Escape) closes the panel before it leaves fullscreen or
        // the page: same priority as fullscreen, registered later, so asked first.
        _settingsBack = PlatformService?.Back.Register(() =>
        {
            if (_disposed || !IsSettingsOpen) return false;
            CloseSettings();
            Repaint();
            return true;
        }, BackButtonPriority.Overlay);
    }

    private void CloseSettings()
    {
        if (!IsSettingsOpen) return;
        IsSettingsOpen = false;
        _settingsBack?.Dispose();
        _settingsBack = null;
        ApplySettingsStyle();
        _settingsPanel.Children.Clear();
        ScheduleAutoHide();
    }

    /// <summary>
    /// Drawer: the right edge of the player, full height, clear of the cutout in fullscreen. Pop-up:
    /// anchored above the control bar on the right, as tall as its content and at most the picture;
    /// the layer around it lets taps through (they close it).
    /// </summary>
    private void ApplySettingsStyle()
    {
        var layer = new Style
        {
            Display = IsSettingsOpen ? Display.Flex : Display.None, FlexDirection = FlexDirection.Column, Position = Position.Absolute,
            BoxSizing = BoxSizing.BorderBox, Opacity = 1, Transitions = FadeTransitions,
        };
        var panel = new Style
        {
            Display = Display.Block, MinHeight = Length.Px(0), OverflowY = Overflow.Auto, BoxSizing = BoxSizing.BorderBox,
            Color = Color.White, FontSize = Length.Px(13),
        };
        if (_settingsDrawer)
        {
            layer.Top = Length.Px(0); layer.Right = Length.Px(0); layer.Bottom = Length.Px(0);
            layer.Width = Length.Px(300); layer.MaxWidth = Length.Percent(100);
            layer.BackgroundColor = PanelColor;
            if (IsFullscreen)
            {
                layer.PaddingTop = Length.SafeAreaInsetTop;
                layer.PaddingRight = Length.SafeAreaInsetRight;
                layer.PaddingBottom = Length.SafeAreaInsetBottom;
                layer.Width = Length.SafeAreaInsetRight + Length.Px(300);
            }
            panel.FlexGrow = 1;
            panel.PaddingTop = Length.Px(8); panel.PaddingBottom = Length.Px(8);
            panel.PaddingLeft = Length.Px(16); panel.PaddingRight = Length.Px(16);
        }
        else
        {
            layer.Top = Length.Px(8); layer.Right = Length.Px(12); layer.Bottom = Length.Px(76);
            layer.Width = Length.Px(260); layer.MaxWidth = Length.Percent(100) - Length.Px(24);
            layer.JustifyContent = JustifyContent.FlexEnd;
            layer.PointerEvents = PointerEvents.None;
            panel.FlexShrink = 1;
            panel.PointerEvents = PointerEvents.Auto;
            panel.BackgroundColor = PanelColor;
            panel.BorderRadius = 8;
            panel.PaddingTop = Length.Px(6); panel.PaddingBottom = Length.Px(6);
            panel.PaddingLeft = Length.Px(12); panel.PaddingRight = Length.Px(12);
        }
        _settings.Style = layer;
        _settingsPanel.Style = panel;
    }

    private void BuildSettings()
    {
        _settingsPanel.Children.Clear();
        AddSectionTitle("Playback");
        AddChoices("Speed", Speeds, Controller.PlaybackRate, Controller.SetPlaybackRate);
        AddSwitch("Loop", Controller.Loop, Controller.SetLoop);
        AddSlider("Volume", Controller.Volume, 0, 1, Percent, Controller.SetVolume);
        AddSectionTitle("Danmaku");
        AddSwitch("Show danmaku", _timeline.Settings.Enabled, value => SetDanmakuSettings(_timeline.Settings with { Enabled = value }));
        AddSlider("Opacity", _timeline.Settings.Opacity, 0, 1, Percent, value => SetDanmakuSettings(_timeline.Settings with { Opacity = value }));
        AddSlider("Text size", _timeline.Settings.FontSize, 12, 48, value => $"{value:0}",
            value => SetDanmakuSettings(_timeline.Settings with { FontSize = MathF.Round(value) }));
        AddSlider("Travel time", _timeline.Settings.Duration, 2, 20, value => value.ToString("0.#", CultureInfo.InvariantCulture) + "s",
            value => SetDanmakuSettings(_timeline.Settings with { Duration = value }));
        AddSlider("Area", _timeline.Settings.Area, .2f, 1, Percent, value => SetDanmakuSettings(_timeline.Settings with { Area = value }));
        AddSlider("Density", _timeline.Settings.MaxVisible, 1, Options.MaxVisibleDanmaku, value => $"{MathF.Round(value):0}",
            value => SetDanmakuSettings(_timeline.Settings with { MaxVisible = (int)MathF.Round(value) }));
    }

    private static string Percent(float value) => $"{MathF.Round(value * 100):0}%";

    /// <summary>Touch rows are 44px targets; mouse rows are denser.</summary>
    private float RowHeight => _settingsDrawer ? 44 : 36;

    /// <param name="wrap">The control goes on a line of its own below the label.</param>
    /// <param name="fixedLabel">The label keeps its 76px and the control takes the rest of the line.</param>
    private DivElement AddRow(string label, bool wrap = false, bool fixedLabel = false)
    {
        var row = new DivElement { Class = "miko-player-setting", Style = new Style
        { Display = Display.Flex, FlexWrap = wrap ? FlexWrap.Wrap : FlexWrap.Nowrap, AlignItems = AlignItems.Center,
            MinHeight = Length.Px(RowHeight), Width = Length.Percent(100) } };
        row.Children.Add(new SpanElement { TextContent = label, Style = new Style
        { Display = Display.Block, FlexGrow = fixedLabel ? 0 : 1, FlexShrink = 0, FlexBasis = Length.Px(76), MinWidth = Length.Px(0), WhiteSpace = WhiteSpace.Nowrap } });
        _settingsPanel.Children.Add(row);
        return row;
    }

    private void AddSectionTitle(string title) => _settingsPanel.Children.Add(new DivElement
    {
        TextContent = title,
        Style = new Style { Display = Display.Block, Color = SubtleText, FontSize = Length.Px(12), PaddingTop = Length.Px(8), PaddingBottom = Length.Px(2) },
    });

    /// <summary>
    /// A row of chips under the label, sharing the line evenly; the current one is filled with the
    /// accent color.
    /// </summary>
    private void AddChoices(string label, float[] values, float current, Action<float> change)
    {
        var row = AddRow(label, wrap: true);
        var chips = new DivElement { Style = new Style
        { Display = Display.Flex, Gap = Length.Px(4), Width = Length.Percent(100), PaddingBottom = Length.Px(6) } };
        float height = _settingsDrawer ? 32 : 26;
        var buttons = new List<(DivElement Chip, float Value)>();
        void Restyle(float selected)
        {
            foreach (var (chip, value) in buttons)
                chip.Style = new Style
                {
                    Display = Display.Flex, AlignItems = AlignItems.Center, JustifyContent = JustifyContent.Center,
                    FlexGrow = 1, FlexShrink = 1, FlexBasis = Length.Px(0), MinWidth = Length.Px(0), Height = Length.Px(height),
                    BorderRadius = height / 2, FontSize = Length.Px(12), WhiteSpace = WhiteSpace.Nowrap, BoxSizing = BoxSizing.BorderBox,
                    BackgroundColor = value == selected ? ProgressColor : ChipColor,
                    Color = value == selected ? Color.FromHex("#0b1f19") : Color.White,
                };
        }
        foreach (var value in values)
        {
            var chip = new DivElement { Class = "miko-player-chip", TextContent = FormatRate(value) };
            chip.OnClick = e =>
            {
                e.StopPropagation();
                change(value);
                Restyle(value);
                Repaint();
            };
            buttons.Add((chip, value));
            chips.Children.Add(chip);
        }
        Restyle(current);
        row.Children.Add(chips);
    }

    /// <summary>An on/off switch; the whole row is the target.</summary>
    private void AddSwitch(string label, bool value, Action<bool> change)
    {
        var row = AddRow(label);
        var track = new DivElement { Class = "miko-player-switch" };
        var knob = new DivElement();
        track.Children.Add(knob);
        void Restyle()
        {
            track.Style = new Style { Display = Display.Block, Position = Position.Relative, Width = Length.Px(36), Height = Length.Px(20),
                FlexShrink = 0, BorderRadius = 10, BackgroundColor = value ? ProgressColor : SwitchOffColor };
            knob.Style = new Style { Display = Display.Block, Position = Position.Absolute, Top = Length.Px(2), Left = Length.Px(value ? 18 : 2),
                Width = Length.Px(16), Height = Length.Px(16), BorderRadius = 8, BackgroundColor = Color.White };
        }
        row.OnClick = e =>
        {
            e.StopPropagation();
            value = !value;
            change(value);
            Restyle();
            Repaint();
        };
        Restyle();
        row.Children.Add(track);
    }

    /// <summary>Label, slider and the current value.</summary>
    private void AddSlider(string label, float value, float min, float max, Func<float, string> format, Action<float> change)
    {
        var row = AddRow(label, fixedLabel: true);
        var text = new SpanElement { TextContent = format(value), Style = new Style
        { Display = Display.Block, Width = Length.Px(44), FlexShrink = 0, TextAlign = TextAlign.Right, Color = SubtleText, FontSize = Length.Px(12) } };
        var slider = new PlayerSlider(min, max, value, ProgressColor, v =>
        {
            change(v);
            text.TextContent = format(v);
        }, Repaint);
        row.Children.Add(slider.Element);
        row.Children.Add(text);
    }
}
