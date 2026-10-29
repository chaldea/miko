// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

internal sealed partial class CssPropertyTranslator
{
    /// <summary>Parses <paramref name="value"/> without recording a failure: for telling which part of a shorthand a component is.</summary>
    private string? Try(Func<string?> parse)
    {
        var error = _error;
        var dynamic = _dynamic;
        var notes = _notes.Count;
        var result = parse();
        if (result == null)
        {
            _error = error;
            _dynamic = dynamic;
            if (_notes.Count > notes) _notes.RemoveRange(notes, _notes.Count - notes);
        }

        return result;
    }

    private static bool IsVar(CssValue value) => value.IsFunction && value.FunctionName == "var";

    /// <summary>1–4 values for top, right, bottom, left (margin, padding, inset, border-width/-style/-color).</summary>
    private void Box4(List<CssValue> components, string[] targets, Func<CssValue, string?> parse)
    {
        if (components.Count is < 1 or > 4)
        {
            Fail("expected 1 to 4 values.");
            return;
        }

        var parsed = components.ConvertAll(c => parse(c));
        if (Failed) return;
        var top = parsed[0];
        var right = parsed.Count > 1 ? parsed[1] : top;
        var bottom = parsed.Count > 2 ? parsed[2] : top;
        var left = parsed.Count > 3 ? parsed[3] : right;
        Assign(targets[0], top);
        Assign(targets[1], right);
        Assign(targets[2], bottom);
        Assign(targets[3], left);
    }

    /// <summary>1–2 values for a start/end pair (padding-inline, overflow, …).</summary>
    private void Pair(List<CssValue> components, string first, string second, Func<CssValue, string?> parse)
    {
        if (components.Count is < 1 or > 2)
        {
            Fail("expected 1 or 2 values.");
            return;
        }

        var a = parse(components[0]);
        var b = components.Count > 1 ? parse(components[1]) : a;
        Assign(first, a);
        Assign(second, b);
    }

    private string? RadiusValue(CssValue value) => Length(value, LengthOptions.Percent);

    private void Radius(List<CssValue> components, string target)
    {
        if (components.Count == 2)
        {
            Note("elliptical corners are not supported; the horizontal radius is used.");
            components = components.GetRange(0, 1);
        }

        Assign(target, Single(components, RadiusValue));
    }

    private void BorderRadius(List<CssValue> components, string[] corners)
    {
        var slash = components.FindIndex(c => c.IsDelim('/'));
        if (slash >= 0)
        {
            Note("elliptical corners are not supported; the horizontal radii are used.");
            components = components.GetRange(0, slash);
        }

        // border-radius lists top-left, top-right, bottom-right, bottom-left.
        Box4(components, corners, RadiusValue);
    }

    /// <summary>
    /// border, border-top, border-inline, … — width, style and color in any order. An omitted width
    /// is <c>medium</c> (3px) and an omitted style <c>none</c>, as in CSS; an omitted color is the
    /// initial one (CSS: currentcolor, which Miko does not have).
    /// </summary>
    private void BorderSides(List<CssValue> components, string[] sides)
    {
        if (!LineParts(components, out var width, out var style, out var color)) return;
        width ??= $"{LengthType}.Px(3f)";
        style ??= $"{CommonNs}BorderStyle.None";
        color ??= CssType + ".Initial";
        foreach (var side in sides)
        {
            Assign($"Border{side}Width", width);
            Assign($"Border{side}Style", style);
            Assign($"Border{side}Color", color);
        }
    }

    private void Outline(List<CssValue> components)
    {
        string? autoStyle = null;
        if (components.Exists(c => c.IsIdent("auto")))
        {
            Note("'outline-style: auto' is not supported and is treated as 'solid'.");
            autoStyle = $"{CommonNs}BorderStyle.Solid";
            components = components.FindAll(c => !c.IsIdent("auto"));
            if (components.Count == 0)
            {
                Assign("OutlineWidth", $"{LengthType}.Px(3f)");
                Assign("OutlineStyle", autoStyle);
                Assign("OutlineColor", CssType + ".Initial");
                return;
            }
        }

        if (!LineParts(components, out var width, out var style, out var color)) return;
        Assign("OutlineWidth", width ?? $"{LengthType}.Px(3f)");
        Assign("OutlineStyle", autoStyle ?? style ?? $"{CommonNs}BorderStyle.None");
        Assign("OutlineColor", color ?? CssType + ".Initial");
    }

