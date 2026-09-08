using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Palette styles.</summary>
public sealed class PaletteToken : IonicToken
{
    private Color _primary = default;
    public Color Primary
    {
        get => _primary;
        set { _primary = value; Mark(nameof(Primary)); }
    }

    private Color _secondary = default;
    public Color Secondary
    {
        get => _secondary;
        set { _secondary = value; Mark(nameof(Secondary)); }
    }

    private Color _tertiary = default;
    public Color Tertiary
    {
        get => _tertiary;
        set { _tertiary = value; Mark(nameof(Tertiary)); }
    }

    private Color _success = default;
    public Color Success
    {
        get => _success;
        set { _success = value; Mark(nameof(Success)); }
    }

    private Color _warning = default;
    public Color Warning
    {
        get => _warning;
        set { _warning = value; Mark(nameof(Warning)); }
    }

    private Color _danger = default;
    public Color Danger
    {
        get => _danger;
        set { _danger = value; Mark(nameof(Danger)); }
    }

    private Color _light = default;
    public Color Light
    {
        get => _light;
        set { _light = value; Mark(nameof(Light)); }
    }

    private Color _medium = default;
    public Color Medium
    {
        get => _medium;
        set { _medium = value; Mark(nameof(Medium)); }
    }

    private Color _dark = default;
    public Color Dark
    {
        get => _dark;
        set { _dark = value; Mark(nameof(Dark)); }
    }

    private Color _backgroundColor = default;
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set { _backgroundColor = value; Mark(nameof(BackgroundColor)); }
    }

    private Color _textColor = default;
    public Color TextColor
    {
        get => _textColor;
        set { _textColor = value; Mark(nameof(TextColor)); }
    }

    private Color _backdropColor = default;
    public Color BackdropColor
    {
        get => _backdropColor;
        set { _backdropColor = value; Mark(nameof(BackdropColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (PaletteToken)target;
        if (IsSpecified(nameof(Primary))) token.Primary = Primary;
        if (IsSpecified(nameof(Secondary))) token.Secondary = Secondary;
        if (IsSpecified(nameof(Tertiary))) token.Tertiary = Tertiary;
        if (IsSpecified(nameof(Success))) token.Success = Success;
        if (IsSpecified(nameof(Warning))) token.Warning = Warning;
        if (IsSpecified(nameof(Danger))) token.Danger = Danger;
        if (IsSpecified(nameof(Light))) token.Light = Light;
        if (IsSpecified(nameof(Medium))) token.Medium = Medium;
        if (IsSpecified(nameof(Dark))) token.Dark = Dark;
        if (IsSpecified(nameof(BackgroundColor))) token.BackgroundColor = BackgroundColor;
        if (IsSpecified(nameof(TextColor))) token.TextColor = TextColor;
        if (IsSpecified(nameof(BackdropColor))) token.BackdropColor = BackdropColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, BackdropColor);
        AppendValue(builder, BackgroundColor);
        AppendValue(builder, Danger);
        AppendValue(builder, Dark);
        AppendValue(builder, Light);
        AppendValue(builder, Medium);
        AppendValue(builder, Primary);
        AppendValue(builder, Secondary);
        AppendValue(builder, Success);
        AppendValue(builder, Tertiary);
        AppendValue(builder, TextColor);
        AppendValue(builder, Warning);
    }
}
