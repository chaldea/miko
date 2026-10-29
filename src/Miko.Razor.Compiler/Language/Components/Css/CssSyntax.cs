// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

internal abstract class CssRuleNode
{
}

/// <summary><c>selector-list { declarations }</c>.</summary>
internal sealed class CssStyleRuleNode : CssRuleNode
{
    public CssStyleRuleNode(string selector, int selectorStart, int selectorLength, List<CssDeclarationNode> declarations)
    {
        Selector = selector;
        SelectorStart = selectorStart;
        SelectorLength = selectorLength;
        Declarations = declarations;
    }

    /// <summary>The selector text, comments removed.</summary>
    public string Selector { get; }
    public int SelectorStart { get; }
    public int SelectorLength { get; }
    public List<CssDeclarationNode> Declarations { get; }
}

/// <summary><c>@media prelude { rules }</c>.</summary>
internal sealed class CssMediaRuleNode : CssRuleNode
{
    public CssMediaRuleNode(List<CssToken> prelude, int start, int length, List<CssStyleRuleNode> rules)
    {
        Prelude = prelude;
        Start = start;
        Length = length;
        Rules = rules;
    }

    public List<CssToken> Prelude { get; }
    public int Start { get; }
    public int Length { get; }
    public List<CssStyleRuleNode> Rules { get; }
}

/// <summary><c>@keyframes name { 0% { … } to { … } }</c>.</summary>
internal sealed class CssKeyframesRuleNode : CssRuleNode
{
    public CssKeyframesRuleNode(string name, int start, int length, List<CssKeyframeNode> frames)
    {
        Name = name;
        Start = start;
        Length = length;
        Frames = frames;
    }

    public string Name { get; }
    public int Start { get; }
    public int Length { get; }
    public List<CssKeyframeNode> Frames { get; }
}

internal sealed class CssKeyframeNode
{
    public CssKeyframeNode(List<float> offsets, List<CssDeclarationNode> declarations)
    {
        Offsets = offsets;
        Declarations = declarations;
    }

    /// <summary>0–1.</summary>
    public List<float> Offsets { get; }
    public List<CssDeclarationNode> Declarations { get; }
}

/// <summary><c>name: value [!important]</c>.</summary>
internal sealed class CssDeclarationNode
{
    public CssDeclarationNode(string name, int nameStart, List<CssToken> value, bool important, int start, int length)
    {
        Name = name;
        NameStart = nameStart;
        Value = value;
        Important = important;
        Start = start;
        Length = length;
    }

    /// <summary>Lowercased, except for custom properties (<c>--Name</c> is case-sensitive).</summary>
    public string Name { get; }
    public int NameStart { get; }

    /// <summary>The value tokens, trimmed, without <c>!important</c>.</summary>
    public List<CssToken> Value { get; }
    public bool Important { get; }

    /// <summary>The whole declaration, for diagnostics.</summary>
    public int Start { get; }
    public int Length { get; }

    public bool IsCustomProperty => Name.StartsWith("--", System.StringComparison.Ordinal);

    public bool HasPlaceholder => Value.Any(t => t.Kind == CssTokenKind.Placeholder);
}

internal sealed class CssStyleSheetNode
{
    public List<CssRuleNode> Rules { get; } = new();
}

/// <summary>
/// A component value: a token, a function with its arguments, or a <c>(…)</c> / <c>[…]</c> block
/// (CSS Syntax §5.4.9).
/// </summary>
internal sealed class CssValue
{
    private CssValue(CssToken token, List<CssValue>? children, int end)
    {
        Token = token;
        Children = children;
        End = end;
    }

    public static CssValue Leaf(CssToken token) => new(token, null, token.End);

    public static CssValue Group(CssToken opener, List<CssValue> children, int end) => new(opener, children, end);

    /// <summary>The token itself, or the function / opening bracket token.</summary>
    public CssToken Token { get; }

    /// <summary>The arguments of a function or the content of a block; null for a plain token.</summary>
    public List<CssValue>? Children { get; }

    /// <summary>End offset in the CSS text (after the closing bracket of a group).</summary>
    public int End { get; }

    public CssTokenKind Kind => Token.Kind;

    public bool IsFunction => Token.Kind == CssTokenKind.Function;

    public bool IsWhitespace => Token.Kind == CssTokenKind.Whitespace && Children == null;

    public bool IsComma => Token.Kind == CssTokenKind.Comma;

    public string FunctionName => Token.Value.ToLowerInvariant();

    public int Start => Token.Start;

    public bool IsIdent(string name) => Children == null && Token.IsIdent(name);

    public bool IsDelim(char c) => Children == null && Token.IsDelim(c);

    public bool ContainsPlaceholder()
        => Token.Kind == CssTokenKind.Placeholder || (Children?.Any(c => c.ContainsPlaceholder()) ?? false);

    public bool ContainsFunction(string name)
        => (IsFunction && FunctionName == name) || (Children?.Any(c => c.ContainsFunction(name)) ?? false);
}
