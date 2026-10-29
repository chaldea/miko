// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>
/// Grid templates and placement. <c>GridTrackSize</c>, <c>GridLine</c> and <c>GridTemplateAreas</c>
/// throw on input CSS would reject, so everything they check is checked here first.
/// </summary>
internal sealed partial class CssPropertyTranslator
{
    private const string TrackType = CommonNs + "GridTrackSize";
    private const string LineType = CommonNs + "GridLine";

    private enum TrackKind { Breadth, Fraction, LineNames, Repeat, AutoRepeat }

    private static void AddGridProperties(Dictionary<string, Property> p)
    {
        AddShorthand(p, "grid-template-columns", new[] { "GridTemplateColumns" }, (t, v, c) => t.Assign("GridTemplateColumns", t.TrackList(c)));
        AddShorthand(p, "grid-template-rows", new[] { "GridTemplateRows" }, (t, v, c) => t.Assign("GridTemplateRows", t.TrackList(c)));
        AddShorthand(p, "grid-auto-columns", new[] { "GridAutoColumns" }, (t, v, c) => t.Assign("GridAutoColumns", t.AutoTrack(c)));
        AddShorthand(p, "grid-auto-rows", new[] { "GridAutoRows" }, (t, v, c) => t.Assign("GridAutoRows", t.AutoTrack(c)));
        AddShorthand(p, "grid-template-areas", new[] { "GridTemplateAreas" }, (t, v, c) => t.Assign("GridTemplateAreas", t.TemplateAreas(c)));
        foreach (var (css, cs) in new[] { ("grid-row-start", "GridRowStart"), ("grid-row-end", "GridRowEnd"), ("grid-column-start", "GridColumnStart"), ("grid-column-end", "GridColumnEnd") })
            AddShorthand(p, css, new[] { cs }, (t, v, c) => t.Assign(cs, t.GridLine(c)));
        AddShorthand(p, "grid-row", new[] { "GridRowStart", "GridRowEnd" }, (t, v, c) => t.GridPlacement(c, new[] { "GridRowStart", "GridRowEnd" }));
        AddShorthand(p, "grid-column", new[] { "GridColumnStart", "GridColumnEnd" }, (t, v, c) => t.GridPlacement(c, new[] { "GridColumnStart", "GridColumnEnd" }));
        AddShorthand(p, "grid-area", new[] { "GridRowStart", "GridColumnStart", "GridRowEnd", "GridColumnEnd" },
            (t, v, c) => t.GridPlacement(c, new[] { "GridRowStart", "GridColumnStart", "GridRowEnd", "GridColumnEnd" }));
        AddShorthand(p, "grid-template", new[] { "GridTemplateRows", "GridTemplateColumns", "GridTemplateAreas" }, (t, v, c) => t.GridTemplate(c));
        AddShorthand(p, "grid-auto-flow", Array.Empty<string>(), (t, v, c) =>
        {
            if (!(c.Count == 1 && c[0].IsIdent("row"))) t.Fail("only 'row' is supported (Miko places grid items row by row, without dense packing).");
        });
    }

    private string? TrackList(List<CssValue> components)
    {
        if (components.Count == 1 && components[0].IsIdent("none")) return CssType + ".Initial";
        var tracks = Tracks(components, inRepeat: false, out _);
        if (tracks == null) return null;
        if (!tracks.Exists(t => t.Kind != TrackKind.LineNames)) return Fail("a track list needs at least one track.");
        if (tracks.Count(t => t.Kind == TrackKind.AutoRepeat) > 1) return Fail("a track list may contain only one repeat(auto-fill | auto-fit, …).");
        return $"new global::System.Collections.Generic.List<{TrackType}> {{ {string.Join(", ", tracks.Select(t => t.Expression))} }}";
    }

    private string? AutoTrack(List<CssValue> components)
    {
        if (components.Count > 1) return Fail("only one implicit track size is supported.");
        var tracks = Tracks(components, inRepeat: false, out _);
        if (tracks == null) return null;
        if (tracks.Count != 1 || tracks[0].Kind is TrackKind.LineNames or TrackKind.Repeat or TrackKind.AutoRepeat)
            return Fail("expected a single track size.");
        return tracks[0].Expression;
    }

