using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Item styles.</summary>
public sealed class ItemToken : IonicToken
{
    private float _minHeight = 48f;
    public float MinHeight
    {
        get => _minHeight;
        set { _minHeight = value; Mark(nameof(MinHeight)); }
    }

    private float _paddingStart = 16f;
    public float PaddingStart
    {
        get => _paddingStart;
        set { _paddingStart = value; Mark(nameof(PaddingStart)); }
    }

    private float _paddingEnd = 16f;
    public float PaddingEnd
    {
        get => _paddingEnd;
        set { _paddingEnd = value; Mark(nameof(PaddingEnd)); }
    }

    private float _labelMarginVertical = 10f;
    public float LabelMarginVertical
    {
        get => _labelMarginVertical;
        set { _labelMarginVertical = value; Mark(nameof(LabelMarginVertical)); }
    }

    private float _labelMarginEnd = 0f;
    public float LabelMarginEnd
    {
        get => _labelMarginEnd;
        set { _labelMarginEnd = value; Mark(nameof(LabelMarginEnd)); }
    }

    private float _iconSlotMarginVertical = 12f;
    public float IconSlotMarginVertical
    {
        get => _iconSlotMarginVertical;
        set { _iconSlotMarginVertical = value; Mark(nameof(IconSlotMarginVertical)); }
    }

    private float _iconStartSlotMarginEnd = 32f;
    public float IconStartSlotMarginEnd
    {
        get => _iconStartSlotMarginEnd;
        set { _iconStartSlotMarginEnd = value; Mark(nameof(IconStartSlotMarginEnd)); }
    }

    private float _iconEndSlotMarginStart = 16f;
    public float IconEndSlotMarginStart
    {
        get => _iconEndSlotMarginStart;
        set { _iconEndSlotMarginStart = value; Mark(nameof(IconEndSlotMarginStart)); }
    }

    private float _avatarSlotMarginVertical = 8f;
    public float AvatarSlotMarginVertical
    {
        get => _avatarSlotMarginVertical;
        set { _avatarSlotMarginVertical = value; Mark(nameof(AvatarSlotMarginVertical)); }
    }

    private float _avatarStartSlotMarginEnd = 16f;
    public float AvatarStartSlotMarginEnd
    {
        get => _avatarStartSlotMarginEnd;
        set { _avatarStartSlotMarginEnd = value; Mark(nameof(AvatarStartSlotMarginEnd)); }
    }

    private float _avatarEndSlotMarginStart = 16f;
    public float AvatarEndSlotMarginStart
    {
        get => _avatarEndSlotMarginStart;
        set { _avatarEndSlotMarginStart = value; Mark(nameof(AvatarEndSlotMarginStart)); }
    }

    private float _dividerMinHeight = 30f;
    public float DividerMinHeight
    {
        get => _dividerMinHeight;
        set { _dividerMinHeight = value; Mark(nameof(DividerMinHeight)); }
    }

    private float _avatarSize = 40f;
    public float AvatarSize
    {
        get => _avatarSize;
        set { _avatarSize = value; Mark(nameof(AvatarSize)); }
    }

    private float _thumbnailSize = 56f;
    public float ThumbnailSize
    {
        get => _thumbnailSize;
        set { _thumbnailSize = value; Mark(nameof(ThumbnailSize)); }
    }

    private Color _color = default;
    public Color Color
    {
        get => _color;
        set { _color = value; Mark(nameof(Color)); }
    }

    private Color _borderColor = default;
    public Color BorderColor
    {
        get => _borderColor;
        set { _borderColor = value; Mark(nameof(BorderColor)); }
    }

    private Color _dividerBackground = default;
    public Color DividerBackground
    {
        get => _dividerBackground;
        set { _dividerBackground = value; Mark(nameof(DividerBackground)); }
    }

    private Color _dividerColor = default;
    public Color DividerColor
    {
        get => _dividerColor;
        set { _dividerColor = value; Mark(nameof(DividerColor)); }
    }

    private Color _optionColor = default;
    public Color OptionColor
    {
        get => _optionColor;
        set { _optionColor = value; Mark(nameof(OptionColor)); }
    }

    private Color _optionBackground = default;
    public Color OptionBackground
    {
        get => _optionBackground;
        set { _optionBackground = value; Mark(nameof(OptionBackground)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (ItemToken)target;
        if (IsSpecified(nameof(MinHeight))) token.MinHeight = MinHeight;
        if (IsSpecified(nameof(PaddingStart))) token.PaddingStart = PaddingStart;
        if (IsSpecified(nameof(PaddingEnd))) token.PaddingEnd = PaddingEnd;
        if (IsSpecified(nameof(LabelMarginVertical))) token.LabelMarginVertical = LabelMarginVertical;
        if (IsSpecified(nameof(LabelMarginEnd))) token.LabelMarginEnd = LabelMarginEnd;
        if (IsSpecified(nameof(IconSlotMarginVertical))) token.IconSlotMarginVertical = IconSlotMarginVertical;
        if (IsSpecified(nameof(IconStartSlotMarginEnd))) token.IconStartSlotMarginEnd = IconStartSlotMarginEnd;
        if (IsSpecified(nameof(IconEndSlotMarginStart))) token.IconEndSlotMarginStart = IconEndSlotMarginStart;
        if (IsSpecified(nameof(AvatarSlotMarginVertical))) token.AvatarSlotMarginVertical = AvatarSlotMarginVertical;
        if (IsSpecified(nameof(AvatarStartSlotMarginEnd))) token.AvatarStartSlotMarginEnd = AvatarStartSlotMarginEnd;
        if (IsSpecified(nameof(AvatarEndSlotMarginStart))) token.AvatarEndSlotMarginStart = AvatarEndSlotMarginStart;
        if (IsSpecified(nameof(DividerMinHeight))) token.DividerMinHeight = DividerMinHeight;
        if (IsSpecified(nameof(AvatarSize))) token.AvatarSize = AvatarSize;
        if (IsSpecified(nameof(ThumbnailSize))) token.ThumbnailSize = ThumbnailSize;
        if (IsSpecified(nameof(Color))) token.Color = Color;
        if (IsSpecified(nameof(BorderColor))) token.BorderColor = BorderColor;
        if (IsSpecified(nameof(DividerBackground))) token.DividerBackground = DividerBackground;
        if (IsSpecified(nameof(DividerColor))) token.DividerColor = DividerColor;
        if (IsSpecified(nameof(OptionColor))) token.OptionColor = OptionColor;
        if (IsSpecified(nameof(OptionBackground))) token.OptionBackground = OptionBackground;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, AvatarEndSlotMarginStart);
        AppendValue(builder, AvatarSize);
        AppendValue(builder, AvatarSlotMarginVertical);
        AppendValue(builder, AvatarStartSlotMarginEnd);
        AppendValue(builder, BorderColor);
        AppendValue(builder, Color);
        AppendValue(builder, DividerBackground);
        AppendValue(builder, DividerColor);
        AppendValue(builder, DividerMinHeight);
        AppendValue(builder, IconEndSlotMarginStart);
        AppendValue(builder, IconSlotMarginVertical);
        AppendValue(builder, IconStartSlotMarginEnd);
        AppendValue(builder, LabelMarginEnd);
        AppendValue(builder, LabelMarginVertical);
        AppendValue(builder, MinHeight);
        AppendValue(builder, OptionBackground);
        AppendValue(builder, OptionColor);
        AppendValue(builder, PaddingEnd);
        AppendValue(builder, PaddingStart);
        AppendValue(builder, ThumbnailSize);
    }
}
