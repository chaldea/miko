using Miko.Common;
using Miko.Styling;

namespace Miko.Ionic;

/// <summary>Partial theme overrides for InfiniteScroll styles.</summary>
public sealed class InfiniteScrollToken : IonicToken
{
    private float _contentMinHeight = 84f;
    public float ContentMinHeight
    {
        get => _contentMinHeight;
        set { _contentMinHeight = value; Mark(nameof(ContentMinHeight)); }
    }

    internal override void CopySpecifiedValuesTo(IonicToken target)
    {
        var token = (InfiniteScrollToken)target;
        if (IsSpecified(nameof(ContentMinHeight))) token.ContentMinHeight = ContentMinHeight;
    }

    internal override void AppendFingerprint(System.Text.StringBuilder builder)
    {
        AppendValue(builder, ContentMinHeight);
    }
}
