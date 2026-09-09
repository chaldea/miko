using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Tab styles.</summary>
public sealed class TabToken : IonicToken
{
    private float _barHeight = 56f;
    public float BarHeight
    {
        get => _barHeight;
        set { _barHeight = value; Mark(nameof(BarHeight)); }
    }

    private float _barBorderWidth = 1f;
    public float BarBorderWidth
    {
        get => _barBorderWidth;
        set { _barBorderWidth = value; Mark(nameof(BarBorderWidth)); }
    }

    private float _buttonFontSize = 12f;
    public float ButtonFontSize
    {
        get => _buttonFontSize;
        set { _buttonFontSize = value; Mark(nameof(ButtonFontSize)); }
    }

    private float _buttonIconSize = 22f;
    public float ButtonIconSize
    {
        get => _buttonIconSize;
        set { _buttonIconSize = value; Mark(nameof(ButtonIconSize)); }
    }

    private float _buttonMaxWidth = 168f;
    public float ButtonMaxWidth
    {
        get => _buttonMaxWidth;
        set { _buttonMaxWidth = value; Mark(nameof(ButtonMaxWidth)); }
    }

    private float _buttonPaddingX = 12f;
    public float ButtonPaddingX
    {
        get => _buttonPaddingX;
        set { _buttonPaddingX = value; Mark(nameof(ButtonPaddingX)); }
    }

    private float _buttonBadgeFontSize = 8f;
    public float ButtonBadgeFontSize
    {
        get => _buttonBadgeFontSize;
        set { _buttonBadgeFontSize = value; Mark(nameof(ButtonBadgeFontSize)); }
    }

    private float _buttonBadgeMinWidth = 12f;
    public float ButtonBadgeMinWidth
    {
        get => _buttonBadgeMinWidth;
        set { _buttonBadgeMinWidth = value; Mark(nameof(ButtonBadgeMinWidth)); }
    }

    private float _buttonBadgeBorderRadius = 8f;
    public float ButtonBadgeBorderRadius
    {
        get => _buttonBadgeBorderRadius;
        set { _buttonBadgeBorderRadius = value; Mark(nameof(ButtonBadgeBorderRadius)); }
    }

    private float _buttonBadgePaddingTop = 3f;
    public float ButtonBadgePaddingTop
    {
        get => _buttonBadgePaddingTop;
        set { _buttonBadgePaddingTop = value; Mark(nameof(ButtonBadgePaddingTop)); }
    }

    private float _buttonBadgePaddingEnd = 2f;
    public float ButtonBadgePaddingEnd
    {
        get => _buttonBadgePaddingEnd;
        set { _buttonBadgePaddingEnd = value; Mark(nameof(ButtonBadgePaddingEnd)); }
    }

    private float _buttonBadgePaddingBottom = 2f;
    public float ButtonBadgePaddingBottom
    {
        get => _buttonBadgePaddingBottom;
        set { _buttonBadgePaddingBottom = value; Mark(nameof(ButtonBadgePaddingBottom)); }
    }

    private float _buttonBadgePaddingStart = 2f;
    public float ButtonBadgePaddingStart
    {
        get => _buttonBadgePaddingStart;
        set { _buttonBadgePaddingStart = value; Mark(nameof(ButtonBadgePaddingStart)); }
    }

    private float _buttonBadgeSizeEmpty = 8f;
    public float ButtonBadgeSizeEmpty
    {
        get => _buttonBadgeSizeEmpty;
        set { _buttonBadgeSizeEmpty = value; Mark(nameof(ButtonBadgeSizeEmpty)); }
    }

    private Color _barBackground = default;
    public Color BarBackground
    {
        get => _barBackground;
        set { _barBackground = value; Mark(nameof(BarBackground)); }
    }

    private Color _barBorderColor = default;
    public Color BarBorderColor
    {
        get => _barBorderColor;
        set { _barBorderColor = value; Mark(nameof(BarBorderColor)); }
    }

    private Color _barColor = default;
    public Color BarColor
    {
        get => _barColor;
        set { _barColor = value; Mark(nameof(BarColor)); }
    }

    private Color _barColorSelected = default;
    public Color BarColorSelected
    {
        get => _barColorSelected;
        set { _barColorSelected = value; Mark(nameof(BarColorSelected)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (TabToken)target;
        if (IsSpecified(nameof(BarHeight))) token.BarHeight = BarHeight;
        if (IsSpecified(nameof(BarBorderWidth))) token.BarBorderWidth = BarBorderWidth;
        if (IsSpecified(nameof(ButtonFontSize))) token.ButtonFontSize = ButtonFontSize;
        if (IsSpecified(nameof(ButtonIconSize))) token.ButtonIconSize = ButtonIconSize;
        if (IsSpecified(nameof(ButtonMaxWidth))) token.ButtonMaxWidth = ButtonMaxWidth;
        if (IsSpecified(nameof(ButtonPaddingX))) token.ButtonPaddingX = ButtonPaddingX;
        if (IsSpecified(nameof(ButtonBadgeFontSize))) token.ButtonBadgeFontSize = ButtonBadgeFontSize;
        if (IsSpecified(nameof(ButtonBadgeMinWidth))) token.ButtonBadgeMinWidth = ButtonBadgeMinWidth;
        if (IsSpecified(nameof(ButtonBadgeBorderRadius))) token.ButtonBadgeBorderRadius = ButtonBadgeBorderRadius;
        if (IsSpecified(nameof(ButtonBadgePaddingTop))) token.ButtonBadgePaddingTop = ButtonBadgePaddingTop;
        if (IsSpecified(nameof(ButtonBadgePaddingEnd))) token.ButtonBadgePaddingEnd = ButtonBadgePaddingEnd;
        if (IsSpecified(nameof(ButtonBadgePaddingBottom))) token.ButtonBadgePaddingBottom = ButtonBadgePaddingBottom;
        if (IsSpecified(nameof(ButtonBadgePaddingStart))) token.ButtonBadgePaddingStart = ButtonBadgePaddingStart;
        if (IsSpecified(nameof(ButtonBadgeSizeEmpty))) token.ButtonBadgeSizeEmpty = ButtonBadgeSizeEmpty;
        if (IsSpecified(nameof(BarBackground))) token.BarBackground = BarBackground;
        if (IsSpecified(nameof(BarBorderColor))) token.BarBorderColor = BarBorderColor;
        if (IsSpecified(nameof(BarColor))) token.BarColor = BarColor;
        if (IsSpecified(nameof(BarColorSelected))) token.BarColorSelected = BarColorSelected;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, BarBackground);
        AppendValue(builder, BarBorderColor);
        AppendValue(builder, BarBorderWidth);
        AppendValue(builder, BarColor);
        AppendValue(builder, BarColorSelected);
        AppendValue(builder, BarHeight);
        AppendValue(builder, ButtonBadgeBorderRadius);
        AppendValue(builder, ButtonBadgeFontSize);
        AppendValue(builder, ButtonBadgeMinWidth);
        AppendValue(builder, ButtonBadgePaddingBottom);
        AppendValue(builder, ButtonBadgePaddingEnd);
        AppendValue(builder, ButtonBadgePaddingStart);
        AppendValue(builder, ButtonBadgePaddingTop);
        AppendValue(builder, ButtonBadgeSizeEmpty);
        AppendValue(builder, ButtonFontSize);
        AppendValue(builder, ButtonIconSize);
        AppendValue(builder, ButtonMaxWidth);
        AppendValue(builder, ButtonPaddingX);
    }
}
