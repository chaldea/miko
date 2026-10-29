// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

internal static partial class CssSelectorTranslator
{
    private sealed partial class SelectorParser
    {
        private static bool IsIdentChar(char c) => char.IsLetterOrDigit(c) || c is '-' or '_';

        private string ReadIdent()
        {
            var start = _i;
            while (_i < _text.Length && IsIdentChar(_text[_i])) _i++;
            if (Peek() == '\\') throw Unsupported("escaped characters in selectors are not supported.");
            return _text.Substring(start, _i - start);
        }

        private Compound ParseCompound()
        {
            var compound = new Compound();
            var body = new StringBuilder();
            var c = Peek();
            if (c == '*')
            {
                _i++;
                body.Append('*');
            }
            else if (IsIdentChar(c))
            {
                var tag = ReadIdent().ToLowerInvariant();
                if (tag is "html" or "body")
                {
                    _notes.Add($"Miko has no <{tag}> element; '{tag}' is treated as :root, the root element of the component tree.");
                    body.Append(":root");
                }
                else
                {
                    body.Append(tag);
                }
            }

            if (Peek() == '|') throw Unsupported("namespace prefixes are not supported.");

            while (_i < _text.Length)
            {
                c = Peek();
                if (c == '#' || c == '.')
                {
                    _i++;
                    var name = ReadIdent();
                    if (name.Length == 0) throw Unsupported($"'{c}' must be followed by a name.");
                    body.Append(c).Append(name);
                }
                else if (c == '[')
                {
                    body.Append(ParseAttribute());
                }
                else if (c == ':')
                {
                    if (IsDeep()) break;
                    if (Peek(1) == ':')
                    {
                        _i += 2;
                        compound.PseudoElement = ParsePseudoElement();
                        break;
                    }

                    _i++;
                    var pseudo = ParsePseudoClass(out var element);
                    if (element != null)
                    {
                        compound.PseudoElement = element;
                        break;
                    }

                    body.Append(pseudo);
                }
                else if (c == '&')
                {
                    throw Unsupported("'&' nesting is not supported.");
                }
                else if (char.IsWhiteSpace(c) || c is '>' or '+' or '~' or ')' || (c == '|' && Peek(1) == '|'))
                {
                    break;
                }
                else
                {
                    throw Unsupported(c == '\\' ? "escaped characters in selectors are not supported." : $"unexpected character '{c}'.");
                }
            }

            compound.Body = body.ToString();
            if (compound.Body.Length == 0 && compound.PseudoElement == null)
            {
                throw Unsupported($"unexpected character '{Peek()}'.");
            }

            if (compound.PseudoElement != null && !_allowPseudoElements)
            {
                throw Unsupported(_nested
                    ? $"'::{compound.PseudoElement}' cannot be used inside a functional pseudo-class."
                    : $"'::{compound.PseudoElement}' is not supported inside @media.");
            }

            return compound;
        }

        private string ParsePseudoElement()
        {
            var name = ReadIdent().ToLowerInvariant();
            if (name.StartsWith("-", StringComparison.Ordinal)) throw new SelectorException("vendor-prefixed pseudo-element", silent: true);
            if (Peek() == '(') throw Unsupported($"'::{name}()' is not supported.");
            if (!s_pseudoElements.Contains(name)) throw Unsupported($"the pseudo-element '::{name}' is not supported.");
            return name;
        }

        /// <summary>A pseudo-class (normalized), or — for the legacy <c>:before</c> / <c>:after</c> — a pseudo-element.</summary>
        private string ParsePseudoClass(out string? pseudoElement)
        {
            pseudoElement = null;
            var name = ReadIdent().ToLowerInvariant();
            if (name.Length == 0) throw Unsupported("':' must be followed by a name.");
            if (name.StartsWith("-", StringComparison.Ordinal)) throw new SelectorException("vendor-prefixed pseudo-class", silent: true);

            if (Peek() == '(')
            {
                _i++;
                var argument = ReadArgument();
                return ParseFunctional(name, argument);
            }

            if (name is "before" or "after")
            {
                pseudoElement = name;
                return string.Empty;
            }

            if (s_pseudoClasses.Contains(name)) return ":" + name;
            throw Unsupported($"the pseudo-class ':{name}' is not supported.");
        }

        /// <summary>After <c>(</c>: the text up to the matching <c>)</c>, which is consumed.</summary>
        private string ReadArgument()
        {
            var start = _i;
            var depth = 1;
            while (_i < _text.Length)
            {
                var c = _text[_i];
                if (c is '"' or '\'')
                {
                    var close = _text.IndexOf(c, _i + 1);
                    _i = close < 0 ? _text.Length : close + 1;
                    continue;
                }

                if (c == '(') depth++;
                else if (c == ')' && --depth == 0)
                {
                    var argument = _text.Substring(start, _i - start);
                    _i++;
                    return argument;
                }

                _i++;
            }

            throw Unsupported("unclosed '('.");
        }

        private string ParseFunctional(string name, string argument)
        {
            switch (name)
            {
                case "not" or "is" or "where":
                    return $":{name}({ParseNestedList(argument, relative: false)})";
                case "has":
                    return $":has({ParseNestedList(argument, relative: true)})";
                case "nth-child" or "nth-last-child":
                    return $":{name}({ParseNth(argument, allowOf: true)})";
                case "nth-of-type" or "nth-last-of-type":
                    return $":{name}({ParseNth(argument, allowOf: false)})";
                default:
                    throw Unsupported($"the pseudo-class ':{name}()' is not supported.");
            }
        }

