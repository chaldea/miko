// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>
/// Parses a stylesheet (<c>&lt;style&gt;</c> content, a <c>.razor.css</c> file) or a declaration list
/// (a <c>style</c> attribute) into rules and declarations (ISSUE-161). Errors are reported and the
/// parser recovers the way a browser does: a bad declaration is skipped up to the next <c>;</c>, a
/// bad rule up to the end of its block.
/// </summary>
internal sealed class CssParser
{
    private readonly string _text;
    private readonly List<CssToken> _tokens;
    private readonly List<CssDiagnostic> _diagnostics;
    private int _index;

    private CssParser(string text, List<CssDiagnostic> diagnostics)
    {
        _text = text;
        _tokens = CssTokenizer.Tokenize(text);
        _diagnostics = diagnostics;
    }

    public static CssStyleSheetNode ParseStyleSheet(string text, List<CssDiagnostic> diagnostics)
    {
        var parser = new CssParser(text, diagnostics);
        var sheet = new CssStyleSheetNode();
        parser.ParseRules(sheet.Rules, topLevel: true);
        return sheet;
    }

    public static List<CssDeclarationNode> ParseDeclarationList(string text, List<CssDiagnostic> diagnostics)
    {
        var parser = new CssParser(text, diagnostics);
        return parser.ParseDeclarations(inBlock: false);
    }

    private CssToken Current => _tokens[_index];

    private void Report(CssDiagnosticKind kind, int start, int length, params string[] args)
        => _diagnostics.Add(new CssDiagnostic(kind, start, Math.Max(length, 1), args));

    private void SkipWhitespace()
    {
        while (Current.Kind == CssTokenKind.Whitespace) _index++;
    }

    private void ParseRules(List<CssRuleNode> rules, bool topLevel)
    {
        while (true)
        {
            SkipWhitespace();
            var token = Current;
            switch (token.Kind)
            {
                case CssTokenKind.EndOfFile:
                    return;
                case CssTokenKind.RightBrace when !topLevel:
                    return;
                case CssTokenKind.RightBrace:
                    Report(CssDiagnosticKind.Invalid, token.Start, 1, "unexpected '}'.");
                    _index++;
                    continue;
                case CssTokenKind.Semicolon:
                    _index++;
                    continue;
                case CssTokenKind.AtKeyword:
                    if (ParseAtRule(nested: !topLevel) is { } atRule) rules.Add(atRule);
                    continue;
                default:
                    if (ParseStyleRule() is { } rule) rules.Add(rule);
                    continue;
            }
        }
    }

    private CssStyleRuleNode? ParseStyleRule()
    {
        var start = _index;
        var depth = 0;
        while (true)
        {
            var token = Current;
            if (token.Kind == CssTokenKind.EndOfFile)
            {
                Report(CssDiagnosticKind.Invalid, _tokens[start].Start, token.Start - _tokens[start].Start, "expected '{' after the selector.");
                return null;
            }

            if (depth == 0 && token.Kind == CssTokenKind.LeftBrace) break;
            if (depth == 0 && token.Kind == CssTokenKind.RightBrace)
            {
                Report(CssDiagnosticKind.Invalid, _tokens[start].Start, token.Start - _tokens[start].Start, "expected '{' after the selector.");
                return null;
            }

            if (token.Kind is CssTokenKind.LeftParen or CssTokenKind.LeftBracket or CssTokenKind.Function) depth++;
            else if (token.Kind is CssTokenKind.RightParen or CssTokenKind.RightBracket) depth = Math.Max(0, depth - 1);
            _index++;
        }

        var selector = JoinRaw(start, _index).Trim();
        var selectorStart = _tokens[start].Start;
        var selectorLength = Math.Max(Current.Start - selectorStart, 1);
        _index++; // {
        var declarations = ParseDeclarations(inBlock: true);
        return new CssStyleRuleNode(selector, selectorStart, selectorLength, declarations);
    }

    /// <summary>
    /// The source text of tokens [from, to), with comments (which produce no token) dropped and
    /// whitespace runs collapsed to one space — the runtime selector parser only treats ' ' as
    /// whitespace.
    /// </summary>
    private string JoinRaw(int from, int to)
    {
        var builder = new StringBuilder();
        for (var i = from; i < to; i++)
        {
            var token = _tokens[i];
            if (token.Kind == CssTokenKind.Whitespace)
            {
                builder.Append(' ');
            }
            else
            {
                if (i > from && _tokens[i - 1].End != token.Start && _tokens[i - 1].Kind != CssTokenKind.Whitespace)
                {
                    // A comment separated two tokens: CSS treats it as a token boundary.
                    builder.Append(' ');
                }

                builder.Append(_text, token.Start, token.Length);
            }
        }

        return builder.ToString();
    }

