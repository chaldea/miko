// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

internal static partial class CssSelectorTranslator
{
    /// <summary>
    /// Parses one complex selector into compounds and renders it back in the form the runtime
    /// parser reads: single spaces around combinators, canonical An+B, double-quoted attribute values.
    /// </summary>
    private sealed partial class SelectorParser
    {
        private readonly string _text;
        private readonly string? _scope;
        private readonly bool _allowPseudoElements;
        private readonly bool _nested;
        private readonly List<string> _notes;
        private int _i;

        private sealed class Compound
        {
            public char Combinator = ' ';
            public string Body = string.Empty;
            public string? PseudoElement;

            /// <summary>Followed by <c>::deep</c>: the scope goes here.</summary>
            public bool DeepAfter;
        }

        public SelectorParser(string text, string? scope, bool allowPseudoElements, List<string> notes, bool nested = false)
        {
            _text = text;
            _scope = scope;
            _allowPseudoElements = allowPseudoElements && !nested;
            _nested = nested;
            _notes = notes;
        }

        private char Peek(int offset = 0) => _i + offset < _text.Length ? _text[_i + offset] : '\0';

        private void SkipWhitespace()
        {
            while (_i < _text.Length && char.IsWhiteSpace(_text[_i])) _i++;
        }

        private static SelectorException Unsupported(string message) => new(message);

        /// <summary>A complex selector; inside <c>:has()</c> it may start with a combinator.</summary>
        public string ParseComplex(bool relative = false)
        {
            var compounds = new List<Compound>();
            var leadingDeep = false;
            char? leading = null;
            SkipWhitespace();
            if (Peek() is '>' or '+' or '~')
            {
                if (!relative) throw Unsupported($"'{Peek()}' cannot start a selector.");
                leading = Peek();
                _i++;
            }

            var combinator = ' ';
            var danglingCombinator = false;
            while (true)
            {
                SkipWhitespace();
                if (_i >= _text.Length) break;

                if (IsDeep())
                {
                    _i += "::deep".Length;
                    if (compounds.Count == 0) leadingDeep = true;
                    else compounds[compounds.Count - 1].DeepAfter = true;
                    // `.a > ::deep .b` keeps the '>': ::deep itself is not a combinator.
                    var next = ReadCombinator();
                    combinator = next == ' ' ? combinator : next;
                    danglingCombinator = combinator != ' ';
                    continue;
                }

                var compound = ParseCompound();
                compound.Combinator = combinator;
                compounds.Add(compound);
                danglingCombinator = false;
                if (compound.PseudoElement != null)
                {
                    SkipWhitespace();
                    if (_i < _text.Length) throw Unsupported($"'::{compound.PseudoElement}' must be at the end of the selector.");
                    break;
                }

                combinator = ReadCombinator();
                danglingCombinator = combinator != ' ';
            }

            if (compounds.Count == 0 || compounds[compounds.Count - 1].DeepAfter)
                throw Unsupported("::deep must be followed by a selector.");
            if (danglingCombinator) throw Unsupported($"'{combinator}' must be followed by a selector.");
            return Render(compounds, leadingDeep, leading);
        }

        private bool IsDeep()
        {
            if (string.Compare(_text, _i, "::deep", 0, 6, StringComparison.OrdinalIgnoreCase) != 0) return false;
            var after = _i + 6 < _text.Length ? _text[_i + 6] : '\0';
            if (char.IsLetterOrDigit(after) || after is '-' or '_') return false;
            if (_scope == null || _nested) throw Unsupported("::deep is only meaningful in a .razor.css file.");
            return true;
        }

        /// <summary>Reads whitespace and an optional explicit combinator; ' ' means descendant.</summary>
        private char ReadCombinator()
        {
            SkipWhitespace();
            var c = Peek();
            if (c == '|' && Peek(1) == '|') throw Unsupported("the column combinator '||' is not supported.");
            if (c is not ('>' or '+' or '~')) return ' ';
            _i++;
            SkipWhitespace();
            return c;
        }

        private string Render(List<Compound> compounds, bool leadingDeep, char? leading)
        {
            var scopeIndex = -1;
            if (_scope != null && !leadingDeep)
            {
                scopeIndex = compounds.FindIndex(c => c.DeepAfter);
                if (scopeIndex < 0) scopeIndex = compounds.Count - 1;
            }

            var builder = new StringBuilder();
            if (leading is { } l) builder.Append(l).Append(' ');
            if (leadingDeep) builder.Append('[').Append(_scope).Append(']');

            for (var k = 0; k < compounds.Count; k++)
            {
                var compound = compounds[k];
                if (k > 0 || leadingDeep)
                {
                    builder.Append(compound.Combinator == ' ' ? " " : $" {compound.Combinator} ");
                }

                var body = compound.Body;
                if (body.Length == 0 && k != scopeIndex) body = "*";
                builder.Append(body);
                if (k == scopeIndex) builder.Append('[').Append(_scope).Append(']');
                if (compound.PseudoElement != null) builder.Append("::").Append(compound.PseudoElement);
            }

            return builder.ToString();
        }
    }
}
