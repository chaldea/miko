using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Fab styles.</summary>
public sealed class FabToken : IonicToken
{
    private float _size = 56f;
    public float Size
    {
        get => _size;
        set { _size = value; Mark(nameof(Size)); }
    }

    private float _smallSize = 40f;
    public float SmallSize
    {
        get => _smallSize;
        set { _smallSize = value; Mark(nameof(SmallSize)); }
    }

    private float _contentMargin = 10f;
    public float ContentMargin
    {
        get => _contentMargin;
        set { _contentMargin = value; Mark(nameof(ContentMargin)); }
    }

    private float _listMargin = 10f;
    public float ListMargin
    {
        get => _listMargin;
        set { _listMargin = value; Mark(nameof(ListMargin)); }
    }

    private float _buttonSmallMargin = 8f;
    public float ButtonSmallMargin
    {
        get => _buttonSmallMargin;
        set { _buttonSmallMargin = value; Mark(nameof(ButtonSmallMargin)); }
    }

    private List<BoxShadow> _boxShadow = new();
    public List<BoxShadow> BoxShadow
    {
        get { Mark(nameof(BoxShadow)); return _boxShadow; }
        set { _boxShadow = value; Mark(nameof(BoxShadow)); }
    }

    private float _iconFontSize = 24f;
    public float IconFontSize
    {
        get => _iconFontSize;
        set { _iconFontSize = value; Mark(nameof(IconFontSize)); }
    }

    private float _listButtonIconSize = 18f;
    public float ListButtonIconSize
    {
        get => _listButtonIconSize;
        set { _listButtonIconSize = value; Mark(nameof(ListButtonIconSize)); }
    }

    private float _transitionDuration = 0.30f;
    public float TransitionDuration
    {
        get => _transitionDuration;
        set { _transitionDuration = value; Mark(nameof(TransitionDuration)); }
    }

    private float _hoverOpacity = 0.08f;
    public float HoverOpacity
    {
        get => _hoverOpacity;
        set { _hoverOpacity = value; Mark(nameof(HoverOpacity)); }
    }

    private Color _background = default;
    public Color Background
    {
        get => _background;
        set { _background = value; Mark(nameof(Background)); }
    }

    private Color _color = default;
    public Color Color
    {
        get => _color;
        set { _color = value; Mark(nameof(Color)); }
    }

    private Color _listButtonBackground = default;
    public Color ListButtonBackground
    {
        get => _listButtonBackground;
        set { _listButtonBackground = value; Mark(nameof(ListButtonBackground)); }
    }

    private Color _listButtonColor = default;
    public Color ListButtonColor
    {
        get => _listButtonColor;
        set { _listButtonColor = value; Mark(nameof(ListButtonColor)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (FabToken)target;
        if (IsSpecified(nameof(Size))) token.Size = Size;
        if (IsSpecified(nameof(SmallSize))) token.SmallSize = SmallSize;
        if (IsSpecified(nameof(ContentMargin))) token.ContentMargin = ContentMargin;
        if (IsSpecified(nameof(ListMargin))) token.ListMargin = ListMargin;
        if (IsSpecified(nameof(ButtonSmallMargin))) token.ButtonSmallMargin = ButtonSmallMargin;
        if (IsSpecified(nameof(BoxShadow))) token.BoxShadow = new(BoxShadow);
        if (IsSpecified(nameof(IconFontSize))) token.IconFontSize = IconFontSize;
        if (IsSpecified(nameof(ListButtonIconSize))) token.ListButtonIconSize = ListButtonIconSize;
        if (IsSpecified(nameof(TransitionDuration))) token.TransitionDuration = TransitionDuration;
        if (IsSpecified(nameof(HoverOpacity))) token.HoverOpacity = HoverOpacity;
        if (IsSpecified(nameof(Background))) token.Background = Background;
        if (IsSpecified(nameof(Color))) token.Color = Color;
        if (IsSpecified(nameof(ListButtonBackground))) token.ListButtonBackground = ListButtonBackground;
        if (IsSpecified(nameof(ListButtonColor))) token.ListButtonColor = ListButtonColor;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Background);
        AppendValue(builder, BoxShadow);
        AppendValue(builder, ButtonSmallMargin);
        AppendValue(builder, Color);
        AppendValue(builder, ContentMargin);
        AppendValue(builder, HoverOpacity);
        AppendValue(builder, IconFontSize);
        AppendValue(builder, ListButtonBackground);
        AppendValue(builder, ListButtonColor);
        AppendValue(builder, ListButtonIconSize);
        AppendValue(builder, ListMargin);
        AppendValue(builder, Size);
        AppendValue(builder, SmallSize);
        AppendValue(builder, TransitionDuration);
    }
}
