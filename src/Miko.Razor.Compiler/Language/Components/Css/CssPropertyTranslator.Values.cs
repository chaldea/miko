// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

internal sealed partial class CssPropertyTranslator
{
    [Flags]
    private enum LengthOptions
    {
        None = 0,
        Auto = 1,
        Percent = 2,
        Negative = 4,
        /// <summary>A unitless number is a factor of the font size (line-height).</summary>
        Unitless = 8,
        FitContent = 16,
        /// <summary>min-content / max-content, approximated as auto.</summary>
        Intrinsic = 32,
        AutoIsInitial = 64,
        NoneIsInitial = 128,
        NormalIsInitial = 256,
        BorderWidth = 512,
        FontSize = 1024,
        PercentIsEm = 2048,

        Size = Auto | Percent | FitContent | Intrinsic,
        Offset = Auto | Percent | Negative,
    }

    /// <summary>What a <c>var()</c> fallback must be.</summary>
    private enum VarKind { Length, Color, Number, Integer, Enum, String }

    internal static string Float(double value)
    {
        var text = ((float)value).ToString("R", CultureInfo.InvariantCulture);
        return text + "f";
    }

    private static string Int(double value) => ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);

    private static string Placeholder(CssValue value) => CssPlaceholder.Encode(value.Token.Index);

    private string? Single(List<CssValue> components, Func<CssValue, string?> parse)
    {
        if (components.Count != 1) return Fail(components.Count == 0 ? "the value is empty." : "expected a single value.");
        return parse(components[0]);
    }

    private string? Length(CssValue value, LengthOptions options)
    {
        if (value.IsFunction)
        {
            switch (value.FunctionName)
            {
                case "calc": return CalcLength(value, options);
                case "var": return VarRef(value, VarKind.Length);
                case "env": return Env(value);
                case "min" or "max" or "clamp": return Fail("min(), max() and clamp() are not supported.");
                default: return Fail($"'{value.FunctionName}()' is not supported here.");
            }
        }

        var token = value.Token;
        switch (token.Kind)
        {
            case CssTokenKind.Placeholder:
                if (token.Value.Length == 0 && (options & LengthOptions.Unitless) != 0)
                    return $"{LengthType}.Number({BuilderType}.CssNumber({Placeholder(value)}))";
                return DynamicLength(value);

            case CssTokenKind.Number:
                if ((options & LengthOptions.Unitless) != 0)
                {
                    if (token.Number < 0) return Fail("negative values are not allowed.");
                    return $"{LengthType}.Number({Float(token.Number)})";
                }

                if (token.Number == 0) return $"{LengthType}.Px(0f)";
                return Fail($"'{Snippet(value)}' needs a unit (only 0 may omit it).");

            case CssTokenKind.Percentage:
                if ((options & (LengthOptions.Percent | LengthOptions.PercentIsEm)) == 0) return Fail("percentages are not supported here.");
                if (token.Number < 0 && (options & LengthOptions.Negative) == 0) return Fail("negative values are not allowed.");
                return (options & LengthOptions.PercentIsEm) != 0
                    ? $"{LengthType}.Em({Float(token.Number / 100)})"
                    : $"{LengthType}.Percent({Float(token.Number)})";

            case CssTokenKind.Dimension:
                if (token.Number < 0 && (options & LengthOptions.Negative) == 0) return Fail("negative values are not allowed.");
                return Unit(token.Number, token.Value);

            case CssTokenKind.Ident:
                return LengthKeyword(value, token.Value.ToLowerInvariant(), options);

            default:
                return Fail($"'{Snippet(value)}' is not a length.");
        }
    }

    private string? LengthKeyword(CssValue value, string keyword, LengthOptions options)
    {
        switch (keyword)
        {
            case "auto" when (options & LengthOptions.AutoIsInitial) != 0:
            case "none" when (options & LengthOptions.NoneIsInitial) != 0:
            case "normal" when (options & LengthOptions.NormalIsInitial) != 0:
                return CssType + ".Initial";
            case "auto" when (options & LengthOptions.Auto) != 0:
                return $"{LengthType}.Auto";
            case "fit-content" when (options & LengthOptions.FitContent) != 0:
                return $"{LengthType}.FitContent";
            case "min-content" or "max-content" or "content" when (options & LengthOptions.Intrinsic) != 0:
                Note($"'{keyword}' is not supported and is treated as 'auto'.");
                return $"{LengthType}.Auto";
            case "thin" when (options & LengthOptions.BorderWidth) != 0:
                return $"{LengthType}.Px(1f)";
            case "medium" when (options & LengthOptions.BorderWidth) != 0:
                return $"{LengthType}.Px(3f)";
            case "thick" when (options & LengthOptions.BorderWidth) != 0:
                return $"{LengthType}.Px(5f)";
        }

        if ((options & LengthOptions.FontSize) != 0)
        {
            var px = keyword switch
            {
                "xx-small" => 9.0, "x-small" => 10.0, "small" => 13.0, "medium" => 16.0, "large" => 18.0,
                "x-large" => 24.0, "xx-large" => 32.0, "xxx-large" => 48.0, _ => -1.0,
            };
            if (px > 0) return $"{LengthType}.Px({Float(px)})";
            if (keyword == "larger") return $"{LengthType}.Em(1.2f)";
            if (keyword == "smaller") return $"{LengthType}.Em({Float(1 / 1.2)})";
        }

        return Fail($"'{Snippet(value)}' is not supported here.");
    }

    private string? Unit(double number, string unit)
    {
        var n = Float(number);
        switch (unit.ToLowerInvariant())
        {
            case "px": return $"{LengthType}.Px({n})";
            case "em": return $"{LengthType}.Em({n})";
            case "rem": return $"{LengthType}.Rem({n})";
            case "vw" or "dvw" or "svw" or "lvw": return $"{LengthType}.Vw({n})";
            case "vh" or "dvh" or "svh" or "lvh": return $"{LengthType}.Vh({n})";
            case "pt": return $"{LengthType}.Px({Float(number * 96 / 72)})";
            case "pc": return $"{LengthType}.Px({Float(number * 16)})";
            case "in": return $"{LengthType}.Px({Float(number * 96)})";
            case "cm": return $"{LengthType}.Px({Float(number * 96 / 2.54)})";
            case "mm": return $"{LengthType}.Px({Float(number * 96 / 25.4)})";
            case "q": return $"{LengthType}.Px({Float(number * 96 / 101.6)})";
            default: return Fail($"the unit '{unit}' is not supported.");
        }
    }

    /// <summary><c>@(expr)px</c> in a <c>style</c> attribute.</summary>
    private string? DynamicLength(CssValue value)
    {
        var unit = value.Token.Value.ToLowerInvariant();
        var function = unit switch
        {
            "px" => "Px", "%" => "Percent", "em" => "Em", "rem" => "Rem", "vw" => "Vw", "vh" => "Vh", _ => null,
        };
        if (unit.Length == 0) return FailDynamic("a length needs a unit after the expression, e.g. @(value)px.");
        if (function == null) return FailDynamic($"the unit '{unit}' is not supported after an expression (use px, %, em, rem, vw or vh).");
        return $"{LengthType}.{function}({BuilderType}.CssNumber({Placeholder(value)}))";
    }

    private string? Env(CssValue value)
    {
        var name = CssParser.Components(value.Children!).FirstOrDefault();
        return name?.Token.Value.ToLowerInvariant() switch
        {
            "safe-area-inset-top" => $"{LengthType}.SafeAreaInsetTop",
            "safe-area-inset-right" => $"{LengthType}.SafeAreaInsetRight",
            "safe-area-inset-bottom" => $"{LengthType}.SafeAreaInsetBottom",
            "safe-area-inset-left" => $"{LengthType}.SafeAreaInsetLeft",
            _ => Fail("only env(safe-area-inset-top|right|bottom|left) is supported."),
        };
    }

    /// <summary>opacity, flex-grow, flex-shrink.</summary>
    private string? Number(CssValue value, bool allowPercent = false, bool clampToOne = false)
    {
        if (value.IsFunction && value.FunctionName == "var") return VarRef(value, VarKind.Number);
        if (value.IsFunction && value.FunctionName == "calc")
        {
            var constant = CalcConstant(value);
            return constant is { } c ? Float(clampToOne ? Math.Max(0, Math.Min(1, c)) : c) : null;
        }

        var token = value.Token;
        switch (token.Kind)
        {
            case CssTokenKind.Placeholder when token.Value.Length == 0:
                return $"{BuilderType}.CssNumber({Placeholder(value)})";
            case CssTokenKind.Number when token.Number >= 0 || !clampToOne:
                if (token.Number < 0) return Fail("negative values are not allowed.");
                return Float(clampToOne ? Math.Min(1, token.Number) : token.Number);
            case CssTokenKind.Number:
                return Float(0);
            case CssTokenKind.Percentage when allowPercent:
                return Float(Math.Max(0, Math.Min(1, token.Number / 100)));
            default:
                return Fail($"'{Snippet(value)}' is not a number.");
        }
    }

    /// <summary>z-index, order.</summary>
    private string? Integer(CssValue value)
    {
        if (value.IsFunction && value.FunctionName == "var") return VarRef(value, VarKind.Integer);
        var token = value.Token;
        return token.Kind switch
        {
            CssTokenKind.Placeholder when token.Value.Length == 0 => $"(int){BuilderType}.CssNumber({Placeholder(value)})",
            CssTokenKind.Number when token.IsInteger => Int(token.Number),
            _ => Fail($"'{Snippet(value)}' is not an integer."),
        };
    }

    /// <summary>
    /// A keyword of an enum-valued property. <paramref name="map"/>: <c>keyword:Member</c>, or
    /// <c>keyword:Member:replacement</c> for a keyword that is approximated by <c>replacement</c>.
    /// </summary>
    private string? Enum(CssValue value, string enumType, Dictionary<string, string> map)
    {
        if (value.IsFunction && value.FunctionName == "var") return VarRef(value, VarKind.Enum, enumType);
        if (value.Kind == CssTokenKind.Placeholder)
            return FailDynamic("C# expressions are only supported for lengths, numbers and colors here; bind a Style object for other values.");
        if (value.Kind != CssTokenKind.Ident || value.Children != null) return Fail($"'{Snippet(value)}' is not supported.");
        return EnumKeyword(value.Token.Value.ToLowerInvariant(), enumType, map);
    }

    private string? EnumKeyword(string keyword, string enumType, Dictionary<string, string> map)
    {
        if (!map.TryGetValue(keyword, out var entry)) return Fail($"'{keyword}' is not supported.");
        var parts = entry.Split(':');
        if (parts.Length > 1) Note($"'{keyword}' is not supported and is treated as '{parts[1]}'.");
        return $"{enumType}.{parts[0]}";
    }

    private static Dictionary<string, string> Map(string entries)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in entries.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var colon = entry.IndexOf(':');
            map[entry.Substring(0, colon)] = entry.Substring(colon + 1);
        }

        return map;
    }

    /// <summary><c>var(--name[, fallback])</c> → <c>Css.Var("--name"[, fallback])</c>.</summary>
    private string? VarRef(CssValue value, VarKind kind, string? enumType = null)
    {
        var args = value.Children!;
        var comma = args.FindIndex(a => a.IsComma);
        var head = CssParser.Components(comma < 0 ? args : args.GetRange(0, comma));
        if (head.Count != 1 || head[0].Kind != CssTokenKind.Ident || !head[0].Token.Value.StartsWith("--", StringComparison.Ordinal))
            return Fail("var() needs a custom property name (--name).");

        var name = Str(head[0].Token.Value);
        if (comma < 0) return $"{CssType}.Var({name})";

        var fallback = CssParser.Components(args.GetRange(comma + 1, args.Count - comma - 1));
        if (fallback.Count != 1 || fallback[0].ContainsFunction("var") || fallback[0].ContainsFunction("calc") || fallback[0].ContainsPlaceholder())
            return Fail("the fallback of var() must be a single plain value.");

        var f = fallback[0];
        var expression = kind switch
        {
            VarKind.Length => Length(f, LengthOptions.Auto | LengthOptions.Percent | LengthOptions.Negative | LengthOptions.FitContent),
            VarKind.Color => Color(f),
            VarKind.Number => f.Kind == CssTokenKind.Number ? Float(f.Token.Number) : Fail("the fallback of var() must be a number."),
            VarKind.Integer => f.Kind == CssTokenKind.Number && f.Token.IsInteger ? Int(f.Token.Number) : Fail("the fallback of var() must be an integer."),
            VarKind.String => f.Kind is CssTokenKind.String or CssTokenKind.Ident ? Str(f.Token.Value) : Fail("the fallback of var() must be a string."),
            _ => null,
        };

        if (kind == VarKind.Enum)
        {
            return Fail("var() with a fallback is not supported for keyword values.");
        }

        return expression == null || expression.StartsWith(CssType, StringComparison.Ordinal)
            ? Fail("the fallback of var() must be a single plain value.")
            : $"{CssType}.Var({name}, ({VarValueType})({expression}))";
    }

    /// <summary>A font-family list: quoted names unquoted, the families joined with ", ".</summary>
    private static string FontFamilyList(List<CssValue> values)
    {
        var families = new List<string>();
        foreach (var part in CssParser.SplitByComma(values))
        {
            var names = CssParser.Components(part).Select(v => v.Token.Value);
            families.Add(string.Join(" ", names));
        }

        return string.Join(", ", families);
    }
}
