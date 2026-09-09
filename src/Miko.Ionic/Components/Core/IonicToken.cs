using System.Collections;
using System.Globalization;
using System.Text;
using Miko.Common;

namespace Miko.Ionic;

/// <summary>Tracks explicitly assigned values so partial themes can inherit mode defaults.</summary>
public abstract class IonicToken
{
    private readonly HashSet<string> _specified = new(StringComparer.Ordinal);

    protected void Mark(string propertyName) => _specified.Add(propertyName);
    protected bool IsSpecified(string propertyName) => _specified.Contains(propertyName);

    internal abstract void CopySpecifiedValuesTo(IonicToken target);
    internal abstract void AppendFingerprint(StringBuilder builder);

    protected static void AppendValue(StringBuilder builder, object? value)
    {
        builder.Append('|');
        switch (value)
        {
            case null: builder.Append("<null>"); break;
            case Color color:
                builder.Append(color.R).Append(',').Append(color.G).Append(',').Append(color.B).Append(',').Append(color.A);
                break;
            case BoxShadow shadow:
                AppendValue(builder, shadow.OffsetX);
                AppendValue(builder, shadow.OffsetY);
                AppendValue(builder, shadow.BlurRadius);
                AppendValue(builder, shadow.SpreadRadius);
                AppendValue(builder, shadow.Color);
                AppendValue(builder, shadow.Inset);
                break;
            case Length length:
                builder.Append(length.ToString().Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, "."));
                break;
            case IEnumerable values when value is not string:
                builder.Append('[');
                foreach (var entry in values) AppendValue(builder, entry);
                builder.Append(']');
                break;
            case IFormattable formatted:
                builder.Append(formatted.ToString(null, CultureInfo.InvariantCulture));
                break;
            default: builder.Append(value); break;
        }
    }
}
