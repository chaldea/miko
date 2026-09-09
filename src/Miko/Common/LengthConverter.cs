using System.ComponentModel;
using System.Globalization;

namespace Miko.Common;

/// <summary>Allows configuration binding from a single length, such as 12px or 1.5rem.</summary>
public sealed class LengthConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string text)
            return base.ConvertFrom(context, culture, value);

        text = text.Trim();
        if (text.Equals("auto", StringComparison.OrdinalIgnoreCase)) return Length.Auto;
        if (text.Equals("fit-content", StringComparison.OrdinalIgnoreCase)) return Length.FitContent;

        (string Suffix, LengthUnit Unit)[] units =
        [
            ("rem", LengthUnit.Rem), ("px", LengthUnit.Px), ("em", LengthUnit.Em),
            ("%", LengthUnit.Percent), ("vw", LengthUnit.Vw), ("vh", LengthUnit.Vh),
        ];
        foreach (var (suffix, unit) in units)
        {
            if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return new Length(float.Parse(text[..^suffix.Length], NumberStyles.Float,
                    culture ?? CultureInfo.InvariantCulture), unit);
        }

        return Length.Number(float.Parse(text, NumberStyles.Float, culture ?? CultureInfo.InvariantCulture));
    }
}
