using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Datetime styles.</summary>
public sealed class DatetimeToken : IonicToken
{
    private float _titleFontSize = 12f;
    public float TitleFontSize
    {
        get => _titleFontSize;
        set { _titleFontSize = value; Mark(nameof(TitleFontSize)); }
    }

    private float _selectedDateFontSize = 34f;
    public float SelectedDateFontSize
    {
        get => _selectedDateFontSize;
        set { _selectedDateFontSize = value; Mark(nameof(SelectedDateFontSize)); }
    }

    private float _dayOfWeekFontSize = 14f;
    public float DayOfWeekFontSize
    {
        get => _dayOfWeekFontSize;
        set { _dayOfWeekFontSize = value; Mark(nameof(DayOfWeekFontSize)); }
    }

    private float _daySize = 42f;
    public float DaySize
    {
        get => _daySize;
        set { _daySize = value; Mark(nameof(DaySize)); }
    }

    private float _dayFontSize = 14f;
    public float DayFontSize
    {
        get => _dayFontSize;
        set { _dayFontSize = value; Mark(nameof(DayFontSize)); }
    }

    private Color _buttonBackground = Color.FromHex("edeef0");
    public Color ButtonBackground
    {
        get => _buttonBackground;
        set { _buttonBackground = value; Mark(nameof(ButtonBackground)); }
    }

    private Color _buttonColor = Color.FromHex("000000");
    public Color ButtonColor
    {
        get => _buttonColor;
        set { _buttonColor = value; Mark(nameof(ButtonColor)); }
    }

    private float _buttonBorderRadius = 8f;
    public float ButtonBorderRadius
    {
        get => _buttonBorderRadius;
        set { _buttonBorderRadius = value; Mark(nameof(ButtonBorderRadius)); }
    }

    private float _buttonPaddingY = 6f;
    public float ButtonPaddingY
    {
        get => _buttonPaddingY;
        set { _buttonPaddingY = value; Mark(nameof(ButtonPaddingY)); }
    }

    private float _buttonPaddingX = 12f;
    public float ButtonPaddingX
    {
        get => _buttonPaddingX;
        set { _buttonPaddingX = value; Mark(nameof(ButtonPaddingX)); }
    }

    private float _buttonFontSize = 16f;
    public float ButtonFontSize
    {
        get => _buttonFontSize;
        set { _buttonFontSize = value; Mark(nameof(ButtonFontSize)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    private Color _headerBackground = default;
    public Color HeaderBackground
    {
        get => _headerBackground;
        set { _headerBackground = value; Mark(nameof(HeaderBackground)); }
    }

    private Color _headerColor = default;
    public Color HeaderColor
    {
        get => _headerColor;
        set { _headerColor = value; Mark(nameof(HeaderColor)); }
    }

    private Color _dayOfWeekColor = default;
    public Color DayOfWeekColor
    {
        get => _dayOfWeekColor;
        set { _dayOfWeekColor = value; Mark(nameof(DayOfWeekColor)); }
    }

    private Color _monthYearColor = default;
    public Color MonthYearColor
    {
        get => _monthYearColor;
        set { _monthYearColor = value; Mark(nameof(MonthYearColor)); }
    }

    private Color _dayColor = default;
    public Color DayColor
    {
        get => _dayColor;
        set { _dayColor = value; Mark(nameof(DayColor)); }
    }

    private Color _dayActiveBackground = default;
    public Color DayActiveBackground
    {
        get => _dayActiveBackground;
        set { _dayActiveBackground = value; Mark(nameof(DayActiveBackground)); }
    }

    private Color _dayActiveColor = default;
    public Color DayActiveColor
    {
        get => _dayActiveColor;
        set { _dayActiveColor = value; Mark(nameof(DayActiveColor)); }
    }

    private Color _todayColor = default;
    public Color TodayColor
    {
        get => _todayColor;
        set { _todayColor = value; Mark(nameof(TodayColor)); }
    }

    private Color _buttonActiveColor = default;
    public Color ButtonActiveColor
    {
        get => _buttonActiveColor;
        set { _buttonActiveColor = value; Mark(nameof(ButtonActiveColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (DatetimeToken)target;
        if (IsSpecified(nameof(TitleFontSize))) token.TitleFontSize = TitleFontSize;
        if (IsSpecified(nameof(SelectedDateFontSize))) token.SelectedDateFontSize = SelectedDateFontSize;
        if (IsSpecified(nameof(DayOfWeekFontSize))) token.DayOfWeekFontSize = DayOfWeekFontSize;
        if (IsSpecified(nameof(DaySize))) token.DaySize = DaySize;
        if (IsSpecified(nameof(DayFontSize))) token.DayFontSize = DayFontSize;
        if (IsSpecified(nameof(ButtonBackground))) token.ButtonBackground = ButtonBackground;
        if (IsSpecified(nameof(ButtonColor))) token.ButtonColor = ButtonColor;
        if (IsSpecified(nameof(ButtonBorderRadius))) token.ButtonBorderRadius = ButtonBorderRadius;
        if (IsSpecified(nameof(ButtonPaddingY))) token.ButtonPaddingY = ButtonPaddingY;
        if (IsSpecified(nameof(ButtonPaddingX))) token.ButtonPaddingX = ButtonPaddingX;
        if (IsSpecified(nameof(ButtonFontSize))) token.ButtonFontSize = ButtonFontSize;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(HeaderBackground))) token.HeaderBackground = HeaderBackground;
        if (IsSpecified(nameof(HeaderColor))) token.HeaderColor = HeaderColor;
        if (IsSpecified(nameof(DayOfWeekColor))) token.DayOfWeekColor = DayOfWeekColor;
        if (IsSpecified(nameof(MonthYearColor))) token.MonthYearColor = MonthYearColor;
        if (IsSpecified(nameof(DayColor))) token.DayColor = DayColor;
        if (IsSpecified(nameof(DayActiveBackground))) token.DayActiveBackground = DayActiveBackground;
        if (IsSpecified(nameof(DayActiveColor))) token.DayActiveColor = DayActiveColor;
        if (IsSpecified(nameof(TodayColor))) token.TodayColor = TodayColor;
        if (IsSpecified(nameof(ButtonActiveColor))) token.ButtonActiveColor = ButtonActiveColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, ButtonActiveColor);
        AppendValue(builder, ButtonBackground);
        AppendValue(builder, ButtonBorderRadius);
        AppendValue(builder, ButtonColor);
        AppendValue(builder, ButtonFontSize);
        AppendValue(builder, ButtonPaddingX);
        AppendValue(builder, ButtonPaddingY);
        AppendValue(builder, DayActiveBackground);
        AppendValue(builder, DayActiveColor);
        AppendValue(builder, DayColor);
        AppendValue(builder, DayFontSize);
        AppendValue(builder, DayOfWeekColor);
        AppendValue(builder, DayOfWeekFontSize);
        AppendValue(builder, DaySize);
        AppendValue(builder, HeaderBackground);
        AppendValue(builder, HeaderColor);
        AppendValue(builder, MonthYearColor);
        AppendValue(builder, SelectedDateFontSize);
        AppendValue(builder, TitleFontSize);
        AppendValue(builder, TodayColor);
    }
}
