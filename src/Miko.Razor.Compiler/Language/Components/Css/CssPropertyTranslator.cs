// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>What one stylesheet (or <c>style</c> attribute) translation shares.</summary>
internal sealed class CssTranslationContext
{
    public CssTranslationContext(string text, List<CssDiagnostic> diagnostics, bool allowDynamic)
    {
        Text = text;
        Diagnostics = diagnostics;
        AllowDynamic = allowDynamic;
    }

    public string Text { get; }

    public List<CssDiagnostic> Diagnostics { get; }

    /// <summary>Whether placeholders are C# expressions to translate (a <c>style</c> attribute).</summary>
    public bool AllowDynamic { get; }

    /// <summary>The stylesheet's <c>@keyframes</c>, for <c>animation-name</c>. The last one of a name wins.</summary>
    public Dictionary<string, CssKeyframesRuleNode> Keyframes { get; } = new(StringComparer.Ordinal);

    internal Dictionary<string, string> KeyframeCache { get; } = new(StringComparer.Ordinal);
}

/// <summary>
/// Translates CSS declarations into assignments to <c>Miko.Styling.Style</c> properties
/// (ISSUE-161). Shorthands expand to the longhands they set, a declaration Miko cannot represent
/// is reported and skipped as a whole (as a browser drops an invalid declaration), and one that is
/// applied approximately is reported but kept.
/// </summary>
internal sealed partial class CssPropertyTranslator
{
    private const string LengthType = "global::Miko.Common.Length";
    private const string ColorType = "global::Miko.Common.Color";
    private const string CommonNs = "global::Miko.Common.";
    private const string AnimationNs = "global::Miko.Animation.";
    private const string CssType = "global::Miko.Styling.Css";
    private const string KeywordType = "global::Miko.Styling.StyleKeyword";
    private const string VarValueType = "global::Miko.Styling.VarValue";
    private const string BuilderType = "global::Miko.Components.RenderTreeBuilder";

    private readonly CssTranslationContext _context;
    private readonly CssStyleBlock _block;
    private readonly bool _inKeyframe;
    private readonly List<(string Property, string Expression)> _pending = new();
    private readonly List<Action> _onCommit = new();
    private readonly List<string> _notes = new();
    private CssDeclarationNode _declaration = null!;
    private string? _error;
    private bool _dynamic;

    private CssPropertyTranslator(CssTranslationContext context, CssStyleBlock block, bool inKeyframe)
    {
        _context = context;
        _block = block;
        _inKeyframe = inKeyframe;
    }

    public static CssStyleBlock TranslateBlock(CssTranslationContext context, IEnumerable<CssDeclarationNode> declarations, bool inKeyframe = false)
    {
        var block = new CssStyleBlock();
        var translator = new CssPropertyTranslator(context, block, inKeyframe);
        foreach (var declaration in declarations)
        {
            translator.Translate(declaration);
        }

        translator.Finish();
        return block;
    }

    private void Translate(CssDeclarationNode declaration)
    {
        _declaration = declaration;
        _pending.Clear();
        _onCommit.Clear();
        _notes.Clear();
        _error = null;
        _dynamic = false;

        var name = declaration.Name;
        // In a <style> element a C# expression was reported where it appears.
        if (declaration.HasPlaceholder && !_context.AllowDynamic) return;
        if (declaration.IsCustomProperty)
        {
            TranslateCustomProperty();
            return;
        }

        // -webkit-…: a vendor copy of a standard property. Inside @keyframes, CSS ignores animation properties.
        if (name.StartsWith("-", StringComparison.Ordinal)) return;
        if (_inKeyframe && (name.StartsWith("animation", StringComparison.Ordinal) || name.StartsWith("transition", StringComparison.Ordinal))) return;

        var values = CssParser.ParseComponentValues(declaration.Value);
        var components = CssParser.Components(values);
        if (s_ignored.Contains(name) || (name == "appearance" && components.Count == 1 && (components[0].IsIdent("none") || components[0].IsIdent("auto"))))
        {
            return;
        }

        if (components.Count == 1 && WideKeyword(components[0]) is { } keyword)
        {
            if (!s_targets.TryGetValue(name, out var targets))
            {
                Report(CssDiagnosticKind.UnsupportedProperty, declaration.NameStart, name.Length, name);
                return;
            }

            ResetListState(name);
            foreach (var target in targets) _pending.Add((target, keyword));
            Commit();
            return;
        }

        if (!TranslateProperty(name, values, components))
        {
            Report(CssDiagnosticKind.UnsupportedProperty, declaration.NameStart, name.Length, name);
            return;
        }

        if (_error != null)
        {
            if (_dynamic) Report(CssDiagnosticKind.Dynamic, declaration.Start, declaration.Length, $"'{Snippet(declaration.Start, declaration.Length)}' — {_error}");
            else Report(CssDiagnosticKind.UnsupportedValue, declaration.Start, declaration.Length, name, _error);
            return;
        }

        if (declaration.Important) _notes.Add("!important is not supported; the declaration has normal precedence.");
        Commit();
    }

    private void Commit()
    {
        foreach (var (property, expression) in _pending) _block.Set(property, expression);
        foreach (var action in _onCommit) action();
        foreach (var note in _notes)
        {
            Report(CssDiagnosticKind.Approximated, _declaration.Start, _declaration.Length, Snippet(_declaration.Start, _declaration.Length), note);
        }
    }