    /// <summary>Declarations up to the closing <c>}</c> (consumed) or, outside a block, the end.</summary>
    private List<CssDeclarationNode> ParseDeclarations(bool inBlock)
    {
        var declarations = new List<CssDeclarationNode>();
        while (true)
        {
            SkipWhitespace();
            var token = Current;
            switch (token.Kind)
            {
                case CssTokenKind.EndOfFile:
                    if (inBlock) Report(CssDiagnosticKind.Invalid, token.Start, 1, "expected '}'.");
                    return declarations;
                case CssTokenKind.RightBrace when inBlock:
                    _index++;
                    return declarations;
                case CssTokenKind.Semicolon:
                    _index++;
                    continue;
                case CssTokenKind.AtKeyword:
                    Report(CssDiagnosticKind.UnsupportedAtRule, token.Start, token.Length, token.Value, " inside a declaration block.");
                    SkipBadDeclaration(out _);
                    continue;
                case CssTokenKind.Ident:
                    if (ParseDeclaration() is { } declaration) declarations.Add(declaration);
                    continue;
                default:
                    var nested = SkipBadDeclaration(out var braceStart);
                    if (nested)
                        Report(CssDiagnosticKind.UnsupportedSelector, token.Start, braceStart - token.Start,
                            _text.Substring(token.Start, braceStart - token.Start).Trim(), "nested rules are not supported.");
                    else
                        Report(CssDiagnosticKind.Invalid, token.Start, Math.Max(PrevEnd() - token.Start, 1), "expected a property name.");
                    continue;
            }
        }
    }

    private int PrevEnd() => _index > 0 ? _tokens[_index - 1].End : 0;

    /// <summary>
    /// Skips to after the next top-level <c>;</c>, or over a nested <c>{…}</c> block, stopping before
    /// the <c>}</c> that closes the enclosing block. Returns whether a nested block was skipped.
    /// </summary>
    private bool SkipBadDeclaration(out int braceStart)
    {
        braceStart = -1;
        var depth = 0;
        while (true)
        {
            var token = Current;
            switch (token.Kind)
            {
                case CssTokenKind.EndOfFile:
                    return false;
                case CssTokenKind.Semicolon when depth == 0:
                    _index++;
                    return false;
                case CssTokenKind.RightBrace when depth == 0:
                    return false;
                case CssTokenKind.LeftBrace when depth == 0:
                    braceStart = token.Start;
                    SkipBlock();
                    return true;
                case CssTokenKind.LeftParen or CssTokenKind.LeftBracket or CssTokenKind.Function or CssTokenKind.LeftBrace:
                    depth++;
                    break;
                case CssTokenKind.RightParen or CssTokenKind.RightBracket or CssTokenKind.RightBrace:
                    depth--;
                    break;
            }

            _index++;
        }
    }

    /// <summary>At a <c>{</c>: skips to after its matching <c>}</c>.</summary>
    private void SkipBlock()
    {
        var depth = 0;
        while (Current.Kind != CssTokenKind.EndOfFile)
        {
            var kind = Current.Kind;
            _index++;
            if (kind == CssTokenKind.LeftBrace) depth++;
            else if (kind == CssTokenKind.RightBrace && --depth == 0) return;
        }
    }

