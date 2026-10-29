// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>transform, transform-origin, box-shadow.</summary>
internal sealed partial class CssPropertyTranslator
{
    private const string TransformFn = AnimationNs + "TransformFunction.";

    private static void AddEffectProperties(Dictionary<string, Property> p)
    {
        AddShorthand(p, "transform", new[] { "Transform" }, (t, v, c) => t.Assign("Transform", t.Transform(c)));
        AddShorthand(p, "transform-origin", new[] { "TransformOrigin" }, (t, v, c) => t.Assign("TransformOrigin", t.TransformOrigin(c)));
        AddShorthand(p, "box-shadow", new[] { "BoxShadow" }, (t, v, c) => t.Assign("BoxShadow", t.BoxShadow(v)));
        AddMotionProperties(p);
    }

    /// <summary>A length passed to a constructor (no var(), no keywords).</summary>
    private string? PlainLength(CssValue value, LengthOptions options)
    {
        if (value.ContainsFunction("var")) return Fail("var() is not supported here.");
        return Length(value, options);
    }

    private string? Transform(List<CssValue> components)
    {
        if (components.Count == 1 && components[0].IsIdent("none")) return $"new {AnimationNs}Transform()";
        var functions = new List<string>();
        foreach (var component in components)
        {
            if (!component.IsFunction) return Fail($"'{Snippet(component)}' is not a transform function.");
            var args = CssParser.SplitByComma(component.Children!).ConvertAll(part => part.Count == 1 ? part[0] : null);
            if (args.Contains(null)) return Fail($"invalid arguments to {component.FunctionName}().");

            var name = component.FunctionName;
            string? fn = name switch
            {
                "translate" when args.Count is 1 or 2 => Fn("Translate", Offset(args[0]!), args.Count > 1 ? Offset(args[1]!) : $"{LengthType}.Px(0f)"),
                "translatex" when args.Count == 1 => Fn("TranslateX", Offset(args[0]!)),
                "translatey" when args.Count == 1 => Fn("TranslateY", Offset(args[0]!)),
                "translate3d" when args.Count == 3 => IsZero(args[2]!) ? Fn("Translate", Offset(args[0]!), Offset(args[1]!)) : Fail("3D transforms are not supported."),
                "translatez" when args.Count == 1 => IsZero(args[0]!) ? string.Empty : Fail("3D transforms are not supported."),
                "scale" when args.Count is 1 or 2 => Fn("Scale", Factor(args[0]!), args.Count > 1 ? Factor(args[1]!) : Factor(args[0]!)),
                "scalex" when args.Count == 1 => Fn("ScaleX", Factor(args[0]!)),
                "scaley" when args.Count == 1 => Fn("ScaleY", Factor(args[0]!)),
                "scale3d" when args.Count == 3 => args[2]!.Kind == CssTokenKind.Number && args[2]!.Token.Number == 1
                    ? Fn("Scale", Factor(args[0]!), Factor(args[1]!)) : Fail("3D transforms are not supported."),
                "rotate" or "rotatez" when args.Count == 1 => Fn("Rotate", Degrees(args[0]!)),
                "skew" when args.Count is 1 or 2 => Fn("Skew", Degrees(args[0]!), args.Count > 1 ? Degrees(args[1]!) : Float(0)),
                "skewx" when args.Count == 1 => Fn("SkewX", Degrees(args[0]!)),
                "skewy" when args.Count == 1 => Fn("SkewY", Degrees(args[0]!)),
                "matrix" when args.Count == 6 => Fn("Matrix", args.Select(a => Factor(a!, allowPercent: false)).ToArray()),
                "rotatex" or "rotatey" or "rotate3d" or "perspective" or "matrix3d" => Fail("3D transforms are not supported."),
                _ => Fail($"'{name}()' is not a supported transform function."),
            };

            if (fn == null || Failed) return null;
            if (fn.Length > 0) functions.Add(fn);
        }

        return $"new {AnimationNs}Transform({string.Join(", ", functions)})";
    }

    private string? Fn(string type, params string?[] args)
        => args.Contains(null) ? null : $"new {TransformFn}{type}({string.Join(", ", args)})";

    private string? Offset(CssValue value) => PlainLength(value, LengthOptions.Percent | LengthOptions.Negative);

    private static bool IsZero(CssValue value)
        => value.Children == null && value.Kind is CssTokenKind.Number or CssTokenKind.Dimension && value.Token.Number == 0;

    private string? Factor(CssValue value, bool allowPercent = true)
    {
        var token = value.Token;
        if (value.Children != null) return Fail($"'{Snippet(value)}' is not supported in a transform.");
        return token.Kind switch
        {
            CssTokenKind.Number => Float(token.Number),
            CssTokenKind.Percentage when allowPercent => Float(token.Number / 100),
            CssTokenKind.Placeholder when token.Value.Length == 0 => $"{BuilderType}.CssNumber({Placeholder(value)})",
            _ => Fail($"'{Snippet(value)}' is not a number."),
        };
    }