    /// <summary>The width, style and color of a border or outline shorthand.</summary>
    private bool LineParts(List<CssValue> components, out string? width, out string? style, out string? color)
    {
        width = style = color = null;
        if (components.Count is < 1 or > 3)
        {
            Fail("expected a width, a style and a color.");
            return false;
        }

        var vars = new List<CssValue>();
        foreach (var component in components)
        {
            if (IsVar(component))
            {
                vars.Add(component);
                continue;
            }

            string? parsed;
            if (style == null && (parsed = Try(() => Enum(component, CommonNs + "BorderStyle", BorderStyleMap))) != null) style = parsed;
            else if (width == null && (parsed = Try(() => Length(component, LengthOptions.BorderWidth))) != null) width = parsed;
            else if (color == null && (parsed = Try(() => Color(component))) != null) color = parsed;
            else
            {
                if (component.ContainsPlaceholder()) FailDynamic("C# expressions are not supported in this shorthand; use the longhand (border-width, border-color).");
                else Fail($"'{Snippet(component)}' is not a width, a style or a color.");
                return false;
            }
        }

        // var(): by its name if that says what it is, else the part still missing (color first).
        foreach (var v in vars)
        {
            var name = CssParser.Components(v.Children!).Count > 0 ? CssParser.Components(v.Children!)[0].Token.Value.ToLowerInvariant() : "";
            if (name.Contains("width") && width == null) width = VarRef(v, VarKind.Length);
            else if (name.Contains("style") && style == null) style = VarRef(v, VarKind.Enum);
            else if (color == null) color = VarRef(v, VarKind.Color);
            else if (width == null) width = VarRef(v, VarKind.Length);
            else
            {
                Fail("cannot tell which part of the shorthand var() stands for; use the longhands.");
                return false;
            }
        }

        return !Failed;
    }

    /// <summary><c>flex: none | auto | &lt;grow&gt; [&lt;shrink&gt;] || &lt;basis&gt;</c>.</summary>
    private void Flex(List<CssValue> components)
    {
        if (components.Count == 1 && components[0].IsIdent("none"))
        {
            Assign("FlexGrow", Float(0));
            Assign("FlexShrink", Float(0));
            Assign("FlexBasis", $"{LengthType}.Auto");
            return;
        }

        if (components.Count == 1 && components[0].IsIdent("auto"))
        {
            Assign("FlexGrow", Float(1));
            Assign("FlexShrink", Float(1));
            Assign("FlexBasis", $"{LengthType}.Auto");
            return;
        }

        if (components.Count is < 1 or > 3)
        {
            Fail("expected 1 to 3 values.");
            return;
        }

        var numbers = new List<string>();
        string? basis = null;
        foreach (var component in components)
        {
            if (IsVar(component))
            {
                Fail("var() is not supported in the flex shorthand; use flex-grow, flex-shrink and flex-basis.");
                return;
            }

            var isNumber = component.Children == null && (component.Kind == CssTokenKind.Number
                || component.Kind == CssTokenKind.Placeholder && component.Token.Value.Length == 0);
            if (isNumber && basis == null && numbers.Count < 2)
            {
                numbers.Add(Number(component) ?? "");
            }
            else if (basis == null)
            {
                basis = Length(component, LengthOptions.Size);
            }
            else
            {
                Fail($"'{Snippet(component)}' is not supported.");
                return;
            }
        }

        if (Failed) return;
        // Omitted parts: grow 1, shrink 1, basis 0% (CSS flex shorthand).
        Assign("FlexGrow", numbers.Count > 0 ? numbers[0] : Float(1));
        Assign("FlexShrink", numbers.Count > 1 ? numbers[1] : Float(1));
        Assign("FlexBasis", basis ?? $"{LengthType}.Percent(0f)");
    }

    private void FlexFlow(List<CssValue> components)
    {
        string? direction = null, wrap = null;
        foreach (var component in components)
        {
            string? parsed;
            if (direction == null && (parsed = Try(() => Enum(component, CommonNs + "FlexDirection", FlexDirectionMap))) != null) direction = parsed;
            else if (wrap == null && (parsed = Try(() => Enum(component, CommonNs + "FlexWrap", FlexWrapMap))) != null) wrap = parsed;
            else
            {
                Fail($"'{Snippet(component)}' is not a flex direction or wrap.");
                return;
            }
        }

        Assign("FlexDirection", direction ?? $"{CommonNs}FlexDirection.Row");
        Assign("FlexWrap", wrap ?? $"{CommonNs}FlexWrap.Nowrap");
    }

    private void Gap(List<CssValue> components)
        => Pair(components, "RowGap", "ColumnGap", c => Length(c, LengthOptions.Percent | LengthOptions.NormalIsInitial));

