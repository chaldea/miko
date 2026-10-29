// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Components.Css;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

/// <summary>
/// Compiles a component's CSS into it (ISSUE-161): the content of its <c>&lt;style&gt;</c>
/// elements and its <c>.razor.css</c> file become a static method that builds a
/// <c>Miko.Styling.StyleSheet</c>, registered by <c>BuildRenderTree</c> the first time the component
/// renders. The <c>&lt;style&gt;</c> elements are removed from the markup — Miko has no style element.
///
/// <para>The CSS is translated here, never at run time; what cannot be translated is reported
/// (RZ10102–RZ10108). <c>&lt;style&gt;</c> rules apply to the whole app, as in a browser;
/// <c>.razor.css</c> rules only to the component's own elements (Blazor CSS isolation, the scope
/// attribute being added by <see cref="ComponentCssScopePass"/>).</para>
///
/// <para>Runs before <see cref="ComponentWhitespacePass"/>, so the CSS text is still the source
/// text and its positions map back to the .razor file.</para>
/// </summary>
internal sealed class ComponentStylePass : ComponentIntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    public override int Order => -10;

    private const string BuildMethodName = "__MikoBuildComponentStyles";

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode, CancellationToken cancellationToken)
    {
        if (!IsComponentDocument(documentNode) || documentNode.Options.SuppressPrimaryMethodBody)
        {
            return;
        }

        var emitter = new CssStyleSheetEmitter();
        foreach (var reference in documentNode.FindDescendantReferences<MarkupElementIntermediateNode>())
        {
            if (!string.Equals(reference.Node.TagName, "style", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!documentNode.Options.DesignTime)
            {
                AddStyleElement(documentNode, reference.Node, emitter);
            }

            reference.Remove();
        }

        var scopedCss = codeDocument.GetScopedCss();
        var scope = codeDocument.CodeGenerationOptions.CssScope;
        if (scopedCss == null && !string.IsNullOrEmpty(scope) && !documentNode.Options.DesignTime && codeDocument.Source.FilePath is { } path)
        {
            // The SDK scoped the component (it has a .razor.css), but the file never reached the generator.
            documentNode.AddDiagnostic(RazorDiagnostic.Create(ComponentDiagnosticFactory.ScopedCssNotReceived, new SourceSpan(path, 0, 0, 0, 1)));
        }
        else if (scopedCss != null && !string.IsNullOrEmpty(scope) && !documentNode.Options.DesignTime)
        {
            var diagnostics = new List<CssDiagnostic>();
            emitter.Add(scopedCss.Text, scope, diagnostics);
            var map = CssSourceMap.ForFile(scopedCss.FilePath, scopedCss.Text);
            foreach (var diagnostic in diagnostics)
            {
                documentNode.AddDiagnostic(diagnostic.ToRazorDiagnostic(map));
            }
        }

        if (emitter.HasRules)
        {
            AddMembers(documentNode, emitter.Body);
        }
    }

    private static void AddStyleElement(DocumentIntermediateNode documentNode, MarkupElementIntermediateNode element, CssStyleSheetEmitter emitter)
    {
        var css = new StringBuilder();
        var map = new CssSourceMap();

        // <style media="…"> applies its rules under that query.
        var media = element.Children.OfType<HtmlAttributeIntermediateNode>()
            .FirstOrDefault(a => string.Equals(a.AttributeName, "media", StringComparison.OrdinalIgnoreCase));
        var mediaText = media == null ? null : LiteralAttribute(documentNode, media);
        if (mediaText != null)
        {
            css.Append("@media ");
            AppendAttributeValue(media!, map, css);
            css.Append(" {\n");
        }

        var placeholders = 0;
        foreach (var child in element.Body)
        {
            if (child is HtmlContentIntermediateNode content)
            {
                foreach (var token in content.Children.OfType<HtmlIntermediateToken>())
                {
                    map.Add(css.Length, token.Content, token.Source, exact: true);
                    css.Append(token.Content);
                }
            }
            else
            {
                // A component's styles are built once per component type: there is no instance
                // to take a value from.
                documentNode.AddDiagnostic(RazorDiagnostic.Create(ComponentDiagnosticFactory.DynamicCss, child.Source,
                    "a <style> element is compiled once per component; use a style=\"…\" attribute or a Style object for values that change."));
                css.Append(CssPlaceholder.Encode(placeholders++));
            }
        }

        if (mediaText != null) css.Append("\n}");

        var diagnostics = new List<CssDiagnostic>();
        emitter.Add(css.ToString(), scope: null, diagnostics);
        foreach (var diagnostic in diagnostics)
        {
            documentNode.AddDiagnostic(diagnostic.ToRazorDiagnostic(map));
        }
    }

    /// <summary>The literal value of an attribute, or null (reported) when it holds C#.</summary>
    private static string? LiteralAttribute(DocumentIntermediateNode documentNode, HtmlAttributeIntermediateNode attribute)
    {
        var builder = new StringBuilder();
        foreach (var child in attribute.Children)
        {
            if (child is HtmlAttributeValueIntermediateNode value)
            {
                builder.Append(value.Prefix);
                foreach (var token in value.Children.OfType<IntermediateToken>()) builder.Append(token.Content);
            }
            else
            {
                documentNode.AddDiagnostic(RazorDiagnostic.Create(ComponentDiagnosticFactory.DynamicCss, child.Source ?? attribute.Source,
                    "the media attribute of a <style> element must be literal."));
                return null;
            }
        }

        var text = builder.ToString().Trim();
        return text.Length == 0 ? null : text;
    }

    private static void AppendAttributeValue(HtmlAttributeIntermediateNode attribute, CssSourceMap map, StringBuilder css)
    {
        foreach (var value in attribute.Children.OfType<HtmlAttributeValueIntermediateNode>())
        {
            var text = value.Prefix + string.Concat(value.Children.OfType<IntermediateToken>().Select(t => t.Content));
            map.Add(css.Length, text, value.Source, exact: true);
            css.Append(text);
        }
    }

    /// <summary>
    /// <c>private static StyleSheet __MikoBuildComponentStyles()</c> on the class, and the call that
    /// registers it at the top of <c>BuildRenderTree</c>.
    /// </summary>
    private static void AddMembers(DocumentIntermediateNode documentNode, string body)
    {
        var @class = documentNode.FindPrimaryClass();
        var method = documentNode.FindPrimaryMethod();
        if (@class?.Name == null || method == null)
        {
            return;
        }

        var builder = method.Parameters.Length > 0 ? method.Parameters[0].Name : "__builder";
        var typeParameters = @class.TypeParameters.Length;
        var type = typeParameters == 0 ? @class.Name : @class.Name + "<" + new string(',', typeParameters - 1) + ">";

        var build = new StringBuilder()
            .Append("private static global::Miko.Styling.StyleSheet ").Append(BuildMethodName).Append("()\n")
            .Append("        {\n")
            .Append("            var sheet = new global::Miko.Styling.StyleSheet();\n")
            .Append(body)
            .Append("            return sheet;\n")
            .Append("        }\n")
            .ToString();

        @class.Children.Add(new CSharpCodeIntermediateNode
        {
            Children = { IntermediateNodeFactory.CSharpToken(build) },
        });

        method.Children.Insert(0, new CSharpCodeIntermediateNode
        {
            Children =
            {
                IntermediateNodeFactory.CSharpToken(
                    $"{builder}.AddComponentStyles(typeof({type}), \"{Hash(build)}\", {BuildMethodName});"),
            },
        });
    }

    /// <summary>FNV-1a over the generated code: a component recompiled with other CSS (hot reload) re-registers.</summary>
    private static string Hash(string text)
    {
        var hash = 14695981039346656037UL;
        foreach (var c in text)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }

        return hash.ToString("x16", CultureInfo.InvariantCulture);
    }
}
