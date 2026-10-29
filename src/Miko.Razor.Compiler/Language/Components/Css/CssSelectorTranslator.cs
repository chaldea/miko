// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>
/// Validates CSS selectors against what Miko's runtime selector parser (<c>CssSelectorParser</c>)
/// accepts, normalizes them, and applies a <c>.razor.css</c> scope (ISSUE-161).
///
/// <para>Every selector the generated code passes to the runtime has been through here, so a
/// stylesheet can never throw when a component first renders. A selector list is split into its
/// branches — each becomes its own rule, so each keeps its own specificity — and an unsupported
/// branch is reported and dropped while the others still apply.</para>
///
/// <para>Scoping follows Blazor's CSS isolation: <c>[scope]</c> is added to the last compound
/// selector, before a pseudo-element (<c>.a .b::before</c> → <c>.a .b[b-x]::before</c>), or to the
/// compound before <c>::deep</c> (<c>.a ::deep .b</c> → <c>.a[b-x] .b</c>, <c>::deep .b</c> →
/// <c>[b-x] .b</c>).</para>
/// </summary>
internal static partial class CssSelectorTranslator
{
    private static readonly HashSet<string> s_pseudoClasses = new(StringComparer.Ordinal)
    {
        "hover", "active", "focus", "focus-within", "disabled", "enabled", "first-child", "last-child",
        "first-of-type", "last-of-type", "empty", "checked", "root", "only-child", "only-of-type",
    };

    private static readonly HashSet<string> s_pseudoElements = new(StringComparer.Ordinal)
    {
        "before", "after", "placeholder", "range-thumb", "range-track", "range-progress",
    };

    /// <summary>An unsupported selector. <see cref="Silent"/>: a vendor-prefixed one, dropped without a warning.</summary>
    private sealed class SelectorException(string message, bool silent = false) : Exception(message)
    {
        public bool Silent { get; } = silent;
    }

    /// <summary>
    /// The supported branches of <paramref name="selectorList"/>, normalized and scoped.
    /// </summary>
    /// <param name="allowPseudoElements">False inside <c>@media</c>: the runtime only resolves pseudo-element rules outside media rules.</param>
    public static List<string> Translate(string selectorList, int start, int length, string? scope,
        bool allowPseudoElements, List<CssDiagnostic> diagnostics)
    {
        var results = new List<string>();
        foreach (var branch in SplitTopLevel(selectorList, ','))
        {
            var text = branch.Trim();
            if (text.Length == 0)
            {
                diagnostics.Add(new CssDiagnostic(CssDiagnosticKind.Invalid, start, length, $"empty selector in '{selectorList}'."));
                continue;
            }

            try
            {
                var notes = new List<string>();
                var normalized = new SelectorParser(text, scope, allowPseudoElements, notes).ParseComplex();
                foreach (var note in notes)
                {
                    diagnostics.Add(new CssDiagnostic(CssDiagnosticKind.Approximated, start, length, text, note));
                }

                if (!results.Contains(normalized)) results.Add(normalized);
            }
            catch (SelectorException e) when (!e.Silent)
            {
                diagnostics.Add(new CssDiagnostic(CssDiagnosticKind.UnsupportedSelector, start, length, text, e.Message));
            }
            catch (SelectorException)
            {
                // Vendor-prefixed (::-webkit-…): a copy of a standard rule for another engine.
            }
        }

        return results;
    }

    /// <summary>Splits at <paramref name="separator"/> outside quotes, parentheses and brackets.</summary>
    internal static List<string> SplitTopLevel(string text, char separator)
    {
        var parts = new List<string>();
        var depth = 0;
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c is '"' or '\'')
            {
                var close = text.IndexOf(c, i + 1);
                i = close < 0 ? text.Length - 1 : close;
            }
            else if (c is '(' or '[') depth++;
            else if (c is ')' or ']') depth--;
            else if (c == separator && depth == 0)
            {
                parts.Add(text.Substring(start, i - start));
                start = i + 1;
            }
        }

        parts.Add(text.Substring(start));
        return parts;
    }
}