    private string? Display(List<CssValue> components)
    {
        if (components.Count == 1) return Enum(components[0], CommonNs + "Display", DisplayMap);
        if (components.Count == 2 && components.TrueForAll(c => c.Kind == CssTokenKind.Ident && c.Children == null))
        {
            var pair = components[0].Token.Value.ToLowerInvariant() + " " + components[1].Token.Value.ToLowerInvariant();
            var single = pair switch
            {
                "block flow" or "flow block" => "block",
                "inline flow" or "flow inline" => "inline",
                "inline flow-root" or "flow-root inline" => "inline-block",
                "block flex" or "flex block" => "flex",
                "inline flex" or "flex inline" => "inline-flex",
                "block grid" or "grid block" => "grid",
                "block table" or "table block" => "table",
                _ => null,
            };
            if (single != null) return EnumKeyword(single, CommonNs + "Display", DisplayMap);
        }

        return Fail("this display value is not supported.");
    }

    /// <summary>An alignment keyword, with the <c>safe</c> / <c>unsafe</c> and <c>first baseline</c> forms.</summary>
    private string? Align(List<CssValue> components, string enumType, Dictionary<string, string> map)
    {
        if (components.Count == 2 && components[0].Kind == CssTokenKind.Ident)
        {
            var first = components[0].Token.Value.ToLowerInvariant();
            if (first is "safe" or "unsafe")
            {
                if (first == "safe") Note("overflow alignment 'safe' is not supported.");
                return Enum(components[1], enumType, map);
            }

            if (first == "first" && components[1].IsIdent("baseline")) return EnumKeyword("baseline", enumType, map);
            if (first == "last" && components[1].IsIdent("baseline")) return Fail("'last baseline' is not supported.");
        }

        return Single(components, c => Enum(c, enumType, map));
    }

    /// <summary>place-items / place-self / place-content: the align value, then the justify value.</summary>
    private void Place(List<CssValue> components, string align, Dictionary<string, string> alignMap, string justify, Dictionary<string, string> justifyMap)
    {
        if (components.Count is < 1 or > 2)
        {
            Fail("expected 1 or 2 values.");
            return;
        }

        Assign(align, Enum(components[0], CommonNs + align, alignMap));
        Assign(justify, Enum(components.Count > 1 ? components[1] : components[0], CommonNs + justify, justifyMap));
    }

    private string? FontWeight(CssValue value)
    {
        if (IsVar(value)) return VarRef(value, VarKind.Enum);
        var weight = value.Kind == CssTokenKind.Number && value.Children == null ? value.Token.Number : value.Token.Value.ToLowerInvariant() switch
        {
            "normal" => 400, "bold" => 700, "lighter" => 300, "bolder" => 900, _ => -1,
        };
        if (value.Kind == CssTokenKind.Placeholder) return FailDynamic("C# expressions are not supported for font-weight; bind a Style object.");
        if (weight is < 1 or > 1000) return Fail($"'{Snippet(value)}' is not a font weight.");
        if (value.Kind == CssTokenKind.Ident && value.Token.Value.ToLowerInvariant() is "lighter" or "bolder")
            Note($"'{value.Token.Value}' is relative in CSS; Miko uses the fixed weight {weight}.");

        var rounded = (int)Math.Max(100, Math.Min(900, Math.Round(weight / 100, MidpointRounding.AwayFromZero) * 100));
        if (rounded != weight) Note($"font weight {weight} is rounded to {rounded}.");
        var member = rounded switch
        {
            100 => "Thin", 200 => "ExtraLight", 300 => "Light", 400 => "Normal", 500 => "Medium",
            600 => "SemiBold", 700 => "Bold", 800 => "ExtraBold", _ => "Black",
        };
        return $"{CommonNs}FontWeight.{member}";
    }

    private string? FontFamily(List<CssValue> values)
    {
        var components = CssParser.Components(values);
        if (components.Count == 1 && IsVar(components[0])) return VarRef(components[0], VarKind.String);
        foreach (var part in CssParser.SplitByComma(values))
        {
            var names = CssParser.Components(part);
            if (names.Count == 0 || !names.TrueForAll(n => n.Children == null && n.Kind is CssTokenKind.Ident or CssTokenKind.String)
                || (names.Count > 1 && names.Exists(n => n.Kind == CssTokenKind.String)))
            {
                return Fail("expected a comma-separated list of font names.");
            }
        }

        return Str(FontFamilyList(values));
    }
}