    private void Report(CssDiagnosticKind kind, int start, int length, params string[] args)
        => _context.Diagnostics.Add(new CssDiagnostic(kind, start, length, args));

    /// <summary>Records why the declaration cannot be translated; the first reason wins.</summary>
    private string? Fail(string message)
    {
        _error ??= message;
        return null;
    }

    private string? FailDynamic(string message)
    {
        if (_error == null)
        {
            _error = message;
            _dynamic = true;
        }

        return null;
    }

    private bool Failed => _error != null;

    private void Assign(string property, string? expression)
    {
        if (expression != null) _pending.Add((property, expression));
        else Fail("the value is not supported.");
    }

    private void Note(string note)
    {
        if (!_notes.Contains(note)) _notes.Add(note);
    }

    private static string? WideKeyword(CssValue value)
    {
        if (value.Children != null || value.Kind != CssTokenKind.Ident) return null;
        return value.Token.Value.ToLowerInvariant() switch
        {
            "inherit" => CssType + ".Inherit",
            "initial" => CssType + ".Initial",
            "unset" => CssType + ".Unset",
            "revert" => CssType + ".Revert",
            "revert-layer" => CssType + ".RevertLayer",
            _ => null,
        };
    }

    /// <summary>The CSS text of a range, with C# expressions shown as <c>@(…)</c>.</summary>
    private string Snippet(int start, int length)
    {
        var text = _context.Text;
        start = Math.Max(0, Math.Min(start, text.Length));
        length = Math.Max(0, Math.Min(length, text.Length - start));
        var builder = new StringBuilder();
        for (var i = start; i < start + length; i++)
        {
            var c = text[i];
            if (c == CssPlaceholder.Open)
            {
                builder.Append("@(…)");
                while (i < start + length && text[i] != CssPlaceholder.Close) i++;
            }
            else
            {
                builder.Append(c);
            }
        }

        var snippet = builder.ToString().Trim();
        return snippet.Length > 80 ? snippet.Substring(0, 77) + "..." : snippet;
    }

    private string Snippet(CssValue value) => Snippet(value.Start, value.End - value.Start);

    /// <summary>A C# string literal.</summary>
    internal static string Str(string value)
    {
        var builder = new StringBuilder("\"");
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (c < 0x20 || c is '\u2028' or '\u2029' or '\u0085') builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else builder.Append(c);
                    break;
            }
        }

        return builder.Append('"').ToString();
    }

    /// <summary>
    /// <c>--name: value</c> → <c>Vars["--name"]</c>. Custom properties are typed at run time
    /// (<c>VarValue</c>), so the value is read as a color, a length, a number or a string.
    /// </summary>
    private void TranslateCustomProperty()
    {
        var name = _declaration.Name;
        var values = CssParser.ParseComponentValues(_declaration.Value);
        var components = CssParser.Components(values);
        string? expression = null;
        if (components.Count == 0)
        {
            expression = Str(string.Empty);
        }
        else if (components.Count == 1)
        {
            var value = components[0];
            if (value.Kind == CssTokenKind.Placeholder)
            {
                expression = value.Token.Value.Length > 0 ? DynamicLength(value) : $"{BuilderType}.CssNumber({Placeholder(value)})";
            }
            else if (value.IsFunction && value.FunctionName == "var")
            {
                Fail("a custom property cannot refer to another custom property in Miko.");
            }
            else if (value.Kind is CssTokenKind.Number && value.Children == null)
            {
                expression = Float(value.Token.Number);
            }
            else if (value.Kind is CssTokenKind.String && value.Children == null)
            {
                expression = Str(value.Token.Value);
            }
            else if (value.Kind is CssTokenKind.Hash || value.IsFunction && value.FunctionName is "rgb" or "rgba" or "hsl" or "hsla"
                     || value.Kind == CssTokenKind.Ident && (s_namedColors.ContainsKey(value.Token.Value.ToLowerInvariant()) || value.IsIdent("transparent")))
            {
                expression = Color(value);
            }
            else if (value.Kind is CssTokenKind.Dimension or CssTokenKind.Percentage || value.IsFunction && value.FunctionName is "calc" or "env")
            {
                expression = Length(value, LengthOptions.Percent | LengthOptions.Negative);
            }
            else if (value.Kind == CssTokenKind.Ident)
            {
                expression = Str(value.Token.Value);
            }
        }
        else if (CssParser.SplitByComma(values).TrueForAll(part => part.Count == 1 && part[0].Kind is CssTokenKind.Ident or CssTokenKind.String))
        {
            // A font-family list.
            expression = Str(FontFamilyList(values));
        }

        if (expression == null && !Failed) Fail("only a color, a length, a number or a string can be stored in a custom property.");
        if (Failed)
        {
            if (_dynamic) Report(CssDiagnosticKind.Dynamic, _declaration.Start, _declaration.Length, $"'{Snippet(_declaration.Start, _declaration.Length)}' — {_error}");
            else Report(CssDiagnosticKind.UnsupportedValue, _declaration.Start, _declaration.Length, name, _error!);
            return;
        }

        _block.SetVar(name, $"({VarValueType})({expression})");
    }
}