    private sealed class Track(TrackKind kind, string expression, bool isFixed)
    {
        public TrackKind Kind { get; } = kind;
        public string Expression { get; } = expression;

        /// <summary>A CSS &lt;fixed-size&gt;: what repeat(auto-fill/auto-fit) needs.</summary>
        public bool IsFixed { get; } = isFixed;
    }

    private List<Track>? Tracks(List<CssValue> components, bool inRepeat, out bool any)
    {
        any = false;
        var tracks = new List<Track>();
        foreach (var c in components)
        {
            var track = c.Kind == CssTokenKind.LeftBracket ? LineNames(c) : TrackSize(c, inRepeat);
            if (track == null) return null;
            any |= track.Kind != TrackKind.LineNames;
            tracks.Add(track);
        }

        return tracks;
    }

    private Track? TrackSize(CssValue c, bool inRepeat, bool breadthOnly = false)
    {
        if (c.IsFunction)
        {
            switch (c.FunctionName)
            {
                case "minmax" when !breadthOnly:
                {
                    var args = CssParser.SplitByComma(c.Children!);
                    if (args.Count != 2 || args[0].Count != 1 || args[1].Count != 1) return TrackFail("minmax() takes two track sizes.");
                    var min = TrackSize(args[0][0], inRepeat, breadthOnly: true);
                    var max = TrackSize(args[1][0], inRepeat, breadthOnly: true);
                    if (min == null || max == null) return null;
                    if (min.Kind == TrackKind.Fraction) return TrackFail("the minimum of minmax() cannot be fr.");
                    return new Track(TrackKind.Breadth, $"{TrackType}.MinMax({min.Expression}, {max.Expression})", min.IsFixed || max.IsFixed);
                }

                case "fit-content" when !breadthOnly:
                {
                    var args = CssParser.Components(c.Children!);
                    if (args.Count != 1) return TrackFail("fit-content() takes one length.");
                    var limit = PlainLength(args[0], LengthOptions.Percent);
                    return limit == null ? null : new Track(TrackKind.Breadth, $"{TrackType}.FitContent({limit})", false);
                }

                case "repeat" when !breadthOnly && !inRepeat:
                    return Repeat(c);
                case "repeat":
                    return TrackFail("repeat() cannot be nested.");
                case "calc":
                {
                    var length = PlainLength(c, LengthOptions.Percent);
                    return length == null ? null : new Track(TrackKind.Breadth, $"{TrackType}.FromLength({length})", true);
                }

                default:
                    return TrackFail($"'{c.FunctionName}()' is not supported in a track list.");
            }
        }

        if (c.Kind == CssTokenKind.Dimension && c.Token.Value.Equals("fr", StringComparison.OrdinalIgnoreCase))
        {
            return c.Token.Number < 0 ? TrackFail("fr cannot be negative.") : new Track(TrackKind.Fraction, $"{TrackType}.Fr({Float(c.Token.Number)})", false);
        }

        if (c.Kind == CssTokenKind.Ident)
        {
            switch (c.Token.Value.ToLowerInvariant())
            {
                case "auto": return new Track(TrackKind.Breadth, TrackType + ".Auto", false);
                case "min-content": return new Track(TrackKind.Breadth, TrackType + ".MinContent", false);
                case "max-content": return new Track(TrackKind.Breadth, TrackType + ".MaxContent", false);
            }
        }

        if (c.Kind is CssTokenKind.Dimension or CssTokenKind.Percentage or CssTokenKind.Number)
        {
            var length = PlainLength(c, LengthOptions.Percent);
            return length == null ? null : new Track(TrackKind.Breadth, $"{TrackType}.FromLength({length})", true);
        }

        return TrackFail($"'{Snippet(c)}' is not a track size.");
    }

    private Track? TrackFail(string message)
    {
        Fail(message);
        return null;
    }

