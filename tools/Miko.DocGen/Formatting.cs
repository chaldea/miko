using System.Collections;
using System.Globalization;
using System.Reflection;
using Miko.Common;

namespace Miko.DocGen;

/// <summary>C#-style type names and default values as they should read in the docs.</summary>
public static class Formatting
{
    private static readonly Dictionary<Type, string> Aliases = new()
    {
        [typeof(string)] = "string",
        [typeof(object)] = "object",
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(short)] = "short",
        [typeof(int)] = "int",
        [typeof(long)] = "long",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(char)] = "char",
        [typeof(void)] = "void",
    };

    private static readonly NullabilityInfoContext Nullability = new();

    public static string TypeName(PropertyInfo property) =>
        TypeName(property.PropertyType, Nullability.Create(property));

    public static string TypeName(ParameterInfo parameter) =>
        TypeName(parameter.ParameterType, Nullability.Create(parameter));

    public static string TypeName(Type type, NullabilityInfo? nullability = null)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return TypeName(underlying, nullability?.GenericTypeArguments.FirstOrDefault()) + "?";

        string name;
        if (type.IsByRef)
            name = TypeName(type.GetElementType()!, nullability?.ElementType);
        else if (type.IsArray)
            name = TypeName(type.GetElementType()!, nullability?.ElementType) + "[]";
        else if (type.IsGenericType)
        {
            var arguments = type.GetGenericArguments();
            var argumentNullability = nullability?.GenericTypeArguments;
            var formatted = arguments.Select((argument, i) =>
                TypeName(argument, argumentNullability is { Length: > 0 } && i < argumentNullability.Length ? argumentNullability[i] : null));
            var simple = type.Name[..type.Name.IndexOf('`')];
            name = $"{simple}<{string.Join(", ", formatted)}>";
        }
        else
            name = Aliases.TryGetValue(type, out var alias) ? alias : type.Name;

        if (!type.IsValueType && !type.IsGenericParameter && nullability?.ReadState == NullabilityState.Nullable)
            name += "?";
        return name;
    }

    /// <summary>
    /// Renders a value the way it would be written in C# (or, for theme values, the CSS-like
    /// shorthand the token accepts). Null when the value has no meaningful short form.
    /// </summary>
    public static string? Value(object? value)
    {
        switch (value)
        {
            case null:
                return "null";
            case string text:
                return $"\"{text}\"";
            case bool flag:
                return flag ? "true" : "false";
            case Enum member:
                return EnumValue(member);
            case float number:
                return number.ToString("0.###", CultureInfo.InvariantCulture);
            case double number:
                return number.ToString("0.###", CultureInfo.InvariantCulture);
            case IFormattable formattable when value.GetType().IsPrimitive || value is decimal:
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            case Length length:
                return length.ToString();
            case Color color:
                return ColorValue(color);
            case BoxShadow shadow:
                return ShadowValue(shadow);
            case TimeSpan span:
                return span.ToString();
            case IEnumerable values:
            {
                var items = values.Cast<object?>().Select(Value).ToList();
                if (items.Count == 0)
                    return value is IEnumerable<BoxShadow> ? "none" : "[]";
                if (items.Any(item => item is null))
                    return null;
                return value is IEnumerable<BoxShadow> ? string.Join(", ", items) : $"[{string.Join(", ", items)}]";
            }
        }

        var toString = value.GetType().GetMethod(nameof(ToString), Type.EmptyTypes);
        return toString?.DeclaringType == typeof(object) || toString?.DeclaringType == typeof(ValueType)
            ? null
            : value.ToString();
    }

    private static string EnumValue(Enum member)
    {
        var type = member.GetType();
        if (Enum.IsDefined(type, member))
            return $"{type.Name}.{member}";
        // Flags: "A, B" → "Type.A | Type.B".
        return string.Join(" | ", member.ToString().Split(", ").Select(part => $"{type.Name}.{part}"));
    }

    private static string ColorValue(Color color)
    {
        if (color.A == 0 && color.R == 0 && color.G == 0 && color.B == 0)
            return "transparent";
        if (color.A == 255)
            return $"#{color.R:x2}{color.G:x2}{color.B:x2}";
        var alpha = (color.A / 255f).ToString("0.##", CultureInfo.InvariantCulture);
        return $"rgba({color.R}, {color.G}, {color.B}, {alpha})";
    }

    private static string ShadowValue(BoxShadow shadow)
    {
        static string Px(float value) => value == 0 ? "0" : value.ToString("0.###", CultureInfo.InvariantCulture) + "px";
        var text = $"{Px(shadow.OffsetX)} {Px(shadow.OffsetY)} {Px(shadow.BlurRadius)} {Px(shadow.SpreadRadius)} {ColorValue(shadow.Color)}";
        return shadow.Inset ? "inset " + text : text;
    }
}
