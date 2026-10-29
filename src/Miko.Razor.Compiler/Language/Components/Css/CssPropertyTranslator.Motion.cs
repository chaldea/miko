// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>
/// transition and animation. Their longhands are comma-separated lists combined by position, so a
/// block collects them and builds <c>Style.Transitions</c> / <c>Style.Animations</c> once at the end.
/// </summary>
internal sealed partial class CssPropertyTranslator
{
    private sealed class Timing(string function, string? bezier)
    {
        public string Function { get; } = function;
        public string? Bezier { get; } = bezier;
    }

    private sealed class AnimationItem
    {
        public string? Name;
        public float Duration;
        public float Delay;
        public Timing Timing = new("Ease", null);
        public string Iterations = "1";
        public bool Infinite;
        public string Direction = "Normal";
        public string FillMode = "None";
        public string PlayState = "Running";
    }

    // Per block. Null: not declared.
    private List<List<string>>? _transitionProperties;
    private List<float>? _transitionDurations;
    private List<float>? _transitionDelays;
    private List<Timing>? _transitionTimings;
    private List<AnimationItem>? _animations;
    private List<Action<AnimationItem, int>>? _animationLonghands;
    private int _animationNameCount;

    private static void AddMotionProperties(Dictionary<string, Property> p)
    {
        var transitions = new[] { "Transitions" };
        AddShorthand(p, "transition", transitions, (t, v, c) => t.TransitionShorthand(v));
        AddShorthand(p, "transition-property", transitions, (t, v, c) => t.LonghandList(v, t.TransitionProperty, r => t._transitionProperties = r));
        AddShorthand(p, "transition-duration", transitions, (t, v, c) => t.LonghandList(v, x => t.Time(x, negative: false), r => t._transitionDurations = r));
        AddShorthand(p, "transition-delay", transitions, (t, v, c) => t.LonghandList(v, x => t.Time(x, negative: true), r => t._transitionDelays = r));
        AddShorthand(p, "transition-timing-function", transitions, (t, v, c) => t.LonghandList(v, t.TimingFunction, r => t._transitionTimings = r));

        var animations = new[] { "Animations" };
        AddShorthand(p, "animation", animations, (t, v, c) => t.AnimationShorthand(v));
        AddShorthand(p, "animation-name", animations, (t, v, c) => t.AnimationLonghand(v, x => x.IsIdent("none") ? "" : x.Kind is CssTokenKind.Ident or CssTokenKind.String && x.Children == null ? t.KeyframesName(x) : null, (a, s) => a.Name = (string)s, isName: true));
        AddShorthand(p, "animation-duration", animations, (t, v, c) => t.AnimationLonghand(v, x => t.Time(x, false), (a, s) => a.Duration = (float)s));
        AddShorthand(p, "animation-delay", animations, (t, v, c) => t.AnimationLonghand(v, x => t.Time(x, true), (a, s) => a.Delay = (float)s));
        AddShorthand(p, "animation-timing-function", animations, (t, v, c) => t.AnimationLonghand(v, t.TimingFunction, (a, s) => a.Timing = (Timing)s));
        AddShorthand(p, "animation-iteration-count", animations, (t, v, c) => t.AnimationLonghand(v, t.Iterations, (a, s) => { a.Infinite = (string)s == "infinite"; a.Iterations = a.Infinite ? "1" : (string)s; }));
        AddShorthand(p, "animation-direction", animations, (t, v, c) => t.AnimationLonghand(v, x => t.Member(x, AnimationDirectionMap), (a, s) => a.Direction = (string)s));
        AddShorthand(p, "animation-fill-mode", animations, (t, v, c) => t.AnimationLonghand(v, x => t.Member(x, FillModeMap), (a, s) => a.FillMode = (string)s));
        AddShorthand(p, "animation-play-state", animations, (t, v, c) => t.AnimationLonghand(v, x => t.Member(x, PlayStateMap), (a, s) => a.PlayState = (string)s));
    }