    private Track? Repeat(CssValue c)
    {
        var comma = c.Children!.FindIndex(v => v.IsComma);
        if (comma < 0) return TrackFail("repeat() takes a count and a track list.");
        var count = CssParser.Components(c.Children!.GetRange(0, comma));
        var inner = CssParser.Components(c.Children!.GetRange(comma + 1, c.Children!.Count - comma - 1));
        if (count.Count != 1) return TrackFail("invalid repeat() count.");

        var tracks = Tracks(inner, inRepeat: true, out var any);
        if (tracks == null) return null;
        if (!any) return TrackFail("repeat() needs at least one track.");
        var list = string.Join(", ", tracks.Select(t => t.Expression));

        if (count[0].IsIdent("auto-fill") || count[0].IsIdent("auto-fit"))
        {
            if (tracks.Exists(t => t.Kind != TrackKind.LineNames && !t.IsFixed))
                return TrackFail("repeat(auto-fill/auto-fit) tracks need a fixed size (a length, or minmax() with a length end).");
            var mode = count[0].IsIdent("auto-fill") ? "AutoFill" : "AutoFit";
            return new Track(TrackKind.AutoRepeat, $"{TrackType}.Repeat({CommonNs}GridAutoRepeat.{mode}, {list})", false);
        }

        if (count[0].Kind != CssTokenKind.Number || !count[0].Token.IsInteger || count[0].Token.Number < 1)
            return TrackFail("the repeat() count must be a positive integer, auto-fill or auto-fit.");
        return new Track(TrackKind.Repeat, $"{TrackType}.Repeat({Int(count[0].Token.Number)}, {list})", false);
    }

    private Track? LineNames(CssValue bracket)
    {
        var names = CssParser.Components(bracket.Children!);
        if (names.Count == 0) return TrackFail("'[]' needs a line name.");
        foreach (var name in names)
        {
            if (name.Kind != CssTokenKind.Ident || name.Children != null) return TrackFail($"'{Snippet(name)}' is not a line name.");
            if (name.Token.Value.ToLowerInvariant() is "span" or "auto") return TrackFail($"'{name.Token.Value}' cannot name a grid line.");
        }

        return new Track(TrackKind.LineNames, $"{TrackType}.LineNames({string.Join(", ", names.Select(n => Str(n.Token.Value)))})", false);
    }

    /// <summary><c>auto | &lt;integer&gt; | name | name &lt;integer&gt; | span &lt;integer&gt; | span name</c>.</summary>
    private string? GridLine(List<CssValue> components)
    {
        if (components.Count == 1 && components[0].IsIdent("auto")) return LineType + ".Auto";
        var span = false;
        int? number = null;
        string? name = null;
        foreach (var c in components)
        {
            if (c.IsIdent("span") && !span) span = true;
            else if (c.Kind == CssTokenKind.Number && c.Children == null && c.Token.IsInteger && number == null) number = (int)c.Token.Number;
            else if (c.Kind == CssTokenKind.Ident && c.Children == null && name == null && !c.IsIdent("auto")) name = c.Token.Value;
            else return Fail($"'{Snippet(c)}' is not a grid line.");
        }

        if (number == 0) return Fail("grid line 0 does not exist.");
        if (span)
        {
            if (number < 0) return Fail("a span must be positive.");
            return name != null
                ? $"{LineType}.Span({Str(name)}, {number ?? 1})"
                : $"{LineType}.Span({number ?? 1})";
        }

        if (name != null) return number is { } n ? $"{LineType}.Named({Str(name)}, {n})" : $"{LineType}.Named({Str(name)})";
        return number is { } line ? $"{LineType}.Line({line})" : Fail("expected a grid line.");
    }

