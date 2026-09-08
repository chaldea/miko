using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for ActionSheet styles.</summary>
public sealed class ActionSheetToken : IonicToken
{
    private Color _backdropColor = Color.FromHex("000000");
    public Color BackdropColor
    {
        get => _backdropColor;
        set { _backdropColor = value; Mark(nameof(BackdropColor)); }
    }

    private float _backdropOpacity = 0.32f;
    public float BackdropOpacity
    {
        get => _backdropOpacity;
        set { _backdropOpacity = value; Mark(nameof(BackdropOpacity)); }
    }

    private float _maxWidth = 500f;
    public float MaxWidth
    {
        get => _maxWidth;
        set { _maxWidth = value; Mark(nameof(MaxWidth)); }
    }

    private float _titleFontSize = 16f;
    public float TitleFontSize
    {
        get => _titleFontSize;
        set { _titleFontSize = value; Mark(nameof(TitleFontSize)); }
    }

    private float _titlePaddingY = 20f;
    public float TitlePaddingY
    {
        get => _titlePaddingY;
        set { _titlePaddingY = value; Mark(nameof(TitlePaddingY)); }
    }

    private float _titlePaddingX = 16f;
    public float TitlePaddingX
    {
        get => _titlePaddingX;
        set { _titlePaddingX = value; Mark(nameof(TitlePaddingX)); }
    }

    private float _subTitleFontSize = 14f;
    public float SubTitleFontSize
    {
        get => _subTitleFontSize;
        set { _subTitleFontSize = value; Mark(nameof(SubTitleFontSize)); }
    }

    private float _buttonHeight = 52f;
    public float ButtonHeight
    {
        get => _buttonHeight;
        set { _buttonHeight = value; Mark(nameof(ButtonHeight)); }
    }

    private float _buttonFontSize = 16f;
    public float ButtonFontSize
    {
        get => _buttonFontSize;
        set { _buttonFontSize = value; Mark(nameof(ButtonFontSize)); }
    }

    private float _buttonPaddingY = 12f;
    public float ButtonPaddingY
    {
        get => _buttonPaddingY;
        set { _buttonPaddingY = value; Mark(nameof(ButtonPaddingY)); }
    }

    private float _buttonPaddingX = 16f;
    public float ButtonPaddingX
    {
        get => _buttonPaddingX;
        set { _buttonPaddingX = value; Mark(nameof(ButtonPaddingX)); }
    }

    private float _iconFontSize = 24f;
    public float IconFontSize
    {
        get => _iconFontSize;
        set { _iconFontSize = value; Mark(nameof(IconFontSize)); }
    }

    private JustifyContent _buttonJustify = JustifyContent.FlexStart;
    public JustifyContent ButtonJustify
    {
        get => _buttonJustify;
        set { _buttonJustify = value; Mark(nameof(ButtonJustify)); }
    }

    private TextAlign _textAlign = TextAlign.Left;
    public TextAlign TextAlign
    {
        get => _textAlign;
        set { _textAlign = value; Mark(nameof(TextAlign)); }
    }

    private FontWeight _cancelFontWeight = FontWeight.Normal;
    public FontWeight CancelFontWeight
    {
        get => _cancelFontWeight;
        set { _cancelFontWeight = value; Mark(nameof(CancelFontWeight)); }
    }

    private float _enterDuration = 0.4f;
    public float EnterDuration
    {
        get => _enterDuration;
        set { _enterDuration = value; Mark(nameof(EnterDuration)); }
    }

