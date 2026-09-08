using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Searchbar styles.</summary>
public sealed class SearchbarToken : IonicToken
{
    private float _paddingTop = 8f;
    public float PaddingTop
    {
        get => _paddingTop;
        set { _paddingTop = value; Mark(nameof(PaddingTop)); }
    }

    private float _paddingEnd = 8f;
    public float PaddingEnd
    {
        get => _paddingEnd;
        set { _paddingEnd = value; Mark(nameof(PaddingEnd)); }
    }

    private float _paddingBottom = 8f;
    public float PaddingBottom
    {
        get => _paddingBottom;
        set { _paddingBottom = value; Mark(nameof(PaddingBottom)); }
    }

    private float _paddingStart = 8f;
    public float PaddingStart
    {
        get => _paddingStart;
        set { _paddingStart = value; Mark(nameof(PaddingStart)); }
    }

    private float _inputBorderRadius = 2f;
    public float InputBorderRadius
    {
        get => _inputBorderRadius;
        set { _inputBorderRadius = value; Mark(nameof(InputBorderRadius)); }
    }

    private List<BoxShadow> _inputBoxShadow = new();
    public List<BoxShadow> InputBoxShadow
    {
        get { Mark(nameof(InputBoxShadow)); return _inputBoxShadow; }
        set { _inputBoxShadow = value; Mark(nameof(InputBoxShadow)); }
    }

    private float _inputFontSize = 16f;
    public float InputFontSize
    {
        get => _inputFontSize;
        set { _inputFontSize = value; Mark(nameof(InputFontSize)); }
    }

    private Length _inputHeight = Length.Auto;
    public Length InputHeight
    {
        get => _inputHeight;
        set { _inputHeight = value; Mark(nameof(InputHeight)); }
    }

    private Length _inputLineHeight = Length.Px(30);
    public Length InputLineHeight
    {
        get => _inputLineHeight;
        set { _inputLineHeight = value; Mark(nameof(InputLineHeight)); }
    }

    private float _searchIconSize = 21f;
    public float SearchIconSize
    {
        get => _searchIconSize;
        set { _searchIconSize = value; Mark(nameof(SearchIconSize)); }
    }

    private float _clearIconSize = 22f;
    public float ClearIconSize
    {
        get => _clearIconSize;
        set { _clearIconSize = value; Mark(nameof(ClearIconSize)); }
    }

    private float _cancelButtonFontSize = 17f;
    public float CancelButtonFontSize
    {
        get => _cancelButtonFontSize;
        set { _cancelButtonFontSize = value; Mark(nameof(CancelButtonFontSize)); }
    }

    private Color _inputBackground = default;
    public Color InputBackground
    {
        get => _inputBackground;
        set { _inputBackground = value; Mark(nameof(InputBackground)); }
    }

    private Color _inputTextColor = default;
    public Color InputTextColor
    {
        get => _inputTextColor;
        set { _inputTextColor = value; Mark(nameof(InputTextColor)); }
    }

    private float _inputMinHeight = default;
    public float InputMinHeight
    {
        get => _inputMinHeight;
        set { _inputMinHeight = value; Mark(nameof(InputMinHeight)); }
    }

    private Color _searchIconColor = default;
    public Color SearchIconColor
    {
        get => _searchIconColor;
        set { _searchIconColor = value; Mark(nameof(SearchIconColor)); }
    }

    private Color _clearIconColor = default;
    public Color ClearIconColor
    {
        get => _clearIconColor;
        set { _clearIconColor = value; Mark(nameof(ClearIconColor)); }
    }

    private Color _cancelButtonColor = default;
    public Color CancelButtonColor
    {
        get => _cancelButtonColor;
        set { _cancelButtonColor = value; Mark(nameof(CancelButtonColor)); }
    }

    private Color _cancelButtonBackground = default;
    public Color CancelButtonBackground
    {
        get => _cancelButtonBackground;
        set { _cancelButtonBackground = value; Mark(nameof(CancelButtonBackground)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (SearchbarToken)target;
        if (IsSpecified(nameof(PaddingTop))) token.PaddingTop = PaddingTop;
        if (IsSpecified(nameof(PaddingEnd))) token.PaddingEnd = PaddingEnd;
        if (IsSpecified(nameof(PaddingBottom))) token.PaddingBottom = PaddingBottom;
        if (IsSpecified(nameof(PaddingStart))) token.PaddingStart = PaddingStart;
        if (IsSpecified(nameof(InputBorderRadius))) token.InputBorderRadius = InputBorderRadius;
        if (IsSpecified(nameof(InputBoxShadow))) token.InputBoxShadow = new(InputBoxShadow);
        if (IsSpecified(nameof(InputFontSize))) token.InputFontSize = InputFontSize;
        if (IsSpecified(nameof(InputHeight))) token.InputHeight = InputHeight;
        if (IsSpecified(nameof(InputLineHeight))) token.InputLineHeight = InputLineHeight;
        if (IsSpecified(nameof(SearchIconSize))) token.SearchIconSize = SearchIconSize;
        if (IsSpecified(nameof(ClearIconSize))) token.ClearIconSize = ClearIconSize;
        if (IsSpecified(nameof(CancelButtonFontSize))) token.CancelButtonFontSize = CancelButtonFontSize;
        if (IsSpecified(nameof(InputBackground))) token.InputBackground = InputBackground;
        if (IsSpecified(nameof(InputTextColor))) token.InputTextColor = InputTextColor;
        if (IsSpecified(nameof(InputMinHeight))) token.InputMinHeight = InputMinHeight;
        if (IsSpecified(nameof(SearchIconColor))) token.SearchIconColor = SearchIconColor;
        if (IsSpecified(nameof(ClearIconColor))) token.ClearIconColor = ClearIconColor;
        if (IsSpecified(nameof(CancelButtonColor))) token.CancelButtonColor = CancelButtonColor;
        if (IsSpecified(nameof(CancelButtonBackground))) token.CancelButtonBackground = CancelButtonBackground;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, CancelButtonBackground);
        AppendValue(builder, CancelButtonColor);
        AppendValue(builder, CancelButtonFontSize);
        AppendValue(builder, ClearIconColor);
        AppendValue(builder, ClearIconSize);
        AppendValue(builder, InputBackground);
        AppendValue(builder, InputBorderRadius);
        AppendValue(builder, InputBoxShadow);
        AppendValue(builder, InputFontSize);
        AppendValue(builder, InputHeight);
        AppendValue(builder, InputLineHeight);
        AppendValue(builder, InputMinHeight);
        AppendValue(builder, InputTextColor);
        AppendValue(builder, PaddingBottom);
        AppendValue(builder, PaddingEnd);
        AppendValue(builder, PaddingStart);
        AppendValue(builder, PaddingTop);
        AppendValue(builder, SearchIconColor);
        AppendValue(builder, SearchIconSize);
    }
}
