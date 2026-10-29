// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Components.Css;

internal sealed partial class CssPropertyTranslator
{
    /// <summary>Translates one property: assigns its longhands, or calls <see cref="Fail(string)"/>.</summary>
    private delegate void Handler(CssPropertyTranslator t, List<CssValue> values, List<CssValue> components);

    private sealed class Property(string[] targets, Handler handler)
    {
        /// <summary>The <c>Style</c> properties it sets (also what a CSS-wide keyword sets).</summary>
        public string[] Targets { get; } = targets;
        public Handler Handler { get; } = handler;
    }

    /// <summary>Properties with no effect on what Miko draws: dropped without a warning.</summary>
    private static readonly HashSet<string> s_ignored = new(StringComparer.Ordinal)
    {
        "will-change", "contain", "content-visibility", "text-rendering", "color-scheme", "font-display",
        "forced-color-adjust", "print-color-adjust", "font-kerning", "font-smooth", "backface-visibility",
        "text-size-adjust", "isolation", "speak", "page-break-inside", "page-break-before", "page-break-after",
        "break-inside", "break-before", "break-after",
    };

    private static Dictionary<string, Property>? s_propertyTable;

    private static Dictionary<string, Property> Properties => s_propertyTable ??= BuildProperties();

    /// <summary>Property name → the Style properties a CSS-wide keyword assigns.</summary>
    private static Dictionary<string, string[]> s_targets => s_targetTable ??= BuildTargets();

    private static Dictionary<string, string[]>? s_targetTable;

