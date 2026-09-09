using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Alert styles.</summary>
public sealed class AlertToken : IonicToken
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

    private float _minWidth = 250f;
    public float MinWidth
    {
        get => _minWidth;
        set { _minWidth = value; Mark(nameof(MinWidth)); }
    }

    private float _maxWidth = 280f;
    public float MaxWidth
    {
        get => _maxWidth;
        set { _maxWidth = value; Mark(nameof(MaxWidth)); }
    }

    private float _borderRadius = 4f;
    public float BorderRadius
    {
        get => _borderRadius;
        set { _borderRadius = value; Mark(nameof(BorderRadius)); }
    }

    private List<BoxShadow> _boxShadow = new();
    public List<BoxShadow> BoxShadow
    {
        get { Mark(nameof(BoxShadow)); return _boxShadow; }
        set { _boxShadow = value; Mark(nameof(BoxShadow)); }
    }

    private float _headPaddingY = 20f;
    public float HeadPaddingY
    {
        get => _headPaddingY;
        set { _headPaddingY = value; Mark(nameof(HeadPaddingY)); }
    }

    private float _headPaddingX = 23f;
    public float HeadPaddingX
    {
        get => _headPaddingX;
        set { _headPaddingX = value; Mark(nameof(HeadPaddingX)); }
    }

    private TextAlign _headTextAlign = TextAlign.Left;
    public TextAlign HeadTextAlign
    {
        get => _headTextAlign;
        set { _headTextAlign = value; Mark(nameof(HeadTextAlign)); }
    }

    private float _titleFontSize = 20f;
    public float TitleFontSize
    {
        get => _titleFontSize;
        set { _titleFontSize = value; Mark(nameof(TitleFontSize)); }
    }

    private FontWeight _titleFontWeight = FontWeight.Medium;
    public FontWeight TitleFontWeight
    {
        get => _titleFontWeight;
        set { _titleFontWeight = value; Mark(nameof(TitleFontWeight)); }
    }

    private float _subTitleFontSize = 16f;
    public float SubTitleFontSize
    {
        get => _subTitleFontSize;
        set { _subTitleFontSize = value; Mark(nameof(SubTitleFontSize)); }
    }

    private float _messagePaddingY = 20f;
    public float MessagePaddingY
    {
        get => _messagePaddingY;
        set { _messagePaddingY = value; Mark(nameof(MessagePaddingY)); }
    }

    private float _messagePaddingX = 24f;
    public float MessagePaddingX
    {
        get => _messagePaddingX;
        set { _messagePaddingX = value; Mark(nameof(MessagePaddingX)); }
    }

    private float _messageFontSize = 16f;
    public float MessageFontSize
    {
        get => _messageFontSize;
        set { _messageFontSize = value; Mark(nameof(MessageFontSize)); }
    }

    private float _buttonGroupPadding = 8f;
    public float ButtonGroupPadding
    {
        get => _buttonGroupPadding;
        set { _buttonGroupPadding = value; Mark(nameof(ButtonGroupPadding)); }
    }

    private JustifyContent _buttonGroupJustify = JustifyContent.FlexEnd;
    public JustifyContent ButtonGroupJustify
    {
        get => _buttonGroupJustify;
        set { _buttonGroupJustify = value; Mark(nameof(ButtonGroupJustify)); }
    }

    private FlexWrap _buttonGroupFlexWrap = FlexWrap.WrapReverse;
    public FlexWrap ButtonGroupFlexWrap
    {
        get => _buttonGroupFlexWrap;
        set { _buttonGroupFlexWrap = value; Mark(nameof(ButtonGroupFlexWrap)); }
    }

    private float _buttonFontSize = 14f;
    public float ButtonFontSize
    {
        get => _buttonFontSize;
        set { _buttonFontSize = value; Mark(nameof(ButtonFontSize)); }
    }

    private FontWeight _buttonFontWeight = FontWeight.Medium;
    public FontWeight ButtonFontWeight
    {
        get => _buttonFontWeight;
        set { _buttonFontWeight = value; Mark(nameof(ButtonFontWeight)); }
    }

    private float _buttonBorderRadius = 2f;
    public float ButtonBorderRadius
    {
        get => _buttonBorderRadius;
        set { _buttonBorderRadius = value; Mark(nameof(ButtonBorderRadius)); }
    }

    private TextTransform _buttonTextTransform = TextTransform.None;
    public TextTransform ButtonTextTransform
    {
        get => _buttonTextTransform;
        set { _buttonTextTransform = value; Mark(nameof(ButtonTextTransform)); }
    }

    private float _buttonPadding = 10f;
    public float ButtonPadding
    {
        get => _buttonPadding;
        set { _buttonPadding = value; Mark(nameof(ButtonPadding)); }
    }

    private float _buttonMarginX = 8f;
    public float ButtonMarginX
    {
        get => _buttonMarginX;
        set { _buttonMarginX = value; Mark(nameof(ButtonMarginX)); }
    }

    private float _tappableHeight = 48f;
    public float TappableHeight
    {
        get => _tappableHeight;
        set { _tappableHeight = value; Mark(nameof(TappableHeight)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    private Color _titleColor = default;
    public Color TitleColor
    {
        get => _titleColor;
        set { _titleColor = value; Mark(nameof(TitleColor)); }
    }

    private Color _subTitleColor = default;
    public Color SubTitleColor
    {
        get => _subTitleColor;
        set { _subTitleColor = value; Mark(nameof(SubTitleColor)); }
    }

    private Color _messageColor = default;
    public Color MessageColor
    {
        get => _messageColor;
        set { _messageColor = value; Mark(nameof(MessageColor)); }
    }

    private Color _buttonColor = default;
    public Color ButtonColor
    {
        get => _buttonColor;
        set { _buttonColor = value; Mark(nameof(ButtonColor)); }
    }

    private Color _listBorderColor = default;
    public Color ListBorderColor
    {
        get => _listBorderColor;
        set { _listBorderColor = value; Mark(nameof(ListBorderColor)); }
    }

    private Color _controlBorderColorOff = default;
    public Color ControlBorderColorOff
    {
        get => _controlBorderColorOff;
        set { _controlBorderColorOff = value; Mark(nameof(ControlBorderColorOff)); }
    }

    private Color _controlAccent = default;
    public Color ControlAccent
    {
        get => _controlAccent;
        set { _controlAccent = value; Mark(nameof(ControlAccent)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (AlertToken)target;
        if (IsSpecified(nameof(BackdropColor))) token.BackdropColor = BackdropColor;
        if (IsSpecified(nameof(BackdropOpacity))) token.BackdropOpacity = BackdropOpacity;
        if (IsSpecified(nameof(MinWidth))) token.MinWidth = MinWidth;
        if (IsSpecified(nameof(MaxWidth))) token.MaxWidth = MaxWidth;
        if (IsSpecified(nameof(BorderRadius))) token.BorderRadius = BorderRadius;
        if (IsSpecified(nameof(BoxShadow))) token.BoxShadow = new(BoxShadow);
        if (IsSpecified(nameof(HeadPaddingY))) token.HeadPaddingY = HeadPaddingY;
        if (IsSpecified(nameof(HeadPaddingX))) token.HeadPaddingX = HeadPaddingX;
        if (IsSpecified(nameof(HeadTextAlign))) token.HeadTextAlign = HeadTextAlign;
        if (IsSpecified(nameof(TitleFontSize))) token.TitleFontSize = TitleFontSize;
        if (IsSpecified(nameof(TitleFontWeight))) token.TitleFontWeight = TitleFontWeight;
        if (IsSpecified(nameof(SubTitleFontSize))) token.SubTitleFontSize = SubTitleFontSize;
        if (IsSpecified(nameof(MessagePaddingY))) token.MessagePaddingY = MessagePaddingY;
        if (IsSpecified(nameof(MessagePaddingX))) token.MessagePaddingX = MessagePaddingX;
        if (IsSpecified(nameof(MessageFontSize))) token.MessageFontSize = MessageFontSize;
        if (IsSpecified(nameof(ButtonGroupPadding))) token.ButtonGroupPadding = ButtonGroupPadding;
        if (IsSpecified(nameof(ButtonGroupJustify))) token.ButtonGroupJustify = ButtonGroupJustify;
        if (IsSpecified(nameof(ButtonGroupFlexWrap))) token.ButtonGroupFlexWrap = ButtonGroupFlexWrap;
        if (IsSpecified(nameof(ButtonFontSize))) token.ButtonFontSize = ButtonFontSize;
        if (IsSpecified(nameof(ButtonFontWeight))) token.ButtonFontWeight = ButtonFontWeight;
        if (IsSpecified(nameof(ButtonBorderRadius))) token.ButtonBorderRadius = ButtonBorderRadius;
        if (IsSpecified(nameof(ButtonTextTransform))) token.ButtonTextTransform = ButtonTextTransform;
        if (IsSpecified(nameof(ButtonPadding))) token.ButtonPadding = ButtonPadding;
        if (IsSpecified(nameof(ButtonMarginX))) token.ButtonMarginX = ButtonMarginX;
        if (IsSpecified(nameof(TappableHeight))) token.TappableHeight = TappableHeight;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(TitleColor))) token.TitleColor = TitleColor;
        if (IsSpecified(nameof(SubTitleColor))) token.SubTitleColor = SubTitleColor;
        if (IsSpecified(nameof(MessageColor))) token.MessageColor = MessageColor;
        if (IsSpecified(nameof(ButtonColor))) token.ButtonColor = ButtonColor;
        if (IsSpecified(nameof(ListBorderColor))) token.ListBorderColor = ListBorderColor;
        if (IsSpecified(nameof(ControlBorderColorOff))) token.ControlBorderColorOff = ControlBorderColorOff;
        if (IsSpecified(nameof(ControlAccent))) token.ControlAccent = ControlAccent;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, BackdropColor);
        AppendValue(builder, BackdropOpacity);
        AppendValue(builder, Background);
        AppendValue(builder, BorderRadius);
        AppendValue(builder, BoxShadow);
        AppendValue(builder, ButtonBorderRadius);
        AppendValue(builder, ButtonColor);
        AppendValue(builder, ButtonFontSize);
        AppendValue(builder, ButtonFontWeight);
        AppendValue(builder, ButtonGroupFlexWrap);
        AppendValue(builder, ButtonGroupJustify);
        AppendValue(builder, ButtonGroupPadding);
        AppendValue(builder, ButtonMarginX);
        AppendValue(builder, ButtonPadding);
        AppendValue(builder, ButtonTextTransform);
        AppendValue(builder, ControlAccent);
        AppendValue(builder, ControlBorderColorOff);
        AppendValue(builder, HeadPaddingX);
        AppendValue(builder, HeadPaddingY);
        AppendValue(builder, HeadTextAlign);
        AppendValue(builder, ListBorderColor);
        AppendValue(builder, MaxWidth);
        AppendValue(builder, MessageColor);
        AppendValue(builder, MessageFontSize);
        AppendValue(builder, MessagePaddingX);
        AppendValue(builder, MessagePaddingY);
        AppendValue(builder, MinWidth);
        AppendValue(builder, SubTitleColor);
        AppendValue(builder, SubTitleFontSize);
        AppendValue(builder, TappableHeight);
        AppendValue(builder, TitleColor);
        AppendValue(builder, TitleFontSize);
        AppendValue(builder, TitleFontWeight);
    }
}