    /// <summary>A comma-separated longhand list; stored when the declaration commits.</summary>
    private void LonghandList<T>(List<CssValue> values, Func<CssValue, T?> parse, Action<List<T>> store) where T : class
        => ListCore(values, v => parse(v) is { } r ? (true, r) : (false, default!), store);

    private void LonghandList(List<CssValue> values, Func<CssValue, float?> parse, Action<List<float>> store)
        => ListCore(values, v => parse(v) is { } r ? (true, r) : (false, 0f), store);

    private void ListCore<T>(List<CssValue> values, Func<CssValue, (bool Ok, T Value)> parse, Action<List<T>> store)
    {
        var items = new List<T>();
        foreach (var part in CssParser.SplitByComma(values))
        {
            if (part.Count != 1)
            {
                Fail("expected a comma-separated list of single values.");
                return;
            }

            var (ok, value) = parse(part[0]);
            if (!ok)
            {
                Fail($"'{Snippet(part[0])}' is not supported.");
                return;
            }

            items.Add(value);
        }

        _onCommit.Add(() => store(items));
    }

    private void ResetListState(string property)
    {
        if (property.StartsWith("transition", StringComparison.Ordinal))
        {
            _onCommit.Add(() => _transitionProperties = null);
            _onCommit.Add(() => _transitionDurations = _transitionDelays = null);
            _onCommit.Add(() => _transitionTimings = null);
        }
        else if (property.StartsWith("animation", StringComparison.Ordinal))
        {
            _onCommit.Add(() => { _animations = null; _animationLonghands = null; _animationNameCount = 0; });
        }
    }

    private float? Time(CssValue value, bool negative)
    {
        var token = value.Token;
        if (value.Children != null) return NullFail<float>("calc() is not supported for times.");
        double seconds;
        if (token.Kind == CssTokenKind.Dimension && token.Value.Equals("s", StringComparison.OrdinalIgnoreCase)) seconds = token.Number;
        else if (token.Kind == CssTokenKind.Dimension && token.Value.Equals("ms", StringComparison.OrdinalIgnoreCase)) seconds = token.Number / 1000;
        else if (token.Kind == CssTokenKind.Number && token.Number == 0) seconds = 0;
        else return NullFail<float>($"'{Snippet(value)}' is not a time (use s or ms).");
        if (seconds < 0 && !negative) return NullFail<float>("a duration cannot be negative.");
        return (float)seconds;
    }

    private T? NullFail<T>(string message) where T : struct
    {
        Fail(message);
        return null;
    }

    private Timing? TimingFunction(CssValue value)
    {
        if (value.IsFunction && value.FunctionName == "cubic-bezier")
        {
            var args = CssParser.SplitByComma(value.Children!);
            if (args.Count != 4 || !args.TrueForAll(a => a.Count == 1 && a[0].Kind == CssTokenKind.Number && a[0].Children == null)) return null;
            var n = args.ConvertAll(a => a[0].Token.Number);
            if (n[0] is < 0 or > 1 || n[2] is < 0 or > 1) return null;
            return new Timing("CubicBezier", $"new {AnimationNs}CubicBezierParams({Float(n[0])}, {Float(n[1])}, {Float(n[2])}, {Float(n[3])})");
        }

        if (value.IsFunction && value.FunctionName == "steps")
        {
            var args = CssParser.SplitByComma(value.Children!);
            var jumpStart = args.Count == 2 && args[1].Count == 1 && (args[1][0].IsIdent("start") || args[1][0].IsIdent("jump-start"));
            Note("steps() is not supported and is treated as step-start / step-end.");
            return new Timing(jumpStart ? "StepStart" : "StepEnd", null);
        }

        if (value.Kind != CssTokenKind.Ident || value.Children != null) return null;
        var keyword = value.Token.Value.ToLowerInvariant();
        if (!TimingFunctionMap.ContainsKey(keyword)) return null;
        return new Timing(EnumKeyword(keyword, "", TimingFunctionMap)!.Substring(1), null);
    }