    private static Dictionary<string, string[]> BuildTargets()
    {
        var targets = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var pair in Properties) targets[pair.Key] = pair.Value.Targets;
        return targets;
    }

    /// <summary>The CSS properties the translator knows, for documentation and tests.</summary>
    internal static IEnumerable<string> SupportedProperties => Properties.Keys;

    /// <summary>Every <c>Style</c> property the translator assigns, for tests.</summary>
    internal static IEnumerable<string> AssignedStyleProperties
        => Properties.Values.SelectMany(p => p.Targets).Concat(s_transitionProperties.Values.SelectMany(t => t)).Distinct();

    private bool TranslateProperty(string name, List<CssValue> values, List<CssValue> components)
    {
        if (!Properties.TryGetValue(name, out var property)) return false;
        property.Handler(this, values, components);
        return true;
    }

    private static void Add(Dictionary<string, Property> table, string name, string target, Func<CssPropertyTranslator, CssValue, string?> parse)
        => table[name] = new Property(new[] { target }, (t, _, c) => t.Assign(target, t.Single(c, v => parse(t, v))));

    private static void AddLength(Dictionary<string, Property> table, string name, string target, LengthOptions options)
        => Add(table, name, target, (t, v) => t.Length(v, options));

    private static void AddColor(Dictionary<string, Property> table, string name, string target)
        => Add(table, name, target, (t, v) => t.Color(v));

    private static void AddEnum(Dictionary<string, Property> table, string name, string target, string enumType, Dictionary<string, string> map)
        => Add(table, name, target, (t, v) => t.Enum(v, enumType, map));

    private static void AddShorthand(Dictionary<string, Property> table, string name, string[] targets, Handler handler)
        => table[name] = new Property(targets, handler);

    private static readonly string[] s_sides = { "Top", "Right", "Bottom", "Left" };
    private static readonly string[] s_logicalSides = { "InlineStart", "InlineEnd", "BlockStart", "BlockEnd" };

    private static string[] Sides(string prefix, string suffix = "")
        => Array.ConvertAll(s_sides, side => prefix + side + suffix);

    private static Dictionary<string, Property> BuildProperties()
    {
        var p = new Dictionary<string, Property>(StringComparer.Ordinal);
        const string C = CommonNs;

        // Sizes
        foreach (var (css, cs) in new[] { ("width", "Width"), ("height", "Height"), ("inline-size", "InlineSize"), ("block-size", "BlockSize") })
            AddLength(p, css, cs, LengthOptions.Size);
        foreach (var (css, cs) in new[] { ("min-width", "MinWidth"), ("min-height", "MinHeight"), ("min-inline-size", "MinInlineSize"), ("min-block-size", "MinBlockSize") })
            AddLength(p, css, cs, LengthOptions.Percent | LengthOptions.AutoIsInitial | LengthOptions.FitContent | LengthOptions.Intrinsic);
        foreach (var (css, cs) in new[] { ("max-width", "MaxWidth"), ("max-height", "MaxHeight"), ("max-inline-size", "MaxInlineSize"), ("max-block-size", "MaxBlockSize") })
            AddLength(p, css, cs, LengthOptions.Percent | LengthOptions.NoneIsInitial | LengthOptions.FitContent | LengthOptions.Intrinsic);

        // Box edges
        foreach (var side in s_sides)
        {
            var css = side.ToLowerInvariant();
            AddLength(p, "padding-" + css, "Padding" + side, LengthOptions.Percent);
            AddLength(p, "margin-" + css, "Margin" + side, LengthOptions.Offset);
            AddLength(p, css, side, LengthOptions.Offset);
            AddLength(p, $"border-{css}-width", $"Border{side}Width", LengthOptions.BorderWidth);
            AddColor(p, $"border-{css}-color", $"Border{side}Color");
            AddEnum(p, $"border-{css}-style", $"Border{side}Style", C + "BorderStyle", BorderStyleMap);
        }

        foreach (var side in s_logicalSides)
        {
            var css = side == "InlineStart" ? "inline-start" : side == "InlineEnd" ? "inline-end" : side == "BlockStart" ? "block-start" : "block-end";
            AddLength(p, "padding-" + css, "Padding" + side, LengthOptions.Percent);
            AddLength(p, "margin-" + css, "Margin" + side, LengthOptions.Offset);
            AddLength(p, "inset-" + css, "Inset" + side, LengthOptions.Offset);
            AddLength(p, $"border-{css}-width", $"Border{side}Width", LengthOptions.BorderWidth);
            AddColor(p, $"border-{css}-color", $"Border{side}Color");
            AddEnum(p, $"border-{css}-style", $"Border{side}Style", C + "BorderStyle", BorderStyleMap);
        }

        foreach (var (css, cs) in new[] { ("top-left", "TopLeft"), ("top-right", "TopRight"), ("bottom-right", "BottomRight"), ("bottom-left", "BottomLeft") })
            AddShorthand(p, $"border-{css}-radius", new[] { $"Border{cs}Radius" }, (t, v, c) => t.Radius(c, $"Border{cs}Radius"));

        AddLength(p, "outline-width", "OutlineWidth", LengthOptions.BorderWidth);
        AddLength(p, "outline-offset", "OutlineOffset", LengthOptions.Negative);
        AddColor(p, "outline-color", "OutlineColor");
        AddEnum(p, "outline-style", "OutlineStyle", C + "BorderStyle", BorderStyleMap);

        // Text
        AddLength(p, "font-size", "FontSize", LengthOptions.FontSize | LengthOptions.PercentIsEm);
        AddLength(p, "line-height", "LineHeight", LengthOptions.Unitless | LengthOptions.NormalIsInitial | LengthOptions.PercentIsEm);
        AddLength(p, "letter-spacing", "LetterSpacing", LengthOptions.Negative | LengthOptions.NormalIsInitial);
        AddColor(p, "color", "Color");
        AddColor(p, "caret-color", "CaretColor");
        AddEnum(p, "font-style", "FontStyle", C + "FontStyle", FontStyleMap);
        AddEnum(p, "text-align", "TextAlign", C + "TextAlign", TextAlignMap);
        AddEnum(p, "text-transform", "TextTransform", C + "TextTransform", TextTransformMap);
        AddEnum(p, "white-space", "WhiteSpace", C + "WhiteSpace", WhiteSpaceMap);
        AddEnum(p, "overflow-wrap", "OverflowWrap", C + "OverflowWrap", OverflowWrapMap);
        AddEnum(p, "word-wrap", "OverflowWrap", C + "OverflowWrap", OverflowWrapMap);
        AddEnum(p, "text-overflow", "TextOverflow", C + "TextOverflow", TextOverflowMap);
        AddEnum(p, "vertical-align", "VerticalAlign", C + "VerticalAlign", VerticalAlignMap);
        AddEnum(p, "direction", "Direction", C + "Direction", DirectionMap);
        AddEnum(p, "writing-mode", "WritingMode", C + "WritingMode", WritingModeMap);
        AddShorthand(p, "font-weight", new[] { "FontWeight" }, (t, v, c) => t.Assign("FontWeight", t.Single(c, t.FontWeight)));
        AddShorthand(p, "font-family", new[] { "FontFamily" }, (t, v, c) => t.Assign("FontFamily", t.FontFamily(v)));
        AddShorthand(p, "content", new[] { "Content" }, (t, v, c) => t.Assign("Content", t.Content(c)));
        AddShorthand(p, "word-break", new[] { "WordBreak", "OverflowWrap" }, (t, v, c) => t.WordBreak(c));
        AddShorthand(p, "text-decoration", new[] { "TextDecoration" }, (t, v, c) => t.TextDecoration(c, shorthand: true));
        AddShorthand(p, "text-decoration-line", new[] { "TextDecoration" }, (t, v, c) => t.TextDecoration(c, shorthand: false));

        // Layout
        AddShorthand(p, "display", new[] { "Display" }, (t, v, c) => t.Assign("Display", t.Display(c)));
        AddEnum(p, "position", "Position", C + "Position", PositionMap);
        AddEnum(p, "box-sizing", "BoxSizing", C + "BoxSizing", BoxSizingMap);
        AddEnum(p, "visibility", "Visibility", C + "Visibility", VisibilityMap);
        AddEnum(p, "overflow-x", "OverflowX", C + "Overflow", OverflowMap);
        AddEnum(p, "overflow-y", "OverflowY", C + "Overflow", OverflowMap);
        AddEnum(p, "table-layout", "TableLayout", C + "TableLayoutAlgorithm", TableLayoutMap);
        AddEnum(p, "scrollbar-width", "ScrollbarWidth", C + "ScrollbarWidth", ScrollbarWidthMap);
        AddEnum(p, "pointer-events", "PointerEvents", C + "PointerEvents", PointerEventsMap);
        AddEnum(p, "user-select", "UserSelect", C + "UserSelect", UserSelectMap);
        AddShorthand(p, "cursor", new[] { "Cursor" }, (t, v, c) => t.Assign("Cursor", t.Cursor(v)));
        AddShorthand(p, "z-index", new[] { "ZIndex" }, (t, v, c) => t.Assign("ZIndex", t.Single(c, x => x.IsIdent("auto") ? CssType + ".Initial" : t.Integer(x))));
        AddShorthand(p, "order", new[] { "Order" }, (t, v, c) => t.Assign("Order", t.Single(c, t.Integer)));
        AddShorthand(p, "opacity", new[] { "Opacity" }, (t, v, c) => t.Assign("Opacity", t.Single(c, x => t.Number(x, allowPercent: true, clampToOne: true))));
        AddShorthand(p, "aspect-ratio", new[] { "AspectRatio" }, (t, v, c) => t.Assign("AspectRatio", t.AspectRatio(c)));

        // Flex and grid alignment
        AddEnum(p, "flex-direction", "FlexDirection", C + "FlexDirection", FlexDirectionMap);
        AddEnum(p, "flex-wrap", "FlexWrap", C + "FlexWrap", FlexWrapMap);
        AddShorthand(p, "flex-grow", new[] { "FlexGrow" }, (t, v, c) => t.Assign("FlexGrow", t.Single(c, x => t.Number(x))));
        AddShorthand(p, "flex-shrink", new[] { "FlexShrink" }, (t, v, c) => t.Assign("FlexShrink", t.Single(c, x => t.Number(x))));
        AddLength(p, "flex-basis", "FlexBasis", LengthOptions.Size);
        AddLength(p, "row-gap", "RowGap", LengthOptions.Percent | LengthOptions.NormalIsInitial);
        AddLength(p, "column-gap", "ColumnGap", LengthOptions.Percent | LengthOptions.NormalIsInitial);
        AddLength(p, "grid-row-gap", "RowGap", LengthOptions.Percent | LengthOptions.NormalIsInitial);
        AddLength(p, "grid-column-gap", "ColumnGap", LengthOptions.Percent | LengthOptions.NormalIsInitial);
        foreach (var (css, cs, map) in new[]
        {
            ("justify-content", "JustifyContent", JustifyContentMap), ("align-items", "AlignItems", AlignItemsMap),
            ("align-self", "AlignSelf", AlignSelfMap), ("align-content", "AlignContent", AlignContentMap),
            ("justify-items", "JustifyItems", JustifyItemsMap), ("justify-self", "JustifySelf", JustifySelfMap),
        })
        {
            AddShorthand(p, css, new[] { cs }, (t, v, c) => t.Assign(cs, t.Align(c, C + cs, map)));
        }

        AddShorthand(p, "place-items", new[] { "AlignItems", "JustifyItems" }, (t, v, c) => t.Place(c, "AlignItems", AlignItemsMap, "JustifyItems", JustifyItemsMap));
        AddShorthand(p, "place-self", new[] { "AlignSelf", "JustifySelf" }, (t, v, c) => t.Place(c, "AlignSelf", AlignSelfMap, "JustifySelf", JustifySelfMap));
        AddShorthand(p, "place-content", new[] { "AlignContent", "JustifyContent" }, (t, v, c) => t.Place(c, "AlignContent", AlignContentMap, "JustifyContent", JustifyContentMap));

        // Background
        AddColor(p, "background-color", "BackgroundColor");
        AddShorthand(p, "background-image", new[] { "BackgroundImage" }, (t, v, c) => t.Assign("BackgroundImage", t.BackgroundImage(c)));
        AddShorthand(p, "background-repeat", new[] { "BackgroundRepeat" }, (t, v, c) => t.Assign("BackgroundRepeat", t.BackgroundRepeat(c)));
        AddShorthand(p, "background-position", new[] { "BackgroundPosition" }, (t, v, c) => t.Assign("BackgroundPosition", t.BackgroundPosition(c)));
        AddShorthand(p, "background-size", new[] { "BackgroundSize" }, (t, v, c) => t.Assign("BackgroundSize", t.BackgroundSize(c)));
        AddShorthand(p, "background", new[] { "BackgroundColor", "BackgroundImage", "BackgroundRepeat", "BackgroundPosition", "BackgroundSize" }, (t, v, c) => t.Background(v));

        // Shorthands
        AddShorthand(p, "padding", Sides("Padding"), (t, v, c) => t.Box4(c, Sides("Padding"), x => t.Length(x, LengthOptions.Percent)));
        AddShorthand(p, "margin", Sides("Margin"), (t, v, c) => t.Box4(c, Sides("Margin"), x => t.Length(x, LengthOptions.Offset)));
        AddShorthand(p, "inset", s_sides, (t, v, c) => t.Box4(c, s_sides, x => t.Length(x, LengthOptions.Offset)));
        AddShorthand(p, "border-width", Sides("Border", "Width"), (t, v, c) => t.Box4(c, Sides("Border", "Width"), x => t.Length(x, LengthOptions.BorderWidth)));
        AddShorthand(p, "border-color", Sides("Border", "Color"), (t, v, c) => t.Box4(c, Sides("Border", "Color"), t.Color));
        AddShorthand(p, "border-style", Sides("Border", "Style"), (t, v, c) => t.Box4(c, Sides("Border", "Style"), x => t.Enum(x, C + "BorderStyle", BorderStyleMap)));
        foreach (var (css, cs) in new[] { ("padding", "Padding"), ("margin", "Margin"), ("inset", "Inset") })
        {
            var options = cs == "Padding" ? LengthOptions.Percent : LengthOptions.Offset;
            AddShorthand(p, css + "-inline", new[] { cs + "InlineStart", cs + "InlineEnd" }, (t, v, c) => t.Pair(c, cs + "InlineStart", cs + "InlineEnd", x => t.Length(x, options)));
            AddShorthand(p, css + "-block", new[] { cs + "BlockStart", cs + "BlockEnd" }, (t, v, c) => t.Pair(c, cs + "BlockStart", cs + "BlockEnd", x => t.Length(x, options)));
        }

        var radii = new[] { "BorderTopLeftRadius", "BorderTopRightRadius", "BorderBottomRightRadius", "BorderBottomLeftRadius" };
        AddShorthand(p, "border-radius", radii, (t, v, c) => t.BorderRadius(c, radii));
        var allBorders = new List<string>();
        foreach (var side in s_sides) allBorders.AddRange(new[] { $"Border{side}Width", $"Border{side}Style", $"Border{side}Color" });
        AddShorthand(p, "border", allBorders.ToArray(), (t, v, c) => t.BorderSides(c, s_sides));
        foreach (var side in s_sides)
            AddShorthand(p, "border-" + side.ToLowerInvariant(), new[] { $"Border{side}Width", $"Border{side}Style", $"Border{side}Color" }, (t, v, c) => t.BorderSides(c, new[] { side }));
        foreach (var (css, sides) in new[]
        {
            ("border-inline-start", new[] { "InlineStart" }), ("border-inline-end", new[] { "InlineEnd" }),
            ("border-block-start", new[] { "BlockStart" }), ("border-block-end", new[] { "BlockEnd" }),
            ("border-inline", new[] { "InlineStart", "InlineEnd" }), ("border-block", new[] { "BlockStart", "BlockEnd" }),
        })
        {
            var targets = new List<string>();
            foreach (var side in sides) targets.AddRange(new[] { $"Border{side}Width", $"Border{side}Style", $"Border{side}Color" });
            AddShorthand(p, css, targets.ToArray(), (t, v, c) => t.BorderSides(c, sides));
        }

        AddShorthand(p, "outline", new[] { "OutlineWidth", "OutlineStyle", "OutlineColor" }, (t, v, c) => t.Outline(c));
        AddShorthand(p, "flex", new[] { "FlexGrow", "FlexShrink", "FlexBasis" }, (t, v, c) => t.Flex(c));
        AddShorthand(p, "flex-flow", new[] { "FlexDirection", "FlexWrap" }, (t, v, c) => t.FlexFlow(c));
        AddShorthand(p, "gap", new[] { "RowGap", "ColumnGap" }, (t, v, c) => t.Gap(c));
        AddShorthand(p, "grid-gap", new[] { "RowGap", "ColumnGap" }, (t, v, c) => t.Gap(c));
        AddShorthand(p, "overflow", new[] { "OverflowX", "OverflowY" }, (t, v, c) => t.Pair(c, "OverflowX", "OverflowY", x => t.Enum(x, C + "Overflow", OverflowMap)));
        AddShorthand(p, "font", new[] { "FontStyle", "FontWeight", "FontSize", "LineHeight", "FontFamily" }, (t, v, c) => t.Font(v));

        AddEffectProperties(p);
        AddGridProperties(p);
        return p;
    }
}
