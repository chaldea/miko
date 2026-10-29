// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

internal sealed partial class CssPropertyTranslator
{
    /// <summary>A color, folded to <c>new Color(r, g, b[, a])</c> at compile time.</summary>
    private string? Color(CssValue value)
    {
        if (value.IsFunction)
        {
            return value.FunctionName switch
            {
                "rgb" or "rgba" => Rgb(value),
                "hsl" or "hsla" => Hsl(value),
                "var" => VarRef(value, VarKind.Color),
                _ => Fail($"the color function '{value.FunctionName}()' is not supported (use a hex, rgb() or hsl() color)."),
            };
        }

        var token = value.Token;
        switch (token.Kind)
        {
            case CssTokenKind.Hash:
                return Hex(token.Value) ?? Fail($"'#{token.Value}' is not a hex color.");
            case CssTokenKind.Placeholder when token.Value.Length == 0:
                return $"{BuilderType}.CssColor({Placeholder(value)})";
            case CssTokenKind.Ident:
                var name = token.Value.ToLowerInvariant();
                if (name == "transparent") return NewColor(0, 0, 0, 0);
                if (name == "currentcolor")
                {
                    if (_declaration.Name == "color") return CssType + ".Inherit";
                    return Fail("'currentcolor' is not supported here (Miko has no current-color reference).");
                }

                return s_namedColors.TryGetValue(name, out var rgb)
                    ? NewColor((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF, 255)
                    : Fail($"'{token.Value}' is not a color.");
            default:
                return Fail($"'{Snippet(value)}' is not a color.");
        }
    }

    private static string NewColor(int r, int g, int b, int a)
        => a == 255
            ? FormattableString.Invariant($"new {ColorType}({r}, {g}, {b})")
            : FormattableString.Invariant($"new {ColorType}({r}, {g}, {b}, {a})");

    private static string? Hex(string hex)
    {
        if (hex.Length is not (3 or 4 or 6 or 8)) return null;
        foreach (var c in hex)
        {
            if (!Uri.IsHexDigit(c)) return null;
        }

        if (hex.Length <= 4)
        {
            var expanded = new char[hex.Length * 2];
            for (var i = 0; i < hex.Length; i++) expanded[2 * i] = expanded[2 * i + 1] = hex[i];
            hex = new string(expanded);
        }

        int Channel(int i) => int.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return NewColor(Channel(0), Channel(1), Channel(2), hex.Length == 8 ? Channel(3) : 255);
    }

    /// <summary>The channels and alpha of rgb()/hsl(): <c>a, b, c[, alpha]</c> or <c>a b c [/ alpha]</c>.</summary>
    private bool ColorArguments(CssValue function, out List<CssValue> channels, out CssValue? alpha)
    {
        alpha = null;
        var args = function.Children!;
        if (args.Exists(a => a.ContainsFunction("var") || a.ContainsFunction("calc") || a.ContainsPlaceholder()))
        {
            channels = new List<CssValue>();
            Fail($"var(), calc() and C# expressions are not supported inside {function.FunctionName}().");
            return false;
        }

        if (args.Exists(a => a.IsComma))
        {
            channels = new List<CssValue>();
            foreach (var part in CssParser.SplitByComma(args))
            {
                if (part.Count != 1)
                {
                    Fail($"invalid {function.FunctionName}() color.");
                    return false;
                }

                channels.Add(part[0]);
            }
        }
        else
        {
            channels = CssParser.Components(args);
            var slash = channels.FindIndex(c => c.IsDelim('/'));
            if (slash >= 0)
            {
                if (slash != channels.Count - 2)
                {
                    Fail($"invalid {function.FunctionName}() color.");
                    return false;
                }

                channels.Add(channels[slash + 1]);
                channels.RemoveRange(slash, 2);
            }
        }

        if (channels.Count == 4)
        {
            alpha = channels[3];
            channels.RemoveAt(3);
        }

        if (channels.Count != 3)
        {
            Fail($"invalid {function.FunctionName}() color.");
            return false;
        }

        return true;
    }

    private string? Rgb(CssValue function)
    {
        if (!ColorArguments(function, out var channels, out var alpha)) return null;
        var rgb = new int[3];
        for (var i = 0; i < 3; i++)
        {
            var t = channels[i].Token;
            if (channels[i].IsIdent("none")) rgb[i] = 0;
            else if (t.Kind == CssTokenKind.Number) rgb[i] = Clamp255(t.Number);
            else if (t.Kind == CssTokenKind.Percentage) rgb[i] = Clamp255(t.Number / 100 * 255);
            else return Fail("invalid rgb() channel.");
        }

        var a = Alpha(alpha);
        return a < 0 ? null : NewColor(rgb[0], rgb[1], rgb[2], a);
    }

    private string? Hsl(CssValue function)
    {
        if (!ColorArguments(function, out var channels, out var alpha)) return null;
        var hue = channels[0];
        double h;
        if (hue.IsIdent("none")) h = 0;
        else if (hue.Kind == CssTokenKind.Number) h = hue.Token.Number;
        else if (hue.Kind == CssTokenKind.Dimension && Angle(hue.Token) is { } degrees) h = degrees;
        else return Fail("invalid hsl() hue.");

        var s = Fraction(channels[1]);
        var l = Fraction(channels[2]);
        if (s is not { } sat || l is not { } light) return Fail("invalid hsl() saturation or lightness.");

        h = ((h % 360) + 360) % 360;
        var chroma = (1 - Math.Abs(2 * light - 1)) * sat;
        var x = chroma * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = light - chroma / 2;
        var (r, g, b) = h switch
        {
            < 60 => (chroma, x, 0.0),
            < 120 => (x, chroma, 0.0),
            < 180 => (0.0, chroma, x),
            < 240 => (0.0, x, chroma),
            < 300 => (x, 0.0, chroma),
            _ => (chroma, 0.0, x),
        };

        var a = Alpha(alpha);
        return a < 0 ? null : NewColor(Clamp255((r + m) * 255), Clamp255((g + m) * 255), Clamp255((b + m) * 255), a);
    }

    /// <summary>0–255, or -1 (failed).</summary>
    private int Alpha(CssValue? alpha)
    {
        if (alpha == null || alpha.IsIdent("none")) return 255;
        var t = alpha.Token;
        if (t.Kind == CssTokenKind.Number) return Clamp255(Math.Max(0, Math.Min(1, t.Number)) * 255);
        if (t.Kind == CssTokenKind.Percentage) return Clamp255(Math.Max(0, Math.Min(100, t.Number)) / 100 * 255);
        Fail("invalid alpha value.");
        return -1;
    }

    private static double? Fraction(CssValue value)
    {
        if (value.IsIdent("none")) return 0;
        var t = value.Token;
        if (t.Kind == CssTokenKind.Percentage || t.Kind == CssTokenKind.Number) return Math.Max(0, Math.Min(100, t.Number)) / 100;
        return null;
    }

    private static double? Angle(CssToken token) => token.Value.ToLowerInvariant() switch
    {
        "deg" => token.Number,
        "rad" => token.Number * 180 / Math.PI,
        "grad" => token.Number * 0.9,
        "turn" => token.Number * 360,
        _ => null,
    };

    private static int Clamp255(double value) => (int)Math.Round(Math.Max(0, Math.Min(255, value)), MidpointRounding.AwayFromZero);

    private static readonly Dictionary<string, int> s_namedColors = ParseColors(
        "aliceblue:f0f8ff antiquewhite:faebd7 aqua:00ffff aquamarine:7fffd4 azure:f0ffff beige:f5f5dc bisque:ffe4c4 " +
        "black:000000 blanchedalmond:ffebcd blue:0000ff blueviolet:8a2be2 brown:a52a2a burlywood:deb887 cadetblue:5f9ea0 " +
        "chartreuse:7fff00 chocolate:d2691e coral:ff7f50 cornflowerblue:6495ed cornsilk:fff8dc crimson:dc143c cyan:00ffff " +
        "darkblue:00008b darkcyan:008b8b darkgoldenrod:b8860b darkgray:a9a9a9 darkgreen:006400 darkgrey:a9a9a9 " +
        "darkkhaki:bdb76b darkmagenta:8b008b darkolivegreen:556b2f darkorange:ff8c00 darkorchid:9932cc darkred:8b0000 " +
        "darksalmon:e9967a darkseagreen:8fbc8f darkslateblue:483d8b darkslategray:2f4f4f darkslategrey:2f4f4f " +
        "darkturquoise:00ced1 darkviolet:9400d3 deeppink:ff1493 deepskyblue:00bfff dimgray:696969 dimgrey:696969 " +
        "dodgerblue:1e90ff firebrick:b22222 floralwhite:fffaf0 forestgreen:228b22 fuchsia:ff00ff gainsboro:dcdcdc " +
        "ghostwhite:f8f8ff gold:ffd700 goldenrod:daa520 gray:808080 green:008000 greenyellow:adff2f grey:808080 " +
        "honeydew:f0fff0 hotpink:ff69b4 indianred:cd5c5c indigo:4b0082 ivory:fffff0 khaki:f0e68c lavender:e6e6fa " +
        "lavenderblush:fff0f5 lawngreen:7cfc00 lemonchiffon:fffacd lightblue:add8e6 lightcoral:f08080 lightcyan:e0ffff " +
        "lightgoldenrodyellow:fafad2 lightgray:d3d3d3 lightgreen:90ee90 lightgrey:d3d3d3 lightpink:ffb6c1 " +
        "lightsalmon:ffa07a lightseagreen:20b2aa lightskyblue:87cefa lightslategray:778899 lightslategrey:778899 " +
        "lightsteelblue:b0c4de lightyellow:ffffe0 lime:00ff00 limegreen:32cd32 linen:faf0e6 magenta:ff00ff maroon:800000 " +
        "mediumaquamarine:66cdaa mediumblue:0000cd mediumorchid:ba55d3 mediumpurple:9370db mediumseagreen:3cb371 " +
        "mediumslateblue:7b68ee mediumspringgreen:00fa9a mediumturquoise:48d1cc mediumvioletred:c71585 " +
        "midnightblue:191970 mintcream:f5fffa mistyrose:ffe4e1 moccasin:ffe4b5 navajowhite:ffdead navy:000080 " +
        "oldlace:fdf5e6 olive:808000 olivedrab:6b8e23 orange:ffa500 orangered:ff4500 orchid:da70d6 palegoldenrod:eee8aa " +
        "palegreen:98fb98 paleturquoise:afeeee palevioletred:db7093 papayawhip:ffefd5 peachpuff:ffdab9 peru:cd853f " +
        "pink:ffc0cb plum:dda0dd powderblue:b0e0e6 purple:800080 rebeccapurple:663399 red:ff0000 rosybrown:bc8f8f " +
        "royalblue:4169e1 saddlebrown:8b4513 salmon:fa8072 sandybrown:f4a460 seagreen:2e8b57 seashell:fff5ee " +
        "sienna:a0522d silver:c0c0c0 skyblue:87ceeb slateblue:6a5acd slategray:708090 slategrey:708090 snow:fffafa " +
        "springgreen:00ff7f steelblue:4682b4 tan:d2b48c teal:008080 thistle:d8bfd8 tomato:ff6347 turquoise:40e0d0 " +
        "violet:ee82ee wheat:f5deb3 white:ffffff whitesmoke:f5f5f5 yellow:ffff00 yellowgreen:9acd32");

    private static Dictionary<string, int> ParseColors(string entries)
    {
        var colors = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in entries.Split(' '))
        {
            var colon = entry.IndexOf(':');
            colors[entry.Substring(0, colon)] = int.Parse(entry.Substring(colon + 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return colors;
    }
}