        private string ParseNestedList(string argument, bool relative)
        {
            var branches = new List<string>();
            foreach (var branch in SplitTopLevel(argument, ','))
            {
                if (branch.Trim().Length == 0) throw Unsupported("empty selector in a functional pseudo-class.");
                branches.Add(new SelectorParser(branch.Trim(), scope: null, allowPseudoElements: false, _notes, nested: true).ParseComplex(relative));
            }

            return string.Join(", ", branches);
        }

        private string ParseNth(string argument, bool allowOf)
        {
            var text = argument.Trim();
            string? of = null;
            var ofAt = IndexOfOf(text);
            if (ofAt >= 0)
            {
                if (!allowOf) throw Unsupported("'of S' is only allowed in :nth-child() and :nth-last-child().");
                of = ParseNestedList(text.Substring(ofAt + 2), relative: false);
                text = text.Substring(0, ofAt).Trim();
            }

            if (!TryParseAnPlusB(text, out var a, out var b)) throw Unsupported($"'{text}' is not a valid An+B expression.");
            var formatted = a == 0
                ? b.ToString(CultureInfo.InvariantCulture)
                : (a == 1 ? "" : a == -1 ? "-" : a.ToString(CultureInfo.InvariantCulture)) + "n"
                  + (b == 0 ? "" : b > 0 ? "+" + b.ToString(CultureInfo.InvariantCulture) : b.ToString(CultureInfo.InvariantCulture));
            return of == null ? formatted : formatted + " of " + of;
        }

        private static int IndexOfOf(string s)
        {
            for (var i = 1; i + 2 < s.Length; i++)
            {
                if (char.IsWhiteSpace(s[i - 1]) && s[i] is 'o' or 'O' && s[i + 1] is 'f' or 'F' && char.IsWhiteSpace(s[i + 2])) return i;
            }

            return -1;
        }

        /// <summary>The An+B grammar as the runtime parser reads it (<c>CssSelectorParser.ParseAnPlusB</c>).</summary>
        private static bool TryParseAnPlusB(string s, out int a, out int b)
        {
            a = 0;
            b = 0;
            if (s.Equals("odd", StringComparison.OrdinalIgnoreCase)) { a = 2; b = 1; return true; }
            if (s.Equals("even", StringComparison.OrdinalIgnoreCase)) { a = 2; return true; }

            var n = s.IndexOfAny(new[] { 'n', 'N' });
            if (n < 0) return TryParseSigned(s, out b);

            var aPart = s.Substring(0, n);
            if (aPart is "" or "+") a = 1;
            else if (aPart == "-") a = -1;
            else if (!TryParseSigned(aPart, out a)) return false;

            var rest = s.Substring(n + 1).TrimStart();
            if (rest.Length == 0) return true;
            if (rest[0] is not ('+' or '-')) return false;
            var digits = rest.Substring(1).TrimStart();
            if (digits.Length == 0 || !IsDigits(digits) || !int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out b)) return false;
            if (rest[0] == '-') b = -b;
            return true;
        }

        private static bool TryParseSigned(string s, out int value)
        {
            value = 0;
            var digits = s.Length > 0 && s[0] is '+' or '-' ? s.Substring(1) : s;
            return digits.Length > 0 && IsDigits(digits)
                && int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        private static bool IsDigits(string s)
        {
            foreach (var c in s)
            {
                if (c is < '0' or > '9') return false;
            }

            return true;
        }

        /// <summary>At <c>[</c>: <c>[name]</c> or <c>[name op "value" i?]</c>.</summary>
        private string ParseAttribute()
        {
            _i++;
            SkipWhitespace();
            var name = ReadIdent();
            if (name.Length == 0) throw Unsupported(Peek() == '|' || Peek() == '*' ? "namespace prefixes are not supported." : "expected an attribute name.");
            if (Peek() == '|' && Peek(1) != '=') throw Unsupported("namespace prefixes are not supported.");
            SkipWhitespace();
            if (Peek() == ']')
            {
                _i++;
                return $"[{name}]";
            }

            string op;
            if (Peek() == '=') { op = "="; _i++; }
            else if (Peek() is '~' or '|' or '^' or '$' or '*' && Peek(1) == '=') { op = _text.Substring(_i, 2); _i += 2; }
            else throw Unsupported($"unexpected character '{Peek()}' in an attribute selector.");

            SkipWhitespace();
            string value;
            if (Peek() is '"' or '\'')
            {
                var quote = Peek();
                var close = _text.IndexOf(quote, _i + 1);
                if (close < 0) throw Unsupported("unclosed string in an attribute selector.");
                value = _text.Substring(_i + 1, close - _i - 1);
                if (value.IndexOf('\\') >= 0) throw Unsupported("escaped characters in selectors are not supported.");
                _i = close + 1;
            }
            else
            {
                value = ReadIdent();
                if (value.Length == 0) throw Unsupported("expected an attribute value.");
            }

            SkipWhitespace();
            if (Peek() is 'i' or 'I' && !IsIdentChar(Peek(1)))
            {
                // Case-insensitive flag: Miko already compares attribute values case-insensitively.
                _i++;
                SkipWhitespace();
            }
            else if (Peek() is 's' or 'S' && !IsIdentChar(Peek(1)))
            {
                throw Unsupported("the case-sensitive attribute flag 's' is not supported; Miko compares attribute values case-insensitively.");
            }

            if (Peek() != ']') throw Unsupported("expected ']' to close the attribute selector.");
            _i++;

            if (value.IndexOf('"') < 0) return $"[{name}{op}\"{value}\"]";
            if (value.IndexOf('\'') < 0) return $"[{name}{op}'{value}']";
            throw Unsupported("an attribute value cannot contain both quote characters.");
        }
    }
}