    /// <summary>
    /// grid-row / grid-column (2 targets) and grid-area (4): values separated by <c>/</c>. An
    /// omitted value is the first one when that is a name, else auto (CSS Grid §8.4).
    /// </summary>
    private void GridPlacement(List<CssValue> components, string[] targets)
    {
        var parts = new List<List<CssValue>> { new() };
        foreach (var c in components)
        {
            if (c.IsDelim('/')) parts.Add(new List<CssValue>());
            else parts[parts.Count - 1].Add(c);
        }

        if (parts.Count > targets.Length || parts.Exists(part => part.Count == 0))
        {
            Fail("invalid grid placement.");
            return;
        }

        var lines = parts.ConvertAll(GridLine);
        if (Failed) return;
        for (var i = 0; i < targets.Length; i++)
        {
            string? line;
            if (i < lines.Count) line = lines[i];
            else
            {
                // grid-area: column-start ← row-start, row-end ← row-start, column-end ← column-start.
                var source = targets.Length == 4 ? (i == 3 ? 1 : 0) : 0;
                var copy = source < parts.Count && parts[source].Count == 1 && parts[source][0].Kind == CssTokenKind.Ident && !parts[source][0].IsIdent("auto");
                line = copy ? lines[source] : LineType + ".Auto";
            }

            Assign(targets[i], line);
        }
    }

    private string? TemplateAreas(List<CssValue> components)
    {
        if (components.Count == 1 && components[0].IsIdent("none")) return CssType + ".Initial";
        if (components.Count == 0 || !components.TrueForAll(c => c.Kind == CssTokenKind.String && c.Children == null))
            return Fail("expected one quoted string per row.");

        var rows = components.ConvertAll(c => c.Token.Value);
        if (ValidateAreas(rows) is { } error) return Fail(error);
        return $"new {CommonNs}GridTemplateAreas({string.Join(", ", rows.Select(Str))})";
    }

    /// <summary>The checks of the <c>GridTemplateAreas</c> constructor, as a message instead of an exception.</summary>
    private static string? ValidateAreas(List<string> rows)
    {
        var cells = rows.ConvertAll(r => r.Split(new[] { ' ', '\t', '\n', '\r', '\f' }, StringSplitOptions.RemoveEmptyEntries));
        if (cells.Exists(r => r.Length == 0)) return "a grid-template-areas row is empty.";
        if (cells.Exists(r => r.Length != cells[0].Length)) return "every grid-template-areas row must have the same number of columns.";

        var bounds = new Dictionary<string, (int R0, int C0, int R1, int C1, int Count)>(StringComparer.Ordinal);
        for (var r = 0; r < cells.Count; r++)
        {
            for (var c = 0; c < cells[r].Length; c++)
            {
                var name = cells[r][c];
                if (name.All(ch => ch == '.')) continue;
                if (!name.All(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' || ch > 0x7F)) return $"\"{name}\" is not a valid area name.";
                bounds[name] = bounds.TryGetValue(name, out var b)
                    ? (Math.Min(b.R0, r), Math.Min(b.C0, c), Math.Max(b.R1, r + 1), Math.Max(b.C1, c + 1), b.Count + 1)
                    : (r, c, r + 1, c + 1, 1);
            }
        }

        foreach (var (name, b) in bounds)
        {
            if ((b.R1 - b.R0) * (b.C1 - b.C0) != b.Count) return $"the cells named \"{name}\" do not form a rectangle.";
        }

        return null;
    }

    /// <summary><c>grid-template: none | rows / columns</c> (the form with area strings is not supported).</summary>
    private void GridTemplate(List<CssValue> components)
    {
        if (components.Count == 1 && components[0].IsIdent("none"))
        {
            Assign("GridTemplateRows", CssType + ".Initial");
            Assign("GridTemplateColumns", CssType + ".Initial");
            Assign("GridTemplateAreas", CssType + ".Initial");
            return;
        }

        if (components.Exists(c => c.Kind == CssTokenKind.String))
        {
            Fail("the grid-template form with area strings is not supported; use grid-template-areas, -rows and -columns.");
            return;
        }

        var slash = components.FindIndex(c => c.IsDelim('/'));
        if (slash < 0)
        {
            Fail("expected 'rows / columns'.");
            return;
        }

        Assign("GridTemplateRows", TrackList(components.GetRange(0, slash)));
        Assign("GridTemplateColumns", TrackList(components.GetRange(slash + 1, components.Count - slash - 1)));
        Assign("GridTemplateAreas", CssType + ".Initial");
    }
}
