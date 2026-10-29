// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>Text, font and background properties with their own grammar.</summary>
internal sealed partial class CssPropertyTranslator
{
    private string? Content(List<CssValue> components)
    {
        if (components.Count == 1 && IsVar(components[0])) return VarRef(components[0], VarKind.String);
        if (components.Count == 1 && (components[0].IsIdent("none") || components[0].IsIdent("normal")))
        {
            Note("Miko creates the pseudo-element whenever a rule matches; 'none' gives it no text.");
            return Str(string.Empty);
        }

        var builder = new StringBuilder();
        foreach (var component in components)
        {
            if (component.Kind != CssTokenKind.String || component.Children != null)
                return Fail("only strings are supported (no attr(), counter(), url() or quotes).");
            builder.Append(component.Token.Value);
        }

        return Str(builder.ToString());
    }

    private void WordBreak(List<CssValue> components)
    {
        if (components.Count == 1 && components[0].IsIdent("break-word"))
        {
            // Deprecated: word-break: normal + overflow-wrap: anywhere.
            Assign("WordBreak", $"{CommonNs}WordBreak.Normal");
            Assign("OverflowWrap", $"{CommonNs}OverflowWrap.Anywhere");
            return;
        }

        Assign("WordBreak", Single(components, c => Enum(c, CommonNs + "WordBreak", WordBreakMap)));
    }

    /// <summary>Miko draws the line only: style, color and thickness are not supported.</summary>
    private void TextDecoration(List<CssValue> components, bool shorthand)
    {
        string? line = null;
        foreach (var component in components)
        {
            string? parsed;
            if ((parsed = Try(() => Enum(component, CommonNs + "TextDecoration", TextDecorationMap))) != null)
            {
                if (line != null) Note("Miko draws one decoration line; only the first is used.");
                line ??= parsed;
            }
            else if (shorthand && !component.ContainsPlaceholder())
            {
                Note("text-decoration style, color and thickness are not supported.");
            }
            else
            {
                Fail($"'{Snippet(component)}' is not supported.");
                return;
            }
        }

        Assign("TextDecoration", line ?? $"{CommonNs}TextDecoration.None");
    }

    private string? Cursor(List<CssValue> values)
    {
        var parts = CssParser.SplitByComma(values);
        if (parts.Count > 1) Note("cursor images are not supported; the keyword fallback is used.");
        var keyword = CssParser.Components(parts[parts.Count - 1]);
        return Single(keyword, c => Enum(c, CommonNs + "Cursor", CursorMap));
    }

    private string? AspectRatio(List<CssValue> components)
    {
        const string Type = CommonNs + "AspectRatio";
        if (components.Count == 1 && components[0].IsIdent("auto")) return Type + ".Auto";
        var auto = components.Count > 0 && components[0].IsIdent("auto")
            || components.Count > 0 && components[components.Count - 1].IsIdent("auto");
        var ratio = components.FindAll(c => !c.IsIdent("auto"));
        if (ratio.Count == 1 && ratio[0].Kind == CssTokenKind.Number && ratio[0].Children == null && ratio[0].Token.Number > 0)
        {
            return auto ? $"{Type}.AutoOr({Float(ratio[0].Token.Number)}, 1f)" : $"new {Type}({Float(ratio[0].Token.Number)})";
        }

        if (ratio.Count == 3 && ratio[1].IsDelim('/') && ratio[0].Kind == CssTokenKind.Number && ratio[2].Kind == CssTokenKind.Number
            && ratio[0].Token.Number > 0 && ratio[2].Token.Number > 0)
        {
            var w = Float(ratio[0].Token.Number);
            var h = Float(ratio[2].Token.Number);
            return auto ? $"{Type}.AutoOr({w}, {h})" : $"new {Type}({w}, {h})";
        }

        return Fail("expected 'auto', a number or 'width / height'.");
    }

    private string? BackgroundImage(List<CssValue> components)
    {
        if (components.Count == 1 && components[0].IsIdent("none")) return CssType + ".Initial";
        return Fail("images and gradients are not supported in CSS; set Style.BackgroundImage from C# (BackgroundImage.FromResource, …).");
    }

