// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

internal enum CssTokenKind
{
    Whitespace,
    Ident,
    Function,
    AtKeyword,
    Hash,
    String,
    Url,
    Number,
    Percentage,
    Dimension,
    Delim,
    Colon,
    Semicolon,
    Comma,
    LeftBrace,
    RightBrace,
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,

    /// <summary>A C# expression in a <c>style</c> attribute (<see cref="CssPlaceholder"/>); <see cref="CssToken.Value"/> is a unit, if any.</summary>
    Placeholder,
    EndOfFile,
}

internal readonly struct CssToken
{
    public CssToken(CssTokenKind kind, int start, int length, string value = "", double number = 0, bool isInteger = false, int index = -1)
    {
        Kind = kind;
        Start = start;
        Length = length;
        Value = value;
        Number = number;
        IsInteger = isInteger;
        Index = index;
    }

    public CssTokenKind Kind { get; }
    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;

    /// <summary>
    /// Ident / function / at-keyword / hash name (escapes decoded), string or url content, unit of a
    /// dimension, the character of a delim.
    /// </summary>
    public string Value { get; }

    public double Number { get; }
    public bool IsInteger { get; }

    /// <summary>The expression index of a <see cref="CssTokenKind.Placeholder"/>.</summary>
    public int Index { get; }

    public bool IsDelim(char c) => Kind == CssTokenKind.Delim && Value.Length == 1 && Value[0] == c;

    public bool IsIdent(string name) => Kind == CssTokenKind.Ident && string.Equals(Value, name, StringComparison.OrdinalIgnoreCase);

    public CssToken WithEnd(int end) => new(Kind, Start, end - Start, Value, Number, IsInteger, Index);
}

/// <summary>
/// Marks the C# expressions of a <c>style</c> attribute in the CSS text (<c>width: @(w)px</c>): each
/// becomes <c>U+E000 n U+E001</c>, n its index, which the tokenizer reads as one placeholder token.
/// Private-use characters cannot appear in the authored CSS.
/// </summary>
internal static class CssPlaceholder
{
    public const char Open = '\uE000';
    public const char Close = '\uE001';

    public static string Encode(int index) => Open + index.ToString(CultureInfo.InvariantCulture) + Close;
}

/// <summary>A CSS Syntax Level 3 tokenizer, reduced to what the translator needs (ISSUE-161).</summary>
internal static class CssTokenizer
{
    public static List<CssToken> Tokenize(string text)
    {
        var tokens = new List<CssToken>();
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c == '/' && Peek(text, i + 1) == '*')
            {
                var end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? text.Length : end + 2;
                continue;
            }

            var start = i;
            if (IsWhitespace(c))
            {
                while (i < text.Length && IsWhitespace(text[i])) i++;
                tokens.Add(new CssToken(CssTokenKind.Whitespace, start, i - start));
                continue;
            }

            if (c is '"' or '\'')
            {
                tokens.Add(ReadString(text, ref i));
                continue;
            }

            if (c == CssPlaceholder.Open)
            {
                tokens.Add(ReadPlaceholder(text, ref i));
                continue;
            }

            if (StartsNumber(text, i))
            {
                tokens.Add(ReadNumeric(text, ref i));
                continue;
            }

            if (StartsIdent(text, i))
            {
                tokens.Add(ReadIdentLike(text, ref i));
                continue;
            }

            switch (c)
            {
                case '#' when IsNameChar(Peek(text, i + 1)) || StartsEscape(text, i + 1):
                    i++;
                    tokens.Add(new CssToken(CssTokenKind.Hash, start, 0, ReadName(text, ref i)).WithEnd(i));
                    continue;
                case '@' when StartsIdent(text, i + 1):
                    i++;
                    tokens.Add(new CssToken(CssTokenKind.AtKeyword, start, 0, ReadName(text, ref i)).WithEnd(i));
                    continue;
            }

