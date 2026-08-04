using System.Globalization;

namespace Miko.Components;

/// <summary>
/// Converts values between their CLR representation and the string form carried by DOM
/// attributes / change events. Used by the code the Razor compiler generates for
/// <c>@bind</c> on a markup element:
/// <code>
/// &lt;input value="@BindConverter.FormatValue(_text)"
///        onchange="@EventCallback.Factory.CreateBinder(this, __value =&gt; _text = __value, _text)" /&gt;
/// </code>
/// <para>
/// The compiler also probes for this type to decide whether the target assembly supports
/// <c>@bind</c> at all (see <c>BindTagHelperDescriptorProvider</c>), so it must live in the
/// <c>Miko</c> assembly under this exact name.
/// </para>
/// </summary>
public static class BindConverter
{
    /// <summary>Culture used when no explicit culture is supplied by <c>@bind:culture</c>.</summary>
    private static CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    // ---------------------------------------------------------------------
    // FormatValue: CLR value -> attribute string
    // ---------------------------------------------------------------------

    public static string? FormatValue(string? value, CultureInfo? culture = null) => value;

    /// <summary>
    /// Booleans format to the HTML attribute forms <c>"true"</c>/<c>"false"</c> rather than
    /// the culture-sensitive <c>bool.ToString()</c> ("True"/"False").
    /// </summary>
    public static bool FormatValue(bool value, CultureInfo? culture = null) => value;

    public static bool? FormatValue(bool? value, CultureInfo? culture = null) => value;

    public static string FormatValue(int value, CultureInfo? culture = null)
        => value.ToString(culture ?? CurrentCulture);

    public static string? FormatValue(int? value, CultureInfo? culture = null)
        => value?.ToString(culture ?? CurrentCulture);

    public static string FormatValue(long value, CultureInfo? culture = null)
        => value.ToString(culture ?? CurrentCulture);

    public static string? FormatValue(long? value, CultureInfo? culture = null)
        => value?.ToString(culture ?? CurrentCulture);

    public static string FormatValue(short value, CultureInfo? culture = null)
        => value.ToString(culture ?? CurrentCulture);

    public static string? FormatValue(short? value, CultureInfo? culture = null)
        => value?.ToString(culture ?? CurrentCulture);

    public static string FormatValue(float value, CultureInfo? culture = null)
        => value.ToString(culture ?? CurrentCulture);

    public static string? FormatValue(float? value, CultureInfo? culture = null)
        => value?.ToString(culture ?? CurrentCulture);

    public static string FormatValue(double value, CultureInfo? culture = null)
        => value.ToString(culture ?? CurrentCulture);

    public static string? FormatValue(double? value, CultureInfo? culture = null)
        => value?.ToString(culture ?? CurrentCulture);

    public static string FormatValue(decimal value, CultureInfo? culture = null)
        => value.ToString(culture ?? CurrentCulture);

    public static string? FormatValue(decimal? value, CultureInfo? culture = null)
        => value?.ToString(culture ?? CurrentCulture);

    public static string FormatValue(DateTime value, CultureInfo? culture = null)
        => value.ToString(culture ?? CurrentCulture);

    public static string FormatValue(DateTime value, string format, CultureInfo? culture = null)
        => value.ToString(format, culture ?? CurrentCulture);

    public static string? FormatValue(DateTime? value, CultureInfo? culture = null)
        => value?.ToString(culture ?? CurrentCulture);

    public static string? FormatValue(DateTime? value, string format, CultureInfo? culture = null)
        => value?.ToString(format, culture ?? CurrentCulture);

    public static string FormatValue(DateTimeOffset value, CultureInfo? culture = null)
        => value.ToString(culture ?? CurrentCulture);

    public static string FormatValue(DateTimeOffset value, string format, CultureInfo? culture = null)
        => value.ToString(format, culture ?? CurrentCulture);

    public static string? FormatValue(DateTimeOffset? value, CultureInfo? culture = null)
        => value?.ToString(culture ?? CurrentCulture);