    private string? Member(CssValue value, Dictionary<string, string> map)
    {
        if (value.Kind != CssTokenKind.Ident || value.Children != null) return null;
        var keyword = value.Token.Value.ToLowerInvariant();
        return map.ContainsKey(keyword) ? EnumKeyword(keyword, "", map)!.Substring(1) : null;
    }

    private string? Iterations(CssValue value)
    {
        if (value.IsIdent("infinite")) return "infinite";
        if (value.Kind != CssTokenKind.Number || value.Children != null || value.Token.Number < 0) return null;
        if (!value.Token.IsInteger) Note("a fractional iteration count is rounded to a whole number.");
        return Int(value.Token.Number);
    }

    /// <summary>A transition-property entry: the C# properties it animates (empty for none).</summary>
    private List<string>? TransitionProperty(CssValue value)
    {
        if (value.Kind != CssTokenKind.Ident || value.Children != null) return null;
        var name = value.Token.Value.ToLowerInvariant();
        if (name == "none") return new List<string>();
        if (name == "all") return new List<string> { "all" };
        if (s_transitionProperties.TryGetValue(name, out var targets)) return targets.ToList();
        Note($"Miko does not animate '{name}'; that transition has no effect.");
        return new List<string>();
    }

    /// <summary>The properties MikoEngine.TriggerPropertyTransitions animates, by CSS name.</summary>
    private static readonly Dictionary<string, string[]> s_transitionProperties = new(StringComparer.Ordinal)
    {
        ["width"] = new[] { "Width" }, ["height"] = new[] { "Height" }, ["max-width"] = new[] { "MaxWidth" },
        ["max-height"] = new[] { "MaxHeight" }, ["opacity"] = new[] { "Opacity" }, ["font-size"] = new[] { "FontSize" },
        ["border-width"] = new[] { "BorderWidth" }, ["padding"] = new[] { "Padding" }, ["margin"] = new[] { "Margin" },
        ["padding-top"] = new[] { "PaddingTop" }, ["padding-right"] = new[] { "PaddingRight" },
        ["padding-bottom"] = new[] { "PaddingBottom" }, ["padding-left"] = new[] { "PaddingLeft" },
        ["margin-top"] = new[] { "MarginTop" }, ["margin-right"] = new[] { "MarginRight" },
        ["margin-bottom"] = new[] { "MarginBottom" }, ["margin-left"] = new[] { "MarginLeft" },
        ["top"] = new[] { "Top" }, ["right"] = new[] { "Right" }, ["bottom"] = new[] { "Bottom" }, ["left"] = new[] { "Left" },
        ["inset"] = new[] { "Top", "Right", "Bottom", "Left" },
        ["border-radius"] = new[] { "BorderTopLeftRadius", "BorderTopRightRadius", "BorderBottomRightRadius", "BorderBottomLeftRadius" },
        ["border-top-left-radius"] = new[] { "BorderTopLeftRadius" }, ["border-top-right-radius"] = new[] { "BorderTopRightRadius" },
        ["border-bottom-right-radius"] = new[] { "BorderBottomRightRadius" }, ["border-bottom-left-radius"] = new[] { "BorderBottomLeftRadius" },
        ["background-color"] = new[] { "BackgroundColor" }, ["background"] = new[] { "BackgroundColor" },
        ["color"] = new[] { "Color" }, ["border-color"] = new[] { "BorderColor" }, ["transform"] = new[] { "Transform" },
    };

    private void AnimationLonghand(List<CssValue> values, Func<CssValue, object?> parse, Action<AnimationItem, object> apply, bool isName = false)
    {
        var parsed = new List<object>();
        foreach (var part in CssParser.SplitByComma(values))
        {
            if (part.Count != 1 || parse(part[0]) is not { } value)
            {
                Fail(part.Count == 1 ? $"'{Snippet(part[0])}' is not supported." : "expected a comma-separated list of single values.");
                return;
            }

            parsed.Add(value);
        }

        _onCommit.Add(() =>
        {
            (_animationLonghands ??= new()).Add((item, i) => apply(item, parsed[i % parsed.Count]));
            if (isName) _animationNameCount = parsed.Count;
        });
    }

