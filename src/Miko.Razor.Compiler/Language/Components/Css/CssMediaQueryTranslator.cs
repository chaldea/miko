// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>
/// Translates a media query list into the body of a <c>MediaCondition</c> lambda over
/// <c>ViewportInfo v</c> (ISSUE-161): <c>(min-width: 600px)</c> → <c>v.Width &gt;= 600f</c>. Miko
/// knows the viewport size only, so width, height, aspect-ratio and orientation are evaluated; a
/// feature it cannot evaluate makes its query never match (reported), as a browser treats an
/// unknown feature.
/// </summary>
internal static class CssMediaQueryTranslator
{
    public const string Always = "true";
    public const string Never = "false";

    public static string Translate(List<CssToken> prelude, int start, int length, CssTranslationContext context)
    {
        var notes = new List<string>();
        var queries = new List<string>();
        foreach (var part in CssParser.SplitByComma(CssParser.ParseComponentValues(prelude)))
        {
            var components = CssParser.Components(part);
            queries.Add(components.Count == 0 ? Never : Query(components, notes) ?? Never);
        }

        foreach (var note in notes)
        {
            context.Diagnostics.Add(new CssDiagnostic(CssDiagnosticKind.Approximated, start, length,
                context.Text.Substring(Math.Max(0, start), Math.Max(0, Math.Min(length, context.Text.Length - start))).Trim(), note));
        }

        if (queries.Contains(Always)) return Always;
        queries.RemoveAll(q => q == Never);
        return queries.Count == 0 ? Never : string.Join(" || ", queries.ConvertAll(Wrap));
    }

    /// <summary>Both conditions (bodies); null stands for none.</summary>
    public static string And(string? a, string b)
    {
        if (a == null || a == Always) return b;
        if (b == Always) return a;
        if (a == Never || b == Never) return Never;
        return Wrap(a) + " && " + Wrap(b);
    }

    /// <summary>The <c>new MediaCondition(…)</c> for a condition body.</summary>
    public static string ToExpression(string body) => $"new global::Miko.Styling.MediaCondition(v => {body})";

    private static string Wrap(string expression) => expression.IndexOf(' ') < 0 || expression.StartsWith("(", StringComparison.Ordinal) && expression.EndsWith(")", StringComparison.Ordinal) && Balanced(expression)
        ? expression : "(" + expression + ")";

    private static bool Balanced(string expression)
    {
        var depth = 0;
        for (var i = 0; i < expression.Length; i++)
        {
            if (expression[i] == '(') depth++;
            else if (expression[i] == ')' && --depth == 0 && i < expression.Length - 1) return false;
        }

        return true;
    }

    private static string? Query(List<CssValue> c, List<string> notes)
    {
        var i = 0;
        var negate = false;
        if (c[0].IsIdent("only")) i++;
        else if (c[0].IsIdent("not") && c.Count > 1 && c[1].Kind == CssTokenKind.Ident) { negate = true; i++; }

        if (i < c.Count && c[i].Kind == CssTokenKind.Ident && c[i].Children == null && !c[i].IsIdent("not"))
        {
            var type = c[i].Token.Value.ToLowerInvariant() switch
            {
                "all" or "screen" => Always,
                _ => Never,
            };
            i++;
            var rest = Always;
            if (i < c.Count)
            {
                if (!c[i].IsIdent("and")) return Unsupported(notes, "invalid media query.");
                i++;
                rest = Condition(c, ref i, notes, allowOr: false) ?? Never;
            }

            var result = And(type, rest);
            if (negate) result = result == Always ? Never : result == Never ? Always : "!" + Wrap(result);
            return i == c.Count ? result : Unsupported(notes, "invalid media query.");
        }

        var condition = Condition(c, ref i, notes, allowOr: true);
        return i == c.Count ? condition : Unsupported(notes, "invalid media query.");
    }

    private static string? Unsupported(List<string> notes, string note)
    {
        if (!notes.Contains(note)) notes.Add(note);
        return null;
    }