            i++;
            var kind = c switch
            {
                ':' => CssTokenKind.Colon,
                ';' => CssTokenKind.Semicolon,
                ',' => CssTokenKind.Comma,
                '{' => CssTokenKind.LeftBrace,
                '}' => CssTokenKind.RightBrace,
                '(' => CssTokenKind.LeftParen,
                ')' => CssTokenKind.RightParen,
                '[' => CssTokenKind.LeftBracket,
                ']' => CssTokenKind.RightBracket,
                _ => CssTokenKind.Delim,
            };
            tokens.Add(new CssToken(kind, start, 1, c.ToString()));
        }

        tokens.Add(new CssToken(CssTokenKind.EndOfFile, text.Length, 0));
        return tokens;
    }

    private static char Peek(string text, int i) => i < text.Length ? text[i] : '\0';

    private static bool IsWhitespace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';

    private static bool IsNameStart(char c) => char.IsLetter(c) || c == '_' || c > 0x7F;

    private static bool IsNameChar(char c) => IsNameStart(c) || char.IsDigit(c) || c == '-';

    private static bool StartsEscape(string text, int i) => Peek(text, i) == '\\' && Peek(text, i + 1) is not ('\n' or '\0');

    private static bool StartsIdent(string text, int i)
    {
        var c = Peek(text, i);
        if (c == '-')
        {
            var next = Peek(text, i + 1);
            return IsNameStart(next) || next == '-' || StartsEscape(text, i + 1);
        }

        return IsNameStart(c) || StartsEscape(text, i);
    }

    private static bool StartsNumber(string text, int i)
    {
        var c = Peek(text, i);
        if (c is '+' or '-')
        {
            i++;
            c = Peek(text, i);
        }

        return char.IsDigit(c) || (c == '.' && char.IsDigit(Peek(text, i + 1)));
    }

    private static string ReadName(string text, ref int i)
    {
        var builder = new StringBuilder();
        while (i < text.Length)
        {
            if (IsNameChar(text[i]))
            {
                builder.Append(text[i++]);
            }
            else if (StartsEscape(text, i))
            {
                builder.Append(ReadEscape(text, ref i));
            }
            else
            {
                break;
            }
        }

        return builder.ToString();
    }

    // i points at the backslash.
    private static string ReadEscape(string text, ref int i)
    {
        i++;
        var start = i;
        while (i < text.Length && i - start < 6 && char.IsAsciiHexDigit(text[i])) i++;
        if (i == start)
        {
            return text[i++].ToString();
        }

        var code = int.Parse(text.Substring(start, i - start), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        if (i < text.Length && IsWhitespace(text[i])) i++;
        return code is 0 or > 0x10FFFF or (>= 0xD800 and <= 0xDFFF) ? "�" : char.ConvertFromUtf32(code);
    }

    private static CssToken ReadIdentLike(string text, ref int i)
    {
        var start = i;
        var name = ReadName(text, ref i);
        if (Peek(text, i) != '(')
        {
            return new CssToken(CssTokenKind.Ident, start, i - start, name);
        }

        i++;
        if (!string.Equals(name, "url", StringComparison.OrdinalIgnoreCase))
        {
            return new CssToken(CssTokenKind.Function, start, i - start, name);
        }

        // url(…) — a quoted argument is an ordinary function; an unquoted one is one token.
        var j = i;
        while (j < text.Length && IsWhitespace(text[j])) j++;
        if (Peek(text, j) is '"' or '\'')
        {
            return new CssToken(CssTokenKind.Function, start, i - start, name);
        }

        var close = text.IndexOf(')', j);
        var end = close < 0 ? text.Length : close;
        i = close < 0 ? text.Length : close + 1;
        return new CssToken(CssTokenKind.Url, start, i - start, text.Substring(j, end - j).Trim());
    }

    private static CssToken ReadString(string text, ref int i)
    {
        var start = i;
        var quote = text[i++];
        var builder = new StringBuilder();
        while (i < text.Length && text[i] != quote && text[i] != '\n')
        {
            if (text[i] == '\\')
            {
                if (Peek(text, i + 1) == '\n')
                {
                    i += 2;
                    continue;
                }

                if (i + 1 >= text.Length)
                {
                    i++;
                    break;
                }

                builder.Append(ReadEscape(text, ref i));
                continue;
            }

            builder.Append(text[i++]);
        }

        if (i < text.Length && text[i] == quote) i++;
        return new CssToken(CssTokenKind.String, start, i - start, builder.ToString());
    }

    private static CssToken ReadPlaceholder(string text, ref int i)
    {
        var start = i++;
        var digits = i;
        while (i < text.Length && char.IsDigit(text[i])) i++;
        var index = int.Parse(text.Substring(digits, i - digits), CultureInfo.InvariantCulture);
        if (Peek(text, i) == CssPlaceholder.Close) i++;

        // A unit right after the expression: @(w)px, @(p)%.
        var unit = string.Empty;
        if (Peek(text, i) == '%')
        {
            unit = "%";
            i++;
        }
        else if (StartsIdent(text, i))
        {
            unit = ReadName(text, ref i);
        }

        return new CssToken(CssTokenKind.Placeholder, start, i - start, unit, index: index);
    }

    private static CssToken ReadNumeric(string text, ref int i)
    {
        var start = i;
        if (text[i] is '+' or '-') i++;
        var isInteger = true;
        while (i < text.Length && char.IsDigit(text[i])) i++;
        if (Peek(text, i) == '.' && char.IsDigit(Peek(text, i + 1)))
        {
            isInteger = false;
            i++;
            while (i < text.Length && char.IsDigit(text[i])) i++;
        }

        if (Peek(text, i) is 'e' or 'E')
        {
            var j = i + 1;
            if (Peek(text, j) is '+' or '-') j++;
            if (char.IsDigit(Peek(text, j)))
            {
                isInteger = false;
                i = j;
                while (i < text.Length && char.IsDigit(text[i])) i++;
            }
        }

        var number = double.Parse(text.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        if (Peek(text, i) == '%')
        {
            i++;
            return new CssToken(CssTokenKind.Percentage, start, i - start, "%", number, isInteger);
        }

        if (StartsIdent(text, i))
        {
            var unit = ReadName(text, ref i);
            return new CssToken(CssTokenKind.Dimension, start, i - start, unit, number, isInteger);
        }

        return new CssToken(CssTokenKind.Number, start, i - start, string.Empty, number, isInteger);
    }
}
