// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

/// <summary>
/// Emits the statements that build a component's <c>Miko.Styling.StyleSheet</c> from its CSS
/// (ISSUE-161): one <c>sheet.Add(new CssObject { [selector] = rule })</c> per rule, in source order
/// so the cascade's definition order matches the CSS, and <c>sheet.Add(…, media)</c> for
/// <c>@media</c> blocks. Selectors stay strings (the runtime parses them once, when the component
/// first renders); every declaration is typed C#.
/// </summary>
internal sealed class CssStyleSheetEmitter
{
    private const string CssObjectType = "global::Miko.Styling.CssObject";
    private const string Indent = "            ";

    private readonly StringBuilder _body = new();
    private int _variables;

    /// <summary>Whether any rule was emitted.</summary>
    public bool HasRules { get; private set; }

    /// <summary>
    /// Translates one stylesheet. <paramref name="scope"/>: the <c>.razor.css</c> scope, null for a
    /// <c>&lt;style&gt;</c> element (whose <c>media</c> attribute the caller wraps around the text as <c>@media</c>).
    /// </summary>
    public void Add(string css, string? scope, List<CssDiagnostic> diagnostics)
    {
        var sheet = CssParser.ParseStyleSheet(css, diagnostics);
        var context = new CssTranslationContext(css, diagnostics, allowDynamic: false);
        foreach (var rule in sheet.Rules)
        {
            if (rule is CssKeyframesRuleNode keyframes) context.Keyframes[keyframes.Name] = keyframes;
        }

        foreach (var rule in sheet.Rules)
        {
            switch (rule)
            {
                case CssStyleRuleNode style:
                    EmitRule(context, style, scope, media: null);
                    break;
                case CssMediaRuleNode mediaRule:
                    var condition = CssMediaQueryTranslator.Translate(mediaRule.Prelude, mediaRule.Start, mediaRule.Length, context);
                    if (condition == CssMediaQueryTranslator.Never) break;
                    var slot = condition == CssMediaQueryTranslator.Always ? null : new MediaSlot(condition);
                    foreach (var inner in mediaRule.Rules) EmitRule(context, inner, scope, slot);
                    break;
            }
        }

        // Keyframes nothing refers to are still translated, so their problems are reported.
        foreach (var name in context.Keyframes.Keys) CssPropertyTranslator.TranslateKeyframes(context, name);
    }

    /// <summary>A media condition, declared as a variable before the first rule that uses it.</summary>
    private sealed class MediaSlot(string condition)
    {
        public string Condition { get; } = condition;
        public string? Variable { get; set; }
    }

    private string Use(MediaSlot slot)
    {
        if (slot.Variable == null)
        {
            slot.Variable = "__media" + (_variables++).ToString(CultureInfo.InvariantCulture);
            _body.Append(Indent).Append("var ").Append(slot.Variable).Append(" = ").Append(CssMediaQueryTranslator.ToExpression(slot.Condition)).Append(";\n");
        }

        return slot.Variable;
    }

    private void EmitRule(CssTranslationContext context, CssStyleRuleNode rule, string? scope, MediaSlot? media)
    {
        // A C# expression in a <style> element is reported where it appears.
        if (rule.Selector.IndexOf(CssPlaceholder.Open) >= 0) return;

        var selectors = CssSelectorTranslator.Translate(rule.Selector, rule.SelectorStart, rule.SelectorLength, scope,
            allowPseudoElements: media == null, context.Diagnostics);
        var block = CssPropertyTranslator.TranslateBlock(context, rule.Declarations);
        if (selectors.Count == 0 || block.IsEmpty) return;

        var mediaArgument = media == null ? "" : ", " + Use(media);
        var style = block.ToInitializer(CssObjectType, Indent + "    ");
        if (selectors.Count == 1)
        {
            _body.Append(Indent).Append("sheet.Add(new ").Append(CssObjectType).Append(" { [")
                .Append(CssPropertyTranslator.Str(selectors[0])).Append("] = ").Append(style).Append(" }").Append(mediaArgument).Append(");\n");
        }
        else
        {
            // One rule object under each selector: each branch keeps its own specificity.
            var variable = "__rule" + (_variables++).ToString(CultureInfo.InvariantCulture);
            _body.Append(Indent).Append("var ").Append(variable).Append(" = ").Append(style).Append(";\n");
            _body.Append(Indent).Append("sheet.Add(new ").Append(CssObjectType).Append(" { ");
            for (var i = 0; i < selectors.Count; i++)
            {
                if (i > 0) _body.Append(", ");
                _body.Append('[').Append(CssPropertyTranslator.Str(selectors[i])).Append("] = ").Append(variable);
            }

            _body.Append(" }").Append(mediaArgument).Append(");\n");
        }

        HasRules = true;
    }

    /// <summary>The statements that fill <c>sheet</c>.</summary>
    public string Body => _body.ToString();
}