    private static string? Condition(List<CssValue> c, ref int i, List<string> notes, bool allowOr)
    {
        if (i < c.Count && c[i].IsIdent("not"))
        {
            i++;
            var inner = InParens(c, ref i, notes) ?? Never;
            return inner == Always ? Never : inner == Never ? Always : "!" + Wrap(inner);
        }

        var result = InParens(c, ref i, notes) ?? Never;
        while (i < c.Count && (c[i].IsIdent("and") || allowOr && c[i].IsIdent("or")))
        {
            var or = c[i].IsIdent("or");
            i++;
            var right = InParens(c, ref i, notes) ?? Never;
            if (or)
            {
                result = result == Always || right == Always ? Always
                    : result == Never ? right : right == Never ? result : Wrap(result) + " || " + Wrap(right);
            }
            else
            {
                result = And(result, right);
            }
        }

        return result;
    }

    private static string? InParens(List<CssValue> c, ref int i, List<string> notes)
    {
        if (i >= c.Count || c[i].Kind != CssTokenKind.LeftParen) return Unsupported(notes, "invalid media query.");
        var inner = CssParser.Components(c[i++].Children!);
        if (inner.Count == 0) return Unsupported(notes, "invalid media query.");
        if (inner[0].Kind == CssTokenKind.LeftParen || inner[0].IsIdent("not"))
        {
            var j = 0;
            var nested = Condition(inner, ref j, notes, allowOr: true);
            return j == inner.Count ? nested : Unsupported(notes, "invalid media query.");
        }

        return Feature(inner, notes);
    }

    /// <summary>The content of one <c>( … )</c>: <c>name</c>, <c>name: value</c> or a range.</summary>
    private static string? Feature(List<CssValue> f, List<string> notes)
    {
        if (f.Count == 1 && f[0].Kind == CssTokenKind.Ident)
        {
            return f[0].Token.Value.ToLowerInvariant() switch
            {
                "width" or "height" or "orientation" or "aspect-ratio" or "color" => Always,
                var name => Unsupported(notes, $"the media feature '{name}' cannot be evaluated; the query never matches."),
            };
        }

        if (f.Count >= 3 && f[0].Kind == CssTokenKind.Ident && f[1].Kind == CssTokenKind.Colon)
        {
            return NameValue(f[0].Token.Value.ToLowerInvariant(), f.GetRange(2, f.Count - 2), notes);
        }

        return Range(f, notes);
    }

    private static string? NameValue(string name, List<CssValue> value, List<string> notes)
    {
        string? keyword = value.Count == 1 && value[0].Kind == CssTokenKind.Ident ? value[0].Token.Value.ToLowerInvariant() : null;
        switch (name)
        {
            case "min-width": return Compare("Width", ">=", value, notes);
            case "max-width": return Compare("Width", "<=", value, notes);
            case "width": return Compare("Width", "==", value, notes);
            case "min-height": return Compare("Height", ">=", value, notes);
            case "max-height": return Compare("Height", "<=", value, notes);
            case "height": return Compare("Height", "==", value, notes);
            case "orientation" when keyword == "portrait": return "v.Height >= v.Width";
            case "orientation" when keyword == "landscape": return "v.Width > v.Height";
            case "min-aspect-ratio": return Ratio(">=", value, notes);
            case "max-aspect-ratio": return Ratio("<=", value, notes);
            case "aspect-ratio": return Ratio("==", value, notes);
            case "prefers-color-scheme" when keyword == "light":
                Unsupported(notes, "Miko has no color scheme setting; 'prefers-color-scheme: light' always matches.");
                return Always;
            case "prefers-reduced-motion" when keyword == "no-preference":
            case "prefers-contrast" when keyword == "no-preference":
                Unsupported(notes, $"Miko has no {name} setting; '{keyword}' always matches.");
                return Always;
            case "prefers-color-scheme" or "prefers-reduced-motion" or "prefers-contrast":
                return Unsupported(notes, $"Miko has no {name} setting; '{name}: {keyword}' never matches.");
            default:
                return Unsupported(notes, $"the media feature '{name}' cannot be evaluated; the query never matches.");
        }
    }