    public static string? FormatValue(DateTimeOffset? value, string format, CultureInfo? culture = null)
        => value?.ToString(format, culture ?? CurrentCulture);

    /// <summary>
    /// Fallback for any other type (enums, custom types with a <c>ToString</c>). Chosen by overload
    /// resolution only when none of the specific overloads above apply.
    /// </summary>
    public static string? FormatValue<T>(T value, CultureInfo? culture = null)
        => value switch
        {
            null => null,
            IFormattable formattable => formattable.ToString(null, culture ?? CurrentCulture),
            _ => value.ToString(),
        };

    // ---------------------------------------------------------------------
    // TryConvertTo: attribute / change-event string -> CLR value
    // ---------------------------------------------------------------------

    /// <summary>
    /// Parses <paramref name="obj"/> into <typeparamref name="T"/>. Returns <c>false</c> (with
    /// <paramref name="value"/> left at its default) when the text is not a valid
    /// <typeparamref name="T"/>, which the binder treats as "keep the previous value".
    /// </summary>
    public static bool TryConvertTo<T>(object? obj, CultureInfo? culture, out T value)
    {
        culture ??= CurrentCulture;

        // Already the right type (change events on components pass strongly-typed payloads).
        if (obj is T typed)
        {
            value = typed;
            return true;
        }

        var targetType = typeof(T);
        var underlying = Nullable.GetUnderlyingType(targetType);
        var isNullable = underlying is not null;
        targetType = underlying ?? targetType;

        if (obj is null)
        {
            value = default!;
            // A null/absent value is valid for reference and Nullable<> targets only.
            return isNullable || !typeof(T).IsValueType;
        }

        var text = obj as string ?? obj.ToString();
        if (string.IsNullOrEmpty(text))
        {
            value = default!;
            return isNullable || targetType == typeof(string);
        }

        if (TryParse(text, targetType, culture, out var parsed))
        {
            value = (T)parsed!;
            return true;
        }

        value = default!;
        return false;
    }

    private static bool TryParse(string text, Type targetType, CultureInfo culture, out object? result)
    {
        if (targetType == typeof(string))
        {
            result = text;
            return true;
        }

        if (targetType == typeof(bool))
        {
            // HTML checkbox semantics: "on"/"true" are both truthy.
            if (bool.TryParse(text, out var b)) { result = b; return true; }
            if (string.Equals(text, "on", StringComparison.OrdinalIgnoreCase)) { result = true; return true; }
            result = null;
            return false;
        }

        if (targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, text, ignoreCase: true, out var e)) { result = e; return true; }
            result = null;
            return false;
        }

        if (targetType == typeof(int))
        { var ok = int.TryParse(text, NumberStyles.Integer, culture, out var v); result = ok ? v : null; return ok; }

        if (targetType == typeof(long))
        { var ok = long.TryParse(text, NumberStyles.Integer, culture, out var v); result = ok ? v : null; return ok; }

        if (targetType == typeof(short))
        { var ok = short.TryParse(text, NumberStyles.Integer, culture, out var v); result = ok ? v : null; return ok; }

        if (targetType == typeof(float))
        { var ok = float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var v); result = ok ? v : null; return ok; }

        if (targetType == typeof(double))
        { var ok = double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture, out var v); result = ok ? v : null; return ok; }

        if (targetType == typeof(decimal))
        { var ok = decimal.TryParse(text, NumberStyles.Number, culture, out var v); result = ok ? v : null; return ok; }

        if (targetType == typeof(DateTime))
        { var ok = DateTime.TryParse(text, culture, DateTimeStyles.None, out var v); result = ok ? v : null; return ok; }

        if (targetType == typeof(DateTimeOffset))
        { var ok = DateTimeOffset.TryParse(text, culture, DateTimeStyles.None, out var v); result = ok ? v : null; return ok; }

        if (targetType == typeof(Guid))
        { var ok = Guid.TryParse(text, out var v); result = ok ? v : null; return ok; }

        try
        {
            result = Convert.ChangeType(text, targetType, culture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            result = null;
            return false;
        }
    }
}
