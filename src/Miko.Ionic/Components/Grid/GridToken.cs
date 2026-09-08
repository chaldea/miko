using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Grid styles.</summary>
public sealed class GridToken : IonicToken
{
    private float _padding = 5f;
    public float Padding
    {
        get => _padding;
        set { _padding = value; Mark(nameof(Padding)); }
    }

    private float _columnPadding = 5f;
    public float ColumnPadding
    {
        get => _columnPadding;
        set { _columnPadding = value; Mark(nameof(ColumnPadding)); }
    }

    private float _fixedWidth = 1140f;
    public float FixedWidth
    {
        get => _fixedWidth;
        set { _fixedWidth = value; Mark(nameof(FixedWidth)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (GridToken)target;
        if (IsSpecified(nameof(Padding))) token.Padding = Padding;
        if (IsSpecified(nameof(ColumnPadding))) token.ColumnPadding = ColumnPadding;
        if (IsSpecified(nameof(FixedWidth))) token.FixedWidth = FixedWidth;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, ColumnPadding);
        AppendValue(builder, FixedWidth);
        AppendValue(builder, Padding);
    }
}