    /// <summary><c>width &gt;= 600px</c>, <c>600px &lt;= width</c>, <c>400px &lt; width &lt;= 800px</c>.</summary>
    private static string? Range(List<CssValue> f, List<string> notes)
    {
        var items = new List<object>();
        for (var i = 0; i < f.Count; i++)
        {
            if (f[i].Kind == CssTokenKind.Delim && f[i].Token.Value is "<" or ">" or "=")
            {
                var op = f[i].Token.Value;
                if (op != "=" && i + 1 < f.Count && f[i + 1].IsDelim('=') && f[i + 1].Start == f[i].Token.End)
                {
                    op += "=";
                    i++;
                }

                items.Add(op == "=" ? "==" : op);
            }
            else
            {
                items.Add(f[i]);
            }
        }

        static bool IsName(object item) => item is CssValue v && v.Kind == CssTokenKind.Ident && v.Token.Value.ToLowerInvariant() is "width" or "height";
        static string Prop(object item) => ((CssValue)item).Token.Value.ToLowerInvariant() == "width" ? "Width" : "Height";
        static string Flip(string op) => op switch { "<" => ">", ">" => "<", "<=" => ">=", ">=" => "<=", _ => op };
        static List<CssValue> One(object item) => new() { (CssValue)item };

        if (items.Count == 3 && IsName(items[0]) && items[1] is string op1 && items[2] is CssValue)
            return Compare(Prop(items[0]), op1, One(items[2]), notes);
        if (items.Count == 3 && items[0] is CssValue && items[1] is string op2 && IsName(items[2]))
            return Compare(Prop(items[2]), Flip(op2), One(items[0]), notes);
        if (items.Count == 5 && items[0] is CssValue && items[1] is string a && IsName(items[2]) && items[3] is string b && items[4] is CssValue)
        {
            var low = Compare(Prop(items[2]), Flip(a), One(items[0]), notes);
            var high = Compare(Prop(items[2]), b, One(items[4]), notes);
            return low == null || high == null ? null : And(low, high);
        }

        return Unsupported(notes, "this media feature cannot be evaluated; the query never matches.");
    }

    private static string? Compare(string property, string op, List<CssValue> value, List<string> notes)
    {
        if (value.Count != 1) return Unsupported(notes, "invalid media feature value.");
        var token = value[0].Token;
        double? px = value[0].Children != null ? null : token.Kind switch
        {
            CssTokenKind.Number when token.Number == 0 => 0,
            CssTokenKind.Dimension => token.Value.ToLowerInvariant() switch
            {
                "px" => token.Number,
                "em" or "rem" => token.Number * 16,
                "pt" => token.Number * 96 / 72,
                _ => null,
            },
            _ => null,
        };

        if (px is not { } length) return Unsupported(notes, "media features support px, em and rem lengths only; the query never matches.");
        return $"v.{property} {op} {CssPropertyTranslator.Float(length)}";
    }

    private static string? Ratio(string op, List<CssValue> value, List<string> notes)
    {
        double w, h;
        if (value.Count == 1 && value[0].Kind == CssTokenKind.Number) { w = value[0].Token.Number; h = 1; }
        else if (value.Count == 3 && value[0].Kind == CssTokenKind.Number && value[1].IsDelim('/') && value[2].Kind == CssTokenKind.Number)
        {
            w = value[0].Token.Number;
            h = value[2].Token.Number;
        }
        else return Unsupported(notes, "invalid aspect-ratio in a media query.");

        if (w <= 0 || h <= 0) return Unsupported(notes, "invalid aspect-ratio in a media query.");
        // width / height op w / h, without dividing by a zero height.
        return $"v.Width * {CssPropertyTranslator.Float(h)} {op} v.Height * {CssPropertyTranslator.Float(w)}";
    }
}