    private string? BackgroundRepeat(List<CssValue> components)
    {
        const string Type = CommonNs + "BackgroundRepeat";
        string? Axis(CssValue c) => c.Children == null && c.Kind == CssTokenKind.Ident ? c.Token.Value.ToLowerInvariant() switch
        {
            "repeat" => "repeat", "no-repeat" => "no-repeat", "space" or "round" => "repeat", _ => null,
        } : null;

        if (components.Count == 1)
        {
            var keyword = components[0].Kind == CssTokenKind.Ident ? components[0].Token.Value.ToLowerInvariant() : "";
            if (keyword is "space" or "round") Note($"'{keyword}' is not supported and is treated as 'repeat'.");
            return keyword switch
            {
                "repeat" or "space" or "round" => Type + ".Repeat",
                "repeat-x" => Type + ".RepeatX",
                "repeat-y" => Type + ".RepeatY",
                "no-repeat" => Type + ".NoRepeat",
                _ => Fail($"'{Snippet(components[0])}' is not supported."),
            };
        }

        if (components.Count == 2 && Axis(components[0]) is { } x && Axis(components[1]) is { } y)
        {
            return (x, y) switch
            {
                ("repeat", "repeat") => Type + ".Repeat",
                ("repeat", _) => Type + ".RepeatX",
                (_, "repeat") => Type + ".RepeatY",
                _ => Type + ".NoRepeat",
            };
        }

        return Fail("this background-repeat value is not supported.");
    }

    /// <summary>Miko places a background at one of nine positions: keywords, or 0% / 50% / 100%.</summary>
    private string? BackgroundPosition(List<CssValue> components)
    {
        if (components.Count is < 1 or > 2) return Fail("only one or two position keywords are supported.");
        string? h = null, v = null;
        for (var i = 0; i < components.Count; i++)
        {
            var component = components[i];
            var keyword = component.Kind == CssTokenKind.Ident && component.Children == null ? component.Token.Value.ToLowerInvariant() : null;
            switch (keyword)
            {
                case "left" when h == null: h = "Left"; continue;
                case "right" when h == null: h = "Right"; continue;
                case "top" when v == null: v = "Top"; continue;
                case "bottom" when v == null: v = "Bottom"; continue;
                case "center": continue;
            }

            // A percentage is horizontal in the first position, vertical in the second.
            var percent = component.Kind == CssTokenKind.Percentage && component.Children == null ? component.Token.Number
                : component.Kind == CssTokenKind.Number && component.Children == null && component.Token.Number == 0 ? 0 : -1;
            var edge = percent switch { 0 => 0, 50 => 1, 100 => 2, _ => -1 };
            if (edge < 0 || keyword != null) return Fail("only the keywords left, center, right, top, bottom and 0%, 50%, 100% are supported.");
            if (i == 0 && h == null) h = edge switch { 0 => "Left", 2 => "Right", _ => "Center" };
            else if (v == null) v = edge switch { 0 => "Top", 2 => "Bottom", _ => "Center" };
            else return Fail("this background-position value is not supported.");
        }

        var horizontal = h ?? "Center";
        var vertical = v ?? "Center";
        var member = (horizontal, vertical) switch
        {
            ("Center", "Center") => "Center",
            ("Center", _) => "Center" + vertical,
            (_, "Center") => horizontal + "Center",
            _ => horizontal + vertical,
        };
        return $"{CommonNs}BackgroundPosition.{member}";
    }

    private string? BackgroundSize(List<CssValue> components)
    {
        const string Type = CommonNs + "BackgroundSize";
        if (components.Count == 1 && components[0].Kind == CssTokenKind.Ident && components[0].Children == null)
        {
            switch (components[0].Token.Value.ToLowerInvariant())
            {
                case "auto": return Type + ".Auto";
                case "cover": return Type + ".Cover";
                case "contain": return Type + ".Contain";
            }
        }

        if (components.Count is < 1 or > 2) return Fail("expected one or two sizes.");
        if (components.Exists(c => c.IsIdent("auto")))
        {
            if (components.TrueForAll(c => c.IsIdent("auto"))) return Type + ".Auto";
            return Fail("'auto' combined with a length is not supported in background-size.");
        }

        var width = Length(components[0], LengthOptions.Percent);
        var height = components.Count > 1 ? Length(components[1], LengthOptions.Percent) : width;
        return width == null || height == null ? null : $"{Type}.From({width}, {height})";
    }