    private CssDeclarationNode? ParseDeclaration()
    {
        var nameToken = Current;
        var start = _index;
        _index++;
        SkipWhitespace();
        if (Current.Kind != CssTokenKind.Colon)
        {
            var nested = SkipBadDeclaration(out var braceStart);
            if (nested)
                Report(CssDiagnosticKind.UnsupportedSelector, nameToken.Start, braceStart - nameToken.Start,
                    _text.Substring(nameToken.Start, braceStart - nameToken.Start).Trim(), "nested rules are not supported.");
            else
                Report(CssDiagnosticKind.Invalid, nameToken.Start, nameToken.Length, $"expected ':' after '{nameToken.Value}'.");
            return null;
        }

        var name = nameToken.Value.StartsWith("--", StringComparison.Ordinal) ? nameToken.Value : nameToken.Value.ToLowerInvariant();
        var custom = name.StartsWith("--", StringComparison.Ordinal);
        _index++; // :

        var value = new List<CssToken>();
        var depth = 0;
        while (true)
        {
            var token = Current;
            if (token.Kind == CssTokenKind.EndOfFile) break;
            if (depth == 0 && token.Kind is CssTokenKind.Semicolon or CssTokenKind.RightBrace) break;
            if (depth == 0 && token.Kind == CssTokenKind.LeftBrace && !custom)
            {
                // `a:hover { … }` inside a block: a nested rule, not a declaration.
                var braceStart = token.Start;
                SkipBlock();
                Report(CssDiagnosticKind.UnsupportedSelector, nameToken.Start, braceStart - nameToken.Start,
                    _text.Substring(nameToken.Start, braceStart - nameToken.Start).Trim(), "nested rules are not supported.");
                return null;
            }

            if (token.Kind is CssTokenKind.LeftParen or CssTokenKind.LeftBracket or CssTokenKind.Function or CssTokenKind.LeftBrace) depth++;
            else if (token.Kind is CssTokenKind.RightParen or CssTokenKind.RightBracket or CssTokenKind.RightBrace) depth--;
            value.Add(token);
            _index++;
        }

        var end = PrevEnd();
        if (Current.Kind == CssTokenKind.Semicolon) _index++;

        Trim(value);
        var important = false;
        if (value.Count >= 2 && value[value.Count - 1].IsIdent("important"))
        {
            var bang = value.Count - 2;
            while (bang >= 0 && value[bang].Kind == CssTokenKind.Whitespace) bang--;
            if (bang >= 0 && value[bang].IsDelim('!'))
            {
                important = true;
                value.RemoveRange(bang, value.Count - bang);
                Trim(value);
            }
        }

        var declStart = _tokens[start].Start;
        if (value.Count == 0 && !custom)
        {
            Report(CssDiagnosticKind.Invalid, declStart, Math.Max(end - declStart, 1), $"'{name}' has no value.");
            return null;
        }

        return new CssDeclarationNode(name, nameToken.Start, value, important, declStart, Math.Max(end - declStart, 1));
    }

    private static void Trim(List<CssToken> tokens)
    {
        while (tokens.Count > 0 && tokens[tokens.Count - 1].Kind == CssTokenKind.Whitespace) tokens.RemoveAt(tokens.Count - 1);
        while (tokens.Count > 0 && tokens[0].Kind == CssTokenKind.Whitespace) tokens.RemoveAt(0);
    }

    private CssRuleNode? ParseAtRule(bool nested)
    {
        var keyword = Current;
        var name = keyword.Value.ToLowerInvariant();
        _index++;

        var prelude = new List<CssToken>();
        var depth = 0;
        while (true)
        {
            var token = Current;
            if (token.Kind == CssTokenKind.EndOfFile) break;
            if (depth == 0 && token.Kind is CssTokenKind.Semicolon or CssTokenKind.LeftBrace or CssTokenKind.RightBrace) break;
            if (token.Kind is CssTokenKind.LeftParen or CssTokenKind.LeftBracket or CssTokenKind.Function) depth++;
            else if (token.Kind is CssTokenKind.RightParen or CssTokenKind.RightBracket) depth--;
            prelude.Add(token);
            _index++;
        }

        Trim(prelude);
        var preludeEnd = PrevEnd();

        if (Current.Kind != CssTokenKind.LeftBrace)
        {
            // Statement at-rule: @import, @charset, @namespace, @layer a, b;
            if (Current.Kind == CssTokenKind.Semicolon) _index++;
            if (name != "charset")
                Report(CssDiagnosticKind.UnsupportedAtRule, keyword.Start, preludeEnd - keyword.Start, name, ".");
            return null;
        }

        if (nested)
        {
            Report(CssDiagnosticKind.UnsupportedAtRule, keyword.Start, preludeEnd - keyword.Start, name, " inside @media.");
            SkipBlock();
            return null;
        }

        switch (name)
        {
            case "media":
            {
                _index++; // {
                var inner = new List<CssRuleNode>();
                ParseRules(inner, topLevel: false);
                if (Current.Kind == CssTokenKind.RightBrace) _index++;
                else Report(CssDiagnosticKind.Invalid, Current.Start, 1, "expected '}' to close @media.");
                var rules = new List<CssStyleRuleNode>();
                foreach (var rule in inner)
                {
                    if (rule is CssStyleRuleNode style) rules.Add(style);
                }

                return new CssMediaRuleNode(prelude, keyword.Start, preludeEnd - keyword.Start, rules);
            }

            case "keyframes":
                return ParseKeyframes(keyword, prelude, preludeEnd);

            case "-webkit-keyframes" or "-moz-keyframes" or "-o-keyframes":
                // Vendor copies of a standard @keyframes block.
                SkipBlock();
                return null;

            default:
                Report(CssDiagnosticKind.UnsupportedAtRule, keyword.Start, preludeEnd - keyword.Start, name, ".");
                SkipBlock();
                return null;
        }
    }

