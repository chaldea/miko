using System.ComponentModel;
using System.Globalization;

namespace Miko.Common;

/// <summary>Allows configuration binding from hexadecimal color strings.</summary>
public sealed class ColorConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string text ? Color.FromHex(text.Trim()) : base.ConvertFrom(context, culture, value);

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
        value is Color color && destinationType == typeof(string)
            ? $"#{color.R:x2}{color.G:x2}{color.B:x2}{color.A:x2}"
            : base.ConvertTo(context, culture, value, destinationType);
}