    private float _leaveDuration = 0.45f;
    public float LeaveDuration
    {
        get => _leaveDuration;
        set { _leaveDuration = value; Mark(nameof(LeaveDuration)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    private float _borderRadius = default;
    public float BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Mark(nameof(BorderRadius)); }
    }

    private float _containerPaddingX = default;
    public float ContainerPaddingX
    {
        get => _containerPaddingX;
        set { _containerPaddingX = value; Mark(nameof(ContainerPaddingX)); }
    }

    private float _groupMarginTop = default;
    public float GroupMarginTop
    {
        get => _groupMarginTop;
        set { _groupMarginTop = value; Mark(nameof(GroupMarginTop)); }
    }

    private float _groupMarginBottom = default;
    public float GroupMarginBottom
    {
        get => _groupMarginBottom;
        set { _groupMarginBottom = value; Mark(nameof(GroupMarginBottom)); }
    }

    private Color _titleColor = default;
    public Color TitleColor
    {
        get => _titleColor;
        set { _titleColor = value; Mark(nameof(TitleColor)); }
    }

    private Color _buttonColor = default;
    public Color ButtonColor
    {
        get => _buttonColor;
        set { _buttonColor = value; Mark(nameof(ButtonColor)); }
    }

    private Color _destructiveColor = default;
    public Color DestructiveColor
    {
        get => _destructiveColor;
        set { _destructiveColor = value; Mark(nameof(DestructiveColor)); }
    }

    private Color _buttonBorderColor = default;
    public Color ButtonBorderColor
    {
        get => _buttonBorderColor;
        set { _buttonBorderColor = value; Mark(nameof(ButtonBorderColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (ActionSheetToken)target;
        if (IsSpecified(nameof(BackdropColor))) token.BackdropColor = BackdropColor;
        if (IsSpecified(nameof(BackdropOpacity))) token.BackdropOpacity = BackdropOpacity;
        if (IsSpecified(nameof(MaxWidth))) token.MaxWidth = MaxWidth;
        if (IsSpecified(nameof(TitleFontSize))) token.TitleFontSize = TitleFontSize;
        if (IsSpecified(nameof(TitlePaddingY))) token.TitlePaddingY = TitlePaddingY;
        if (IsSpecified(nameof(TitlePaddingX))) token.TitlePaddingX = TitlePaddingX;
        if (IsSpecified(nameof(SubTitleFontSize))) token.SubTitleFontSize = SubTitleFontSize;
        if (IsSpecified(nameof(ButtonHeight))) token.ButtonHeight = ButtonHeight;
        if (IsSpecified(nameof(ButtonFontSize))) token.ButtonFontSize = ButtonFontSize;
        if (IsSpecified(nameof(ButtonPaddingY))) token.ButtonPaddingY = ButtonPaddingY;
        if (IsSpecified(nameof(ButtonPaddingX))) token.ButtonPaddingX = ButtonPaddingX;
        if (IsSpecified(nameof(IconFontSize))) token.IconFontSize = IconFontSize;
        if (IsSpecified(nameof(ButtonJustify))) token.ButtonJustify = ButtonJustify;
        if (IsSpecified(nameof(TextAlign))) token.TextAlign = TextAlign;
        if (IsSpecified(nameof(CancelFontWeight))) token.CancelFontWeight = CancelFontWeight;
        if (IsSpecified(nameof(EnterDuration))) token.EnterDuration = EnterDuration;
        if (IsSpecified(nameof(LeaveDuration))) token.LeaveDuration = LeaveDuration;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(ContainerPaddingX))) token.ContainerPaddingX = ContainerPaddingX;
        if (IsSpecified(nameof(GroupMarginTop))) token.GroupMarginTop = GroupMarginTop;
        if (IsSpecified(nameof(GroupMarginBottom))) token.GroupMarginBottom = GroupMarginBottom;
        if (IsSpecified(nameof(TitleColor))) token.TitleColor = TitleColor;
        if (IsSpecified(nameof(ButtonColor))) token.ButtonColor = ButtonColor;
        if (IsSpecified(nameof(DestructiveColor))) token.DestructiveColor = DestructiveColor;
        if (IsSpecified(nameof(ButtonBorderColor))) token.ButtonBorderColor = ButtonBorderColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, BackdropColor);
        AppendValue(builder, BackdropOpacity);
        AppendValue(builder, Background);
        AppendValue(builder, BorderRadius);
        AppendValue(builder, ButtonBorderColor);
        AppendValue(builder, ButtonColor);
        AppendValue(builder, ButtonFontSize);
        AppendValue(builder, ButtonHeight);
        AppendValue(builder, ButtonJustify);
        AppendValue(builder, ButtonPaddingX);
        AppendValue(builder, ButtonPaddingY);
        AppendValue(builder, CancelFontWeight);
        AppendValue(builder, ContainerPaddingX);
        AppendValue(builder, DestructiveColor);
        AppendValue(builder, EnterDuration);
        AppendValue(builder, GroupMarginBottom);
        AppendValue(builder, GroupMarginTop);
        AppendValue(builder, IconFontSize);
        AppendValue(builder, LeaveDuration);
        AppendValue(builder, MaxWidth);
        AppendValue(builder, SubTitleFontSize);
        AppendValue(builder, TextAlign);
        AppendValue(builder, TitleColor);
        AppendValue(builder, TitleFontSize);
        AppendValue(builder, TitlePaddingX);
        AppendValue(builder, TitlePaddingY);
    }
}