    private CssKeyframesRuleNode? ParseKeyframes(CssToken keyword, List<CssToken> prelude, int preludeEnd)
    {
        var frameName = prelude.Count == 1 && prelude[0].Kind is CssTokenKind.Ident or CssTokenKind.String ? prelude[0].Value : null;
        if (frameName is null)
        {
            Report(CssDiagnosticKind.Invalid, keyword.Start, preludeEnd - keyword.Start, "@keyframes needs a name.");
            SkipBlock();
            return null;
        }

        _index++; // {
        var frames = new List<CssKeyframeNode>();
        while (true)
        {
            SkipWhitespace();
            var token = Current;
            if (token.Kind == CssTokenKind.RightBrace)
            {
                _index++;
                break;
            }

            if (token.Kind == CssTokenKind.EndOfFile)
            {
                Report(CssDiagnosticKind.Invalid, token.Start, 1, "expected '}' to close @keyframes.");
                break;
            }

            var offsets = new List<float>();
            var valid = true;
            while (Current.Kind is not (CssTokenKind.LeftBrace or CssTokenKind.RightBrace or CssTokenKind.EndOfFile))
            {
                var part = Current;
                _index++;
                if (part.Kind is CssTokenKind.Whitespace or CssTokenKind.Comma) continue;
                if (part.Kind == CssTokenKind.Percentage && part.Number is >= 0 and <= 100) offsets.Add((float)(part.Number / 100));
                else if (part.IsIdent("from")) offsets.Add(0f);
                else if (part.IsIdent("to")) offsets.Add(1f);
                else
                {
                    Report(CssDiagnosticKind.Invalid, part.Start, part.Length, $"'{_text.Substring(part.Start, part.Length)}' is not a keyframe selector (use from, to or a percentage).");
                    valid = false;
                }
            }

            if (Current.Kind != CssTokenKind.LeftBrace) continue;
            _index++;
            var declarations = ParseDeclarations(inBlock: true);
            if (valid && offsets.Count > 0) frames.Add(new CssKeyframeNode(offsets, declarations));
        }

        return new CssKeyframesRuleNode(frameName, keyword.Start, preludeEnd - keyword.Start, frames);
    }

    /// <summary>Groups value tokens into component values: functions and bracket blocks get their content as children.</summary>
    public static List<CssValue> ParseComponentValues(List<CssToken> tokens)
    {
        var index = 0;
        return ParseComponentValues(tokens, ref index, closing: null);
    }

    private static List<CssValue> ParseComponentValues(List<CssToken> tokens, ref int index, CssTokenKind? closing)
    {
        var values = new List<CssValue>();
        while (index < tokens.Count)
        {
            var token = tokens[index];
            if (closing is { } close && token.Kind == close)
            {
                return values;
            }

            index++;
            CssTokenKind? inner = token.Kind switch
            {
                CssTokenKind.Function or CssTokenKind.LeftParen => CssTokenKind.RightParen,
                CssTokenKind.LeftBracket => CssTokenKind.RightBracket,
                CssTokenKind.LeftBrace => CssTokenKind.RightBrace,
                _ => null,
            };

            if (inner is null)
            {
                values.Add(CssValue.Leaf(token));
                continue;
            }

            var children = ParseComponentValues(tokens, ref index, inner);
            var end = index < tokens.Count ? tokens[index].End : (tokens.Count > 0 ? tokens[tokens.Count - 1].End : token.End);
            if (index < tokens.Count) index++; // closing bracket
            values.Add(CssValue.Group(token, children, end));
        }

        return values;
    }

    /// <summary>Splits at top-level commas. Whitespace around each part is removed.</summary>
    public static List<List<CssValue>> SplitByComma(List<CssValue> values)
    {
        var parts = new List<List<CssValue>> { new() };
        foreach (var value in values)
        {
            if (value.IsComma) parts.Add(new List<CssValue>());
            else parts[parts.Count - 1].Add(value);
        }

        foreach (var part in parts) TrimValues(part);
        return parts;
    }

    /// <summary>The values without whitespace: the space-separated components.</summary>
    public static List<CssValue> Components(List<CssValue> values)
    {
        var components = new List<CssValue>(values.Count);
        foreach (var value in values)
        {
            if (!value.IsWhitespace) components.Add(value);
        }

        return components;
    }

    public static void TrimValues(List<CssValue> values)
    {
        while (values.Count > 0 && values[values.Count - 1].IsWhitespace) values.RemoveAt(values.Count - 1);
        while (values.Count > 0 && values[0].IsWhitespace) values.RemoveAt(0);
    }

    public static string FormatNumber(double value)
        => ((float)value).ToString("R", CultureInfo.InvariantCulture);
}