    /// <summary>
    /// <c>background</c> with one layer: its color, repeat, position and size. Omitted parts are reset
    /// to their initial values; an image makes the declaration unsupported.
    /// </summary>
    private void Background(List<CssValue> values)
    {
        if (CssParser.SplitByComma(values).Count > 1)
        {
            Fail("multiple background layers are not supported.");
            return;
        }

        var components = CssParser.Components(values);
        if (components.Count == 1 && components[0].IsIdent("none"))
        {
            Assign("BackgroundColor", CssType + ".Initial");
            Assign("BackgroundImage", CssType + ".Initial");
            return;
        }

        string? color = null, repeat = null, size = null;
        var position = new List<CssValue>();
        var repeats = new List<CssValue>();
        for (var i = 0; i < components.Count; i++)
        {
            var c = components[i];
            var keyword = c.Kind == CssTokenKind.Ident && c.Children == null ? c.Token.Value.ToLowerInvariant() : null;
            if (c.IsFunction && (c.FunctionName.EndsWith("gradient", System.StringComparison.Ordinal) || c.FunctionName is "url" or "image-set")
                || c.Kind == CssTokenKind.Url)
            {
                Fail("images and gradients are not supported in CSS; set Style.BackgroundImage from C#.");
                return;
            }

            if (keyword == "none") continue;
            if (keyword is "repeat" or "repeat-x" or "repeat-y" or "no-repeat" or "space" or "round")
            {
                repeats.Add(c);
            }
            else if (keyword is "scroll" or "local" or "fixed" or "border-box" or "padding-box" or "content-box" or "text")
            {
                if (keyword is "fixed" or "text") Note($"background '{keyword}' is not supported.");
            }
            else if (c.IsDelim('/'))
            {
                var sizes = new List<CssValue>();
                while (i + 1 < components.Count && sizes.Count < 2 && Try(() => BackgroundSize(new List<CssValue> { components[i + 1] })) != null)
                {
                    sizes.Add(components[++i]);
                }

                size = BackgroundSize(sizes);
                if (size == null) return;
            }
            else if (keyword is "left" or "right" or "top" or "bottom" or "center"
                     || c.Kind is CssTokenKind.Percentage or CssTokenKind.Dimension || (c.Kind == CssTokenKind.Number && c.Token.Number == 0))
            {
                position.Add(c);
            }
            else if (color == null && (color = Try(() => Color(c))) != null)
            {
            }
            else
            {
                if (c.ContainsPlaceholder()) FailDynamic("C# expressions are not supported in the background shorthand; use background-color.");
                else Fail($"'{Snippet(c)}' is not supported.");
                return;
            }
        }

        if (repeats.Count > 0 && (repeat = BackgroundRepeat(repeats)) == null) return;
        var positionExpression = position.Count > 0 ? BackgroundPosition(position) : CssType + ".Initial";
        Assign("BackgroundColor", color ?? CssType + ".Initial");
        Assign("BackgroundRepeat", repeat ?? CssType + ".Initial");
        Assign("BackgroundPosition", positionExpression);
        Assign("BackgroundSize", size ?? CssType + ".Initial");
    }

    /// <summary><c>font: [style || variant || weight || stretch] size [/ line-height] family</c>.</summary>
    private void Font(List<CssValue> values)
    {
        var components = CssParser.Components(values);
        if (components.Count == 1 && components[0].Kind == CssTokenKind.Ident)
        {
            Fail("system font keywords are not supported.");
            return;
        }

        string? style = null, weight = null;
        var index = 0;
        for (; index < components.Count; index++)
        {
            var c = components[index];
            var keyword = c.Kind == CssTokenKind.Ident && c.Children == null ? c.Token.Value.ToLowerInvariant() : null;
            if (keyword == "normal" || keyword == "small-caps" || keyword is not null && keyword.EndsWith("condensed", System.StringComparison.Ordinal)
                || keyword is not null && keyword.EndsWith("expanded", System.StringComparison.Ordinal))
            {
                if (keyword != "normal") Note($"'{keyword}' is not supported in the font shorthand.");
                continue;
            }

            if (keyword is "italic" or "oblique")
            {
                style = $"{CommonNs}FontStyle.{(keyword == "italic" ? "Italic" : "Oblique")}";
                continue;
            }

            if (keyword is "bold" or "bolder" or "lighter" || c.Kind == CssTokenKind.Number && c.Children == null)
            {
                weight = FontWeight(c);
                if (weight == null) return;
                continue;
            }

            break;
        }

        if (index >= components.Count)
        {
            Fail("the font shorthand needs a size and a family.");
            return;
        }

        var size = Length(components[index++], LengthOptions.FontSize | LengthOptions.PercentIsEm);
        string? lineHeight = null;
        if (index < components.Count && components[index].IsDelim('/'))
        {
            index++;
            if (index >= components.Count)
            {
                Fail("expected a line height after '/'.");
                return;
            }

            lineHeight = Length(components[index++], LengthOptions.Unitless | LengthOptions.NormalIsInitial | LengthOptions.PercentIsEm);
        }

        if (index >= components.Count)
        {
            Fail("the font shorthand needs a family.");
            return;
        }

        var familyStart = values.IndexOf(components[index]);
        var family = FontFamily(values.GetRange(familyStart, values.Count - familyStart));
        Assign("FontStyle", style ?? CssType + ".Initial");
        Assign("FontWeight", weight ?? CssType + ".Initial");
        Assign("FontSize", size);
        Assign("LineHeight", lineHeight ?? CssType + ".Initial");
        Assign("FontFamily", family);
    }
}