    private string? KeyframesName(CssValue value)
    {
        var name = value.Token.Value;
        if (!_context.Keyframes.ContainsKey(name)) return Fail($"no @keyframes '{name}' in this stylesheet.");
        return name;
    }

    private void TransitionShorthand(List<CssValue> values)
    {
        var properties = new List<List<string>>();
        var durations = new List<float>();
        var delays = new List<float>();
        var timings = new List<Timing>();
        foreach (var part in CssParser.SplitByComma(values))
        {
            List<string>? property = null;
            float? duration = null, delay = null;
            Timing? timing = null;
            foreach (var c in CssParser.Components(part))
            {
                var isTime = c.Children == null && (c.Kind == CssTokenKind.Dimension || c.Kind == CssTokenKind.Number && c.Token.Number == 0);
                if (isTime && duration == null) duration = Time(c, negative: false);
                else if (isTime && delay == null) delay = Time(c, negative: true);
                else if (timing == null && TimingFunction(c) is { } t) timing = t;
                else if (property == null && c.Kind == CssTokenKind.Ident && c.Children == null) property = TransitionProperty(c);
                else
                {
                    Fail($"'{Snippet(c)}' is not supported in transition.");
                    return;
                }

                if (Failed) return;
            }

            properties.Add(property ?? new List<string> { "all" });
            durations.Add(duration ?? 0);
            delays.Add(delay ?? 0);
            timings.Add(timing ?? new Timing("Ease", null));
        }

        _onCommit.Add(() =>
        {
            _transitionProperties = properties;
            _transitionDurations = durations;
            _transitionDelays = delays;
            _transitionTimings = timings;
        });
    }

    private void AnimationShorthand(List<CssValue> values)
    {
        var items = new List<AnimationItem>();
        foreach (var part in CssParser.SplitByComma(values))
        {
            var item = new AnimationItem();
            bool duration = false, delay = false, timing = false, count = false, direction = false, fill = false, play = false, name = false;
            foreach (var c in CssParser.Components(part))
            {
                var keyword = c.Kind == CssTokenKind.Ident && c.Children == null ? c.Token.Value.ToLowerInvariant() : null;
                var isTime = c.Children == null && c.Kind == CssTokenKind.Dimension;
                string? member;
                Timing? t;
                if (isTime && !duration) { item.Duration = Time(c, false) ?? 0; duration = true; }
                else if (isTime && !delay) { item.Delay = Time(c, true) ?? 0; delay = true; }
                else if (!timing && (t = TimingFunction(c)) != null) { item.Timing = t; timing = true; }
                else if (!count && (c.IsIdent("infinite") || c.Kind == CssTokenKind.Number && c.Children == null))
                {
                    var iterations = Iterations(c);
                    if (iterations == null) { Fail($"'{Snippet(c)}' is not an iteration count."); return; }
                    item.Infinite = iterations == "infinite";
                    item.Iterations = item.Infinite ? "1" : iterations;
                    count = true;
                }
                else if (!direction && (member = Member(c, AnimationDirectionMap)) != null) { item.Direction = member; direction = true; }
                else if (!fill && keyword != "none" && (member = Member(c, FillModeMap)) != null) { item.FillMode = member; fill = true; }
                else if (!play && (member = Member(c, PlayStateMap)) != null) { item.PlayState = member; play = true; }
                else if (!name && keyword == "none") { item.Name = ""; name = true; }
                else if (!name && c.Children == null && c.Kind is CssTokenKind.Ident or CssTokenKind.String) { item.Name = KeyframesName(c); name = true; }
                else
                {
                    Fail($"'{Snippet(c)}' is not supported in animation.");
                    return;
                }

                if (Failed) return;
            }

            items.Add(item);
        }

        _onCommit.Add(() =>
        {
            _animations = items;
            _animationLonghands = null;
            _animationNameCount = 0;
        });
    }

    /// <summary>Builds Transitions / Animations from what the block declared.</summary>
    private void Finish()
    {
        if (_transitionProperties != null || _transitionDurations != null || _transitionDelays != null || _transitionTimings != null)
            _block.Set("Transitions", ComposeTransitions());
        if (_animations != null || _animationLonghands != null)
            _block.Set("Animations", ComposeAnimations());
    }

