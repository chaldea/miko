// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

internal enum CssDiagnosticKind
{
    UnsupportedProperty,
    UnsupportedValue,
    UnsupportedSelector,
    UnsupportedAtRule,
    Invalid,
    Dynamic,
    Approximated,
}

/// <summary>
/// A problem found while translating CSS (ISSUE-161), positioned in the CSS text. The caller maps
/// the position back to the Razor or <c>.razor.css</c> source with a <see cref="CssSourceMap"/>.
/// </summary>
internal readonly struct CssDiagnostic
{
    public CssDiagnostic(CssDiagnosticKind kind, int start, int length, params string[] args)
    {
        Kind = kind;
        Start = start;
        Length = length;
        Args = args;
    }

    public CssDiagnosticKind Kind { get; }

    public int Start { get; }

    public int Length { get; }

    public string[] Args { get; }

    public RazorDiagnosticDescriptor Descriptor => Kind switch
    {
        CssDiagnosticKind.UnsupportedProperty => ComponentDiagnosticFactory.UnsupportedCssProperty,
        CssDiagnosticKind.UnsupportedValue => ComponentDiagnosticFactory.UnsupportedCssValue,
        CssDiagnosticKind.UnsupportedSelector => ComponentDiagnosticFactory.UnsupportedCssSelector,
        CssDiagnosticKind.UnsupportedAtRule => ComponentDiagnosticFactory.UnsupportedCssAtRule,
        CssDiagnosticKind.Dynamic => ComponentDiagnosticFactory.DynamicCss,
        CssDiagnosticKind.Approximated => ComponentDiagnosticFactory.ApproximatedCss,
        _ => ComponentDiagnosticFactory.InvalidCss,
    };

    public RazorDiagnostic ToRazorDiagnostic(CssSourceMap? map)
        => RazorDiagnostic.Create(Descriptor, map?.Map(Start, Length), (object[])Args);

    /// <summary>The formatted message, for tests.</summary>
    public string Message => string.Format(System.Globalization.CultureInfo.InvariantCulture, Descriptor.MessageFormat, Args);
}

/// <summary>
/// Maps positions in CSS text assembled from source fragments (the content tokens of a
/// <c>&lt;style&gt;</c> element or a <c>style</c> attribute, or a whole <c>.razor.css</c> file) back
/// to the source they came from.
/// </summary>
internal sealed class CssSourceMap
{
    private readonly List<Segment> _segments = new();

    private readonly struct Segment
    {
        public Segment(int cssStart, string text, SourceSpan origin, bool exact)
        {
            CssStart = cssStart;
            Text = text;
            Origin = origin;
            Exact = exact;
        }

        public int CssStart { get; }
        public string Text { get; }
        public SourceSpan Origin { get; }

        /// <summary>
        /// Whether <see cref="Text"/> is the source text verbatim. Razor folds <c>@@</c> into one
        /// <c>@</c>, so a fragment that differs from its source maps to the fragment's start.
        /// </summary>
        public bool Exact { get; }
    }

    /// <summary>A map for a file whose text is the CSS itself.</summary>
    public static CssSourceMap ForFile(string filePath, string text)
    {
        var map = new CssSourceMap();
        map.Add(0, text, new SourceSpan(filePath, 0, 0, 0, text.Length), exact: true);
        return map;
    }

    public void Add(int cssStart, string text, SourceSpan? origin, bool exact)
    {
        if (origin is { } span && span.FilePath != null)
        {
            _segments.Add(new Segment(cssStart, text, span, exact && span.Length == text.Length));
        }
    }

    public SourceSpan? Map(int cssStart, int length)
    {
        if (_segments.Count == 0)
        {
            return null;
        }

        var segment = _segments[0];
        foreach (var candidate in _segments)
        {
            if (candidate.CssStart > cssStart)
            {
                break;
            }

            segment = candidate;
        }

        var origin = segment.Origin;
        var offset = cssStart - segment.CssStart;
        if (!segment.Exact || offset < 0 || offset > segment.Text.Length)
        {
            return new SourceSpan(origin.FilePath, origin.AbsoluteIndex, origin.LineIndex, origin.CharacterIndex,
                System.Math.Min(System.Math.Max(length, 1), System.Math.Max(origin.Length, 1)));
        }

        var line = origin.LineIndex;
        var character = origin.CharacterIndex;
        for (var i = 0; i < offset; i++)
        {
            if (segment.Text[i] == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }
        }

        // Diagnostics are single-line spans: stop at the end of the line.
        var end = offset;
        while (end < segment.Text.Length && end - offset < length && segment.Text[end] != '\n' && segment.Text[end] != '\r')
        {
            end++;
        }

        return new SourceSpan(origin.FilePath, origin.AbsoluteIndex + offset, line, character, System.Math.Max(end - offset, 1));
    }
}
