using System.Globalization;
using System.Reflection;
using Miko.Common;
using Miko.Components;

namespace Miko.Docs.Demo;

public enum DemoPropertyKind
{
    Boolean,
    Option,
    Number,
    Text,
}

public sealed record DemoOption(string Label, object? Value);

/// <summary>A public component parameter that can be edited without a custom property panel.</summary>
public sealed class DemoProperty
{
    private static readonly HashSet<string> ContentNames =
    [
        "Text", "Label", "Message", "Header", "SubHeader", "Title", "Placeholder", "Value", "Name", "Icon",
    ];

    private DemoProperty(PropertyInfo property)
    {
        Property = property;
        ValueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        IsNullable = !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) is not null;
        Kind = ResolveKind(ValueType);
        Options = ResolveOptions(property, ValueType, IsNullable);
        if (Options is { Count: > 0 }) Kind = DemoPropertyKind.Option;
        (Minimum, Maximum, Step) = ResolveNumberRange(property, ValueType);
    }

    public PropertyInfo Property { get; }
    public string Name => Property.Name;
    public string DisplayName => SplitName(Name);
    public string Group => ResolveGroup(Name, Kind);
    public Type ValueType { get; }
    public bool IsNullable { get; }
    public DemoPropertyKind Kind { get; }
    public IReadOnlyList<DemoOption>? Options { get; }
    public double Minimum { get; }
    public double Maximum { get; }
    public double Step { get; }

    public object? ConvertText(string? text)
    {
        if (ValueType == typeof(string)) return text;
        if (string.IsNullOrWhiteSpace(text))
        {
            if (IsNullable) return null;
            if (ValueType == typeof(bool)) return false;
            if (ValueType.IsEnum) return Enum.GetValues(ValueType).GetValue(0);
            if (ValueType == typeof(Color)) return default(Color);
            if (ValueType == typeof(Length)) return default(Length);
            return Convert.ChangeType(0, ValueType, CultureInfo.InvariantCulture);
        }
        if (ValueType == typeof(Color)) return Color.FromHex(text);
        if (ValueType == typeof(Length)) return ParseLength(text);
        return Convert.ChangeType(text, ValueType, CultureInfo.InvariantCulture);
    }

    private static Length ParseLength(string text)
    {
        var value = text.Trim();
        if (value.Equals("auto", StringComparison.OrdinalIgnoreCase)) return Length.Auto;
        if (value.Equals("fit-content", StringComparison.OrdinalIgnoreCase)) return Length.FitContent;
        if (TryNumber(value, "rem", out var number)) return Length.Rem(number);
        if (TryNumber(value, "em", out number)) return Length.Em(number);
        if (TryNumber(value, "px", out number)) return Length.Px(number);
        if (TryNumber(value, "%", out number)) return Length.Percent(number);
        if (TryNumber(value, "vw", out number)) return Length.Vw(number);
        if (TryNumber(value, "vh", out number)) return Length.Vh(number);
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return Length.Px(number);
        throw new FormatException($"‘{text}’ is not a supported CSS length.");
    }

    private static bool TryNumber(string text, string unit, out float number)
    {
        number = 0;
        return text.EndsWith(unit, StringComparison.OrdinalIgnoreCase) &&
               float.TryParse(text[..^unit.Length], NumberStyles.Float, CultureInfo.InvariantCulture, out number);
    }

    public static string FormatValue(object? value) => value switch
    {
        null => "default",
        bool boolean => boolean ? "true" : "false",
        Color color => color.A == 255 ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    public static DemoProperty? Create(PropertyInfo property)
    {
        if (!property.CanRead || !property.CanWrite || property.SetMethod?.IsPublic != true ||
            property.GetIndexParameters().Length != 0 ||
            property.GetCustomAttribute<ParameterAttribute>(inherit: true) is null ||
            property.Name == nameof(ComponentBase.ChildContent) ||
            IsEventCallback(property.PropertyType))
        {
            return null;
        }

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        if (type != typeof(string) && type != typeof(bool) && !type.IsEnum &&
            type != typeof(byte) && type != typeof(short) && type != typeof(int) && type != typeof(long) &&
            type != typeof(float) && type != typeof(double) && type != typeof(decimal) &&
            type != typeof(Color) && type != typeof(Length))
        {
            return null;
        }

        return new DemoProperty(property);
    }

    public static bool IsEventCallback(Type type) => type == typeof(EventCallback) ||
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>);

    private static DemoPropertyKind ResolveKind(Type type)
    {
        if (type == typeof(bool)) return DemoPropertyKind.Boolean;
        if (type.IsEnum) return DemoPropertyKind.Option;
        if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
            type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return DemoPropertyKind.Number;
        return DemoPropertyKind.Text;
    }

    private static IReadOnlyList<DemoOption>? ResolveOptions(PropertyInfo property, Type type, bool nullable)
    {
        if (type.IsEnum)
        {
            var options = new List<DemoOption>();
            if (nullable) options.Add(new DemoOption("Default", null));
            options.AddRange(Enum.GetValues(type).Cast<object>()
                .Select(value => new DemoOption(SplitName(Enum.GetName(type, value) ?? value.ToString()!), value)));
            return options;
        }

        if (type == typeof(bool) && nullable)
            return [new DemoOption("Default", null), new DemoOption("False", false), new DemoOption("True", true)];

        if (type != typeof(string)) return null;
        var values = property.Name switch
        {
            "Color" => new string?[] { "primary", "secondary", "tertiary", "success", "warning", "danger", "medium", "dark", "light" },
            "Fill" => ["solid", "outline", "clear"],
            "Size" => ["small", "default", "large"],
            "Shape" => ["default", "round"],
            "Expand" when property.DeclaringType?.Name == "IonAccordionGroup" => ["inset"],
            "Expand" => ["block", "full"],
            "Mode" => ["md", "ios"],
            "Type" when property.DeclaringType?.Name is "IonInput" or "IonTextarea" => ["text", "email", "password", "number", "search", "tel", "url"],
            "Type" when property.DeclaringType?.Name == "IonInputOtp" => ["text", "number"],
            "Type" when property.DeclaringType?.Name == "IonProgressBar" => ["determinate", "indeterminate"],
            "Type" when property.DeclaringType?.Name == "IonButton" => ["button", "submit", "reset"],
            "InputMode" or "Inputmode" => ["none", "text", "decimal", "numeric", "tel", "search", "email", "url"],
            "LabelPlacement" => ["start", "end", "fixed", "stacked", "floating"],
            "Justify" => ["start", "center", "end", "space-between", "space-around", "space-evenly"],
            "Alignment" => ["start", "center", "end"],
            "Position" when property.DeclaringType?.Name == "IonToast" => ["top", "middle", "bottom"],
            "Position" when property.DeclaringType?.Name == "IonLabel" => ["fixed", "floating", "stacked"],
            "Position" => ["top", "bottom"],
            "Side" when property.DeclaringType?.Name == "IonFabList" => ["top", "bottom", "start", "end"],
            "Side" => ["start", "end"],
            "Horizontal" => ["start", "center", "end"],
            "Vertical" => ["top", "center", "bottom"],
            "Slot" => ["start", "end", "fixed", "top", "bottom"],
            "OpenSide" => ["start", "end"],
            "FixedSlotPlacement" => ["before", "after"],
            "TriggerAction" => ["click", "context-menu", "hover"],
            "ShowCancelButton" or "ShowClearButton" => ["none", "focus", "always"],
            "Interface" => ["alert", "action-sheet", "popover", "modal"],
            "Presentation" => ["date", "time", "date-time", "month", "year", "month-year"],
            "Effect" => ["slide", "fade"],
            "Lines" => ["full", "inset", "none"],
            "RouterDirection" => ["forward", "back", "root"],
            "HourCycle" => ["h11", "h12", "h23", "h24"],
            "Layout" => ["icon-top", "icon-bottom", "icon-start", "icon-end", "icon-only"],
            "EnterAnimation" or "LeaveAnimation" => null,
            _ => null,
        };
        if (values is null) return null;
        var result = new List<DemoOption>();
        if (nullable) result.Add(new DemoOption("None", null));
        result.AddRange(values.Select(value => new DemoOption(SplitName(value!), value)));
        return result;
    }

    private static (double Min, double Max, double Step) ResolveNumberRange(PropertyInfo property, Type type)
    {
        var name = property.Name;
        var integer = type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long);
        if (name is "Progress" or "Buffer" or "Opacity" or "ZoomingSpeed" or "Volume" ||
            name == "Value" && property.DeclaringType?.Name == "IonProgressBar") return (0, 1, 0.05);
        if (name == "PlaybackRate") return (.25, 4, .25);
        if (name == "LongPressPlaybackRate") return (0, 4, .25);
        if (name == "InitialRotation") return (-360, 360, 15);
        if (name is "MaxAngle" or "TotalAngle") return (0, 360, 15);
        if (name == "InnerRadius") return (0, 200, 5);
        if (name is "MinValue" or "MaxValue") return (-1000, 1000, 1);
        if (name is "Duration" or "AutoplayDelay" or "Speed") return (0, 60000, 100);
        if (name is "Lines" or "Rows" or "Cols" or "Maxlength" or "Minlength") return (0, 100, 1);
        if (name is "Min" or "Max") return (0, 100, integer ? 1 : 0.1);
        return (0, 100, integer ? 1 : 0.1);
    }

    private static string ResolveGroup(string name, DemoPropertyKind kind)
    {
        if (name == "Mode") return "PLATFORM";
        if (name is "Color" or "Fill" or "Size" or "Shape" or "Expand" or "Strong" or "Translucent" ||
            name.Contains("Color", StringComparison.Ordinal))
            return "APPEARANCE";
        if (ContentNames.Contains(name) || name.Contains("Placeholder", StringComparison.Ordinal)) return "CONTENT";
        if (kind == DemoPropertyKind.Boolean) return "BEHAVIOR";
        return "PROPERTIES";
    }

    private static string SplitName(string value)
    {
        if (value.Length == 0) return value;
        var chars = new List<char>(value.Length + 8) { value[0] };
        for (var i = 1; i < value.Length; i++)
        {
            var wordBoundary = char.IsUpper(value[i]) &&
                (!char.IsUpper(value[i - 1]) || i + 1 < value.Length && char.IsLower(value[i + 1]));
            if (wordBoundary) chars.Add(' ');
            chars.Add(value[i]);
        }
        return new string(chars.ToArray());
    }
}
