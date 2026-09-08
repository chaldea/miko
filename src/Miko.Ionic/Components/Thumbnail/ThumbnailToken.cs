using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for Thumbnail styles.</summary>
public sealed class ThumbnailToken : IonicToken
{
    private float _size = 48f;
    public float Size
    {
        get => _size;
        set { _size = value; Mark(nameof(Size)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (ThumbnailToken)target;
        if (IsSpecified(nameof(Size))) token.Size = Size;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, Size);
    }
}
