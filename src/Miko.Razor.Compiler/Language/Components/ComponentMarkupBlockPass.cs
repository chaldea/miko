// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

// Rewrites contiguous subtrees of HTML into a special node type to reduce the
// size of the Render tree.
//
// Does not preserve insignificant details of the HTML, like tag closing style
// or quote style.
internal sealed class ComponentMarkupBlockPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    private readonly RazorLanguageVersion _version;

    public ComponentMarkupBlockPass(RazorLanguageVersion version)
    {
        _version = version;
    }

    // Runs LATE because we want to destroy structure.
    //
    // We also need to run after ComponentMarkupDiagnosticPass to avoid destroying diagnostics
    // added in that pass.
    public override int Order => ComponentMarkupDiagnosticPass.DefaultOrder + 10;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        // Disabled: Miko requires all elements to be individually parsed.
        // Do not collapse static HTML into MarkupBlock nodes.
        return;
    }

    // Finds HTML-blocks using a postorder traversal. We store nodes in an
    // ordered list so we can avoid redundant rewrites.
    //
    // Consider a case like:
    //  <div>
    //    <a href="...">click me</a>
    //  </div>
    //
    // We would store both the div and a tag in a list, but make sure to visit
    // the div first. Then when we process the div (recursively), we would remove
    // the a from the list.
    private class FindHtmlTreeVisitor : IntermediateNodeWalker
    {
        private readonly RazorLanguageVersion _version;

        public FindHtmlTreeVisitor(RazorLanguageVersion version)
        {
            _version = version;
        }

        private bool _foundNonHtml;

        public List<IntermediateNodeReference> Trees { get; } = new List<IntermediateNodeReference>();

        public override void VisitDefault(IntermediateNode node)
        {
            // If we get here, we found a non-HTML node. Keep traversing.
            _foundNonHtml = true;
            base.VisitDefault(node);
        }

        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            // We need to restore the state after processing this node.
            // We might have found a leaf-block of HTML, but that shouldn't
            // affect our parent's state.
            var originalState = _foundNonHtml;

            _foundNonHtml = false;

            if (node.HasDiagnostics)
            {
                // Treat node with errors as non-HTML - don't let the parent rewrite this either.
                _foundNonHtml = true;
            }

            if (_version <= RazorLanguageVersion.Version_7_0 &&
                string.Equals("script", node.TagName, StringComparison.OrdinalIgnoreCase))
            {
                // Treat script tags as non-HTML in .NET 7 and earlier.
                _foundNonHtml = true;
            }
            else if (string.Equals("option", node.TagName, StringComparison.OrdinalIgnoreCase))
            {
                // Also, treat <option>...</option> as non-HTML - we don't want it to be coalesced so that we can support setting "selected" attribute on it.
                // We only care about option tags that are nested under a select tag.
                foreach (var ancestor in Ancestors)
                {
                    if (ancestor is MarkupElementIntermediateNode element &&
                        string.Equals("select", element.TagName, StringComparison.OrdinalIgnoreCase))
                    {
                        _foundNonHtml = true;
                        break;
                    }
                }
            }

            base.VisitDefault(node);

            if (!_foundNonHtml)
            {
                Trees.Add(new IntermediateNodeReference(node, Parent!));
            }

            _foundNonHtml = originalState |= _foundNonHtml;
        }

        public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
        {
            if (node.HasDiagnostics)
            {
                // Treat node with errors as non-HTML
                _foundNonHtml = true;
            }

            // Visit Children
            base.VisitDefault(node);
        }

        public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
        {
            if (node.HasDiagnostics)
            {
                // Treat node with errors as non-HTML
                _foundNonHtml = true;
            }

            // Visit Children
            base.VisitDefault(node);
        }

        public override void VisitHtml(HtmlContentIntermediateNode node)
        {
            // We need to restore the state after processing this node.
            // We might have found a leaf-block of HTML, but that shouldn't
            // affect our parent's state.
            var originalState = _foundNonHtml;

            _foundNonHtml = false;

            if (node.HasDiagnostics)
            {
                // Treat node with errors as non-HTML
                _foundNonHtml = true;
            }

            // Visit Children
            base.VisitDefault(node);

            if (!_foundNonHtml)
            {
                Trees.Add(new IntermediateNodeReference(node, Parent!));
            }

            _foundNonHtml = originalState |= _foundNonHtml;
        }

        public override void VisitToken(IntermediateToken node)
        {
            if (node.HasDiagnostics)
            {
                // Treat node with errors as non-HTML
                _foundNonHtml = true;
            }

            if (node is CSharpIntermediateToken)
            {
                _foundNonHtml = true;
            }
        }
    }

    private class RewriteVisitor : IntermediateNodeWalker
    {
        private readonly List<IntermediateNodeReference> _trees;

        public RewriteVisitor(List<IntermediateNodeReference> trees)
        {
            _trees = trees;
        }

        public StringBuilder Builder { get; } = new StringBuilder();

        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            for (var i = 0; i < _trees.Count; i++)
            {
                // Remove this node if it's in the list. This ensures that we don't
                // do redundant operations.
                if (ReferenceEquals(_trees[i].Node, node))
                {
                    _trees.RemoveAt(i);
                    break;
                }
            }

            var isVoid = Legacy.ParserHelpers.VoidElements.Contains(node.TagName);
            var hasBodyContent = node.Body.Any();

            Builder.Append('<');
            Builder.Append(node.TagName);

            foreach (var attribute in node.Attributes)
            {
                Visit(attribute);
            }

            // If for some reason a void element contains body, then treat it as a
            // start/end tag.
            if (!hasBodyContent && isVoid)
            {
                // void
                Builder.Append('>');
                return;
            }
            else if (!hasBodyContent)
            {
                // In HTML5, we can't have self-closing non-void elements, so explicitly
                // add a close tag
                Builder.Append("></");
                Builder.Append(node.TagName);
                Builder.Append('>');
                return;
            }

            // start/end tag with body.
            Builder.Append('>');

            foreach (var item in node.Body)
            {
                Visit(item);
            }

            Builder.Append("</");
            Builder.Append(node.TagName);
            Builder.Append('>');
        }

        public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
        {
            Builder.Append(' ');
            Builder.Append(node.AttributeName);

            if (node.Children.Count == 0)
            {
                // Minimized attribute
                return;
            }

            // We examine the node.Prefix (e.g. " onfocus='" or " on focus=\"")
            // to preserve the quote type that is used in the original markup.
            var quoteType = node.Prefix.EndsWith("'", StringComparison.Ordinal) ? "'" : "\"";

            Builder.Append('=');
            Builder.Append(quoteType);

            // Visit Children
            base.VisitDefault(node);

            Builder.Append(quoteType);
        }

        public override void VisitHtmlAttributeValue(HtmlAttributeValueIntermediateNode node)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                Builder.Append(node.Prefix);

                if (node.Children[i] is IntermediateToken token)
                {
                    Builder.Append(token.Content);
                }
            }
        }

        public override void VisitHtml(HtmlContentIntermediateNode node)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token)
                {
                    Builder.Append(token.Content);
                }
            }
        }
    }
}