    private string? Degrees(CssValue value)
    {
        var token = value.Token;
        if (value.Children != null) return Fail($"'{Snippet(value)}' is not supported in a transform.");
        if (token.Kind == CssTokenKind.Number && token.Number == 0) return Float(0);
        if (token.Kind == CssTokenKind.Dimension && Angle(token) is { } degrees) return Float(degrees);
        if (token.Kind == CssTokenKind.Placeholder)
        {
            return token.Value.ToLowerInvariant() switch
            {
                "deg" => $"{BuilderType}.CssNumber({Placeholder(value)})",
                "turn" => $"{BuilderType}.CssNumber({Placeholder(value)}) * 360f",
                _ => FailDynamic("an angle after an expression must be in deg or turn, e.g. rotate(@(angle)deg)."),
            };
        }

        return Fail($"'{Snippet(value)}' is not an angle.");
    }

    private string? TransformOrigin(List<CssValue> components)
    {
        if (components.Count is < 1 or > 3) return Fail("expected 1 to 3 values.");
        if (components.Count == 3)
        {
            if (!IsZero(components[2])) Note("the z offset of transform-origin is not supported.");
            components = components.GetRange(0, 2);
        }

        static string? Keyword(CssValue v) => v.Kind == CssTokenKind.Ident && v.Children == null ? v.Token.Value.ToLowerInvariant() : null;
        var first = components[0];
        var second = components.Count > 1 ? components[1] : null;
        // Keywords may be given vertical first (top left).
        if (Keyword(first) is "top" or "bottom" || second != null && Keyword(second) is "left" or "right")
        {
            (first, second) = (second ?? first, first);
            if (components.Count == 1) second = null;
        }

        string? Axis(CssValue? v, bool horizontal)
        {
            if (v == null) return $"{LengthType}.Percent(50f)";
            switch (Keyword(v))
            {
                case "center": return $"{LengthType}.Percent(50f)";
                case "left" when horizontal: return $"{LengthType}.Percent(0f)";
                case "right" when horizontal: return $"{LengthType}.Percent(100f)";
                case "top" when !horizontal: return $"{LengthType}.Percent(0f)";
                case "bottom" when !horizontal: return $"{LengthType}.Percent(100f)";
                case null: return PlainLength(v, LengthOptions.Percent | LengthOptions.Negative);
                default: return Fail($"'{Snippet(v)}' is not supported in transform-origin.");
            }
        }

        if (components.Count == 1 && Keyword(components[0]) is "top" or "bottom")
        {
            var y = Axis(components[0], horizontal: false);
            return y == null ? null : $"new {AnimationNs}TransformOrigin({LengthType}.Percent(50f), {y})";
        }

        var x = Axis(first, horizontal: true);
        var yy = Axis(second, horizontal: false);
        return x == null || yy == null ? null : $"new {AnimationNs}TransformOrigin({x}, {yy})";
    }

    /// <summary><c>[inset] x y [blur [spread]] [color]</c>, comma-separated. Lengths are pixels.</summary>
    private string? BoxShadow(List<CssValue> values)
    {
        var all = CssParser.Components(values);
        if (all.Count == 1 && all[0].IsIdent("none")) return $"new global::System.Collections.Generic.List<{CommonNs}BoxShadow>()";

        var shadows = new List<string>();
        foreach (var part in CssParser.SplitByComma(values))
        {
            var components = CssParser.Components(part);
            var inset = false;
            string? color = null;
            var lengths = new List<string>();
            foreach (var c in components)
            {
                if (c.IsIdent("inset") && !inset)
                {
                    inset = true;
                    continue;
                }

                if (c.ContainsFunction("var")) return Fail("var() is not supported in box-shadow.");
                var isLength = c.Children == null && (c.Kind == CssTokenKind.Dimension || c.Kind == CssTokenKind.Number && c.Token.Number == 0
                    || c.Kind == CssTokenKind.Placeholder && c.Token.Value.Length > 0) || c.IsFunction && c.FunctionName == "calc";
                if (isLength && lengths.Count < 4)
                {
                    var length = PlainLength(c, LengthOptions.Negative);
                    if (length == null) return null;
                    if (!length.StartsWith(LengthType + ".Px(", System.StringComparison.Ordinal))
                    {
                        if (length.StartsWith(LengthType + ".Vw(", System.StringComparison.Ordinal) || length.StartsWith(LengthType + ".Vh(", System.StringComparison.Ordinal))
                            return Fail("viewport units are not supported in box-shadow.");
                        Note("box-shadow lengths are converted to pixels when the style is created (1em = 16px).");
                    }

                    lengths.Add(length);
                }
                else if (color == null)
                {
                    color = Color(c);
                    if (color == null) return null;
                    if (color.StartsWith(CssType, System.StringComparison.Ordinal)) return Fail("currentcolor is not supported in box-shadow.");
                }
                else
                {
                    return Fail($"'{Snippet(c)}' is not supported in box-shadow.");
                }
            }

            if (lengths.Count is < 2 or > 4) return Fail("a shadow needs 2 to 4 lengths.");
            if (color == null)
            {
                Note("a shadow without a color is black (Miko has no currentcolor).");
                color = NewColor(0, 0, 0, 255);
            }

            while (lengths.Count < 4) lengths.Add($"{LengthType}.Px(0f)");
            shadows.Add($"new {CommonNs}BoxShadow({string.Join(", ", lengths)}, {color}, {(inset ? "true" : "false")})");
        }

        return $"new global::System.Collections.Generic.List<{CommonNs}BoxShadow> {{ {string.Join(", ", shadows)} }}";
    }
}