    private string ComposeTransitions()
    {
        var properties = _transitionProperties ?? new List<List<string>> { new() { "all" } };
        var entries = new List<string>();
        for (var i = 0; i < properties.Count; i++)
        {
            var duration = _transitionDurations is { Count: > 0 } d ? d[i % d.Count] : 0;
            var delay = _transitionDelays is { Count: > 0 } l ? l[i % l.Count] : 0;
            var timing = _transitionTimings is { Count: > 0 } t ? t[i % t.Count] : new Timing("Ease", null);
            foreach (var property in properties[i])
            {
                var entry = $"new {AnimationNs}Transition({Str(property)}, {Float(duration)}, {AnimationNs}TimingFunction.{timing.Function}, {Float(delay)})";
                if (timing.Bezier != null) entry += $" {{ CubicBezier = {timing.Bezier} }}";
                entries.Add(entry);
            }
        }

        return $"new global::System.Collections.Generic.List<{AnimationNs}Transition> {{ {string.Join(", ", entries)} }}";
    }

    private string ComposeAnimations()
    {
        var count = _animations?.Count ?? Math.Max(_animationNameCount, 1);
        var entries = new List<string>();
        for (var i = 0; i < count; i++)
        {
            var item = _animations != null ? Clone(_animations[i]) : new AnimationItem();
            if (_animationLonghands != null)
            {
                foreach (var apply in _animationLonghands) apply(item, i);
            }

            if (string.IsNullOrEmpty(item.Name)) continue;
            entries.Add($"new {AnimationNs}KeyframeAnimation {{ Name = {Str(item.Name!)}, Duration = {Float(item.Duration)}, " +
                $"TimingFunction = {AnimationNs}TimingFunction.{item.Timing.Function}, " +
                (item.Timing.Bezier != null ? $"CubicBezier = {item.Timing.Bezier}, " : "") +
                $"Delay = {Float(item.Delay)}, IterationCount = {item.Iterations}, Infinite = {(item.Infinite ? "true" : "false")}, " +
                $"Direction = {AnimationNs}AnimationDirection.{item.Direction}, FillMode = {AnimationNs}AnimationFillMode.{item.FillMode}, " +
                $"PlayState = {AnimationNs}AnimationPlayState.{item.PlayState}, Keyframes = {KeyframesExpression(item.Name!)} }}");
        }

        return $"new global::System.Collections.Generic.List<{AnimationNs}KeyframeAnimation> {{ {string.Join(", ", entries)} }}";
    }

    private static AnimationItem Clone(AnimationItem item) => new()
    {
        Name = item.Name, Duration = item.Duration, Delay = item.Delay, Timing = item.Timing, Iterations = item.Iterations,
        Infinite = item.Infinite, Direction = item.Direction, FillMode = item.FillMode, PlayState = item.PlayState,
    };

    private string KeyframesExpression(string name) => TranslateKeyframes(_context, name);

    /// <summary>The frames of <c>@keyframes name</c>, translated once per stylesheet.</summary>
    internal static string TranslateKeyframes(CssTranslationContext context, string name)
    {
        if (context.KeyframeCache.TryGetValue(name, out var cached)) return cached;
        var frames = new List<(float Offset, string Style)>();
        foreach (var frame in context.Keyframes[name].Frames)
        {
            var style = TranslateBlock(context, frame.Declarations, inKeyframe: true).ToInitializer("global::Miko.Styling.Style", "                ");
            foreach (var offset in frame.Offsets) frames.Add((offset, style));
        }

        var sorted = frames.OrderBy(f => f.Offset).Select(f => $"new {AnimationNs}Keyframe({Float(f.Offset)}, {f.Style})");
        var expression = $"new global::System.Collections.Generic.List<{AnimationNs}Keyframe> {{ {string.Join(", ", sorted)} }}";
        context.KeyframeCache[name] = expression;
        return expression;
    }
}
