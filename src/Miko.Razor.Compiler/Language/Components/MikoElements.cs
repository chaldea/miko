// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Components;

/// <summary>
/// Compile-time knowledge of Miko's DOM element types and their attribute slots (ISSUE-136).
///
/// <para>Before this table existed, generated code called
/// <c>__builder.OpenElement(seq, "div")</c> and <c>__builder.AddAttribute(seq, "class", value)</c>,
/// leaving the runtime to look the tag up in a dictionary and route the attribute name through a
/// ~70-branch switch with a type test on every arm. Both inputs are string literals in the Razor
/// source, so all of that work can happen here instead: the writer emits the concrete element type
/// and a direct property assignment.</para>
///
/// <para>Kept deliberately in sync with <c>Miko.Components.RenderTreeBuilder</c>'s tag map and
/// attribute switch — those remain the fallback path for
/// <c>AddMarkupContent</c>, whose tags and attribute names are only known at runtime.</para>
/// </summary>
internal static class MikoElements
{
    private const string DomNamespace = "global::Miko.Core.DomElements.";
    private const string BuilderTypeName = "global::" + ComponentsApi.RenderTreeBuilder.FullTypeName;

    /// <summary>How a value expression is written onto the element instance.</summary>
    internal enum SlotKind
    {
        /// <summary><c>element.Prop = value;</c></summary>
        Property,

        /// <summary><c>element.Prop = RenderTreeBuilder.ParseInputType(value);</c> and friends.</summary>
        Converted,

        /// <summary><c>element.OnX = RenderTreeBuilder.ToHandler(callback);</c></summary>
        EventHandler,

        /// <summary>Needs a helper call for its HTML semantics, e.g. the ISSUE-121 value rules.</summary>
        Helper,
    }

    internal readonly struct Slot
    {
        public Slot(string propertyName, SlotKind kind, string converter = null, string helper = null)
        {
            PropertyName = propertyName;
            Kind = kind;
            Converter = converter;
            Helper = helper;
        }

        /// <summary>Property on the element type, or the handler slot for events.</summary>
        public string PropertyName { get; }

        public SlotKind Kind { get; }

        /// <summary>Fully-qualified conversion helper for <see cref="SlotKind.Converted"/>.</summary>
        public string Converter { get; }

        /// <summary>Fully-qualified static helper for <see cref="SlotKind.Helper"/>.</summary>
        public string Helper { get; }
    }

    /// <summary>
    /// Tag name → element type. Mirrors <c>RenderTreeBuilder._tagMap</c>; a tag missing here
    /// simply keeps using the legacy string-based emit, so the two can never disagree in a way
    /// that changes behaviour.
    /// </summary>
    private static readonly Dictionary<string, string> s_tagTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = DomNamespace + "AnchorElement",
        ["div"] = DomNamespace + "DivElement",
        ["span"] = DomNamespace + "SpanElement",
        ["p"] = DomNamespace + "ParagraphElement",
        ["button"] = DomNamespace + "ButtonElement",
        ["input"] = DomNamespace + "InputElement",
        ["textarea"] = DomNamespace + "TextAreaElement",
        ["select"] = DomNamespace + "SelectElement",
        ["option"] = DomNamespace + "OptionElement",
        ["optgroup"] = DomNamespace + "OptGroupElement",
        ["label"] = DomNamespace + "LabelElement",
        ["br"] = DomNamespace + "BrElement",
        ["hr"] = DomNamespace + "HrElement",
        ["h1"] = DomNamespace + "H1Element",
        ["h2"] = DomNamespace + "H2Element",
        ["h3"] = DomNamespace + "H3Element",
        ["h4"] = DomNamespace + "H4Element",
        ["h5"] = DomNamespace + "H5Element",
        ["h6"] = DomNamespace + "H6Element",
        ["ul"] = DomNamespace + "UlElement",
        ["ol"] = DomNamespace + "OlElement",
        ["li"] = DomNamespace + "LiElement",
        ["img"] = DomNamespace + "ImageElement",
        ["video"] = DomNamespace + "VideoElement",
        ["table"] = DomNamespace + "TableElement",
        ["caption"] = DomNamespace + "CaptionElement",
        ["colgroup"] = DomNamespace + "ColgroupElement",
        ["col"] = DomNamespace + "ColElement",
        ["thead"] = DomNamespace + "TheadElement",
        ["tbody"] = DomNamespace + "TbodyElement",
        ["tfoot"] = DomNamespace + "TfootElement",
        ["tr"] = DomNamespace + "TrElement",
        ["th"] = DomNamespace + "ThElement",
        ["td"] = DomNamespace + "TdElement",
        ["nav"] = DomNamespace + "NavElement",
        ["strong"] = DomNamespace + "StrongElement",
        ["b"] = DomNamespace + "BElement",
        ["pre"] = DomNamespace + "PreElement",
        ["code"] = DomNamespace + "CodeElement",
    };

    /// <summary>
    /// Attributes available on every element (declared on <c>Element</c> itself).
    /// </summary>
    private static readonly Dictionary<string, Slot> s_commonSlots = new(StringComparer.Ordinal)
    {
        ["class"] = new Slot("Class", SlotKind.Property),
        ["id"] = new Slot("Id", SlotKind.Property),
        // `style` normally carries a Miko.Styling.Style object (style="@SomeStyle"), but a few
        // components pass a CSS string. The legacy switch matched only Style and silently ignored
        // everything else; SetStyle's overloads reproduce that rather than breaking the build.
        // Both casings appear in the wild, matching the legacy switch.
        ["style"] = new Slot("Style", SlotKind.Helper, helper: BuilderTypeName + ".SetStyle"),
        ["Style"] = new Slot("Style", SlotKind.Helper, helper: BuilderTypeName + ".SetStyle"),

        ["onclick"] = new Slot("OnClick", SlotKind.EventHandler),
        ["onmouseenter"] = new Slot("OnMouseEnter", SlotKind.EventHandler),
        ["onmouseleave"] = new Slot("OnMouseLeave", SlotKind.EventHandler),
        ["onmousedown"] = new Slot("OnMouseDown", SlotKind.EventHandler),
        ["onmouseup"] = new Slot("OnMouseUp", SlotKind.EventHandler),
        ["onmousemove"] = new Slot("OnMouseMove", SlotKind.EventHandler),
        ["onpointerdown"] = new Slot("OnPointerDown", SlotKind.EventHandler),
        ["onpointerup"] = new Slot("OnPointerUp", SlotKind.EventHandler),
        ["onpointermove"] = new Slot("OnPointerMove", SlotKind.EventHandler),
        ["onpointercancel"] = new Slot("OnPointerCancel", SlotKind.EventHandler),
        ["onlongpress"] = new Slot("OnLongPress", SlotKind.EventHandler),
        ["onfocus"] = new Slot("OnFocus", SlotKind.EventHandler),
        ["onblur"] = new Slot("OnBlur", SlotKind.EventHandler),
        ["onchange"] = new Slot("OnChange", SlotKind.EventHandler),
        ["onscroll"] = new Slot("OnScroll", SlotKind.EventHandler),
        ["onkeydown"] = new Slot("OnKeyDown", SlotKind.EventHandler),
        ["oninput"] = new Slot("OnInput", SlotKind.EventHandler),
    };

    /// <summary>
    /// Element-specific attributes, keyed by tag then attribute name. Only the tags whose
    /// attributes the legacy switch actually handled appear here — anything else keeps falling
    /// through to the legacy path (and is therefore still silently ignored, as before).
    /// </summary>
    private static readonly Dictionary<string, Dictionary<string, Slot>> s_tagSlots =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = new(StringComparer.Ordinal)
            {
                ["href"] = new Slot("Href", SlotKind.Property),
                ["target"] = new Slot("Target", SlotKind.Property),
                ["rel"] = new Slot("Rel", SlotKind.Property),
            },
            ["input"] = new(StringComparer.Ordinal)
            {
                ["type"] = new Slot("Type", SlotKind.Converted,
                    converter: BuilderTypeName + ".ParseInputType"),
                ["checked"] = new Slot("Checked", SlotKind.Converted,
                    converter: BuilderTypeName + ".ParseBooleanAttribute"),
                // Absent `value` must not clear user input — see SetInputValue (ISSUE-121).
                ["value"] = new Slot("Value", SlotKind.Helper,
                    helper: BuilderTypeName + ".SetInputValue"),
            },
            ["textarea"] = new(StringComparer.Ordinal)
            {
                ["placeholder"] = new Slot("Placeholder", SlotKind.Property),
                ["value"] = new Slot("Value", SlotKind.Helper,
                    helper: BuilderTypeName + ".SetTextAreaValue"),
                // rows/cols 是整数；HTML 属性值是字符串，解析失败时保留元素默认值
                // （与旧 switch 的 int.TryParse 语义一致）。
                ["rows"] = new Slot("Rows", SlotKind.Helper,
                    helper: BuilderTypeName + ".SetTextAreaRows"),
                ["cols"] = new Slot("Cols", SlotKind.Helper,
                    helper: BuilderTypeName + ".SetTextAreaCols"),
            },
            ["img"] = new(StringComparer.Ordinal)
            {
                ["src"] = new Slot("Source", SlotKind.Property),
                ["placeholder"] = new Slot("Placeholder", SlotKind.Property),
            },
            ["video"] = new(StringComparer.Ordinal)
            {
                ["src"] = new Slot("Source", SlotKind.Property),
                ["poster"] = new Slot("Poster", SlotKind.Property),
                ["autoplay"] = new Slot("AutoPlay", SlotKind.Converted,
                    converter: BuilderTypeName + ".ParseBooleanAttribute"),
                ["loop"] = new Slot("Loop", SlotKind.Converted,
                    converter: BuilderTypeName + ".ParseBooleanAttribute"),
                ["muted"] = new Slot("Muted", SlotKind.Converted,
                    converter: BuilderTypeName + ".ParseBooleanAttribute"),
                ["controls"] = new Slot("Controls", SlotKind.Converted,
                    converter: BuilderTypeName + ".ParseBooleanAttribute"),
            },
            ["code"] = new(StringComparer.Ordinal)
            {
                ["language"] = new Slot("Language", SlotKind.Property),
                ["highlight"] = new Slot("Highlight", SlotKind.Converted,
                    converter: BuilderTypeName + ".ParseBooleanAttribute"),
            },
        };

    /// <summary>
    /// The globally-qualified element type for a tag, or <c>null</c> when the tag is unknown
    /// (in which case the caller keeps the legacy string-based emit).
    /// </summary>
    public static string GetElementTypeName(string tagName)
        => tagName != null && s_tagTypes.TryGetValue(tagName, out var typeName) ? typeName : null;

    /// <summary>
    /// Resolves an attribute to a typed slot on the given tag's element type, or <c>null</c> when
    /// there is no such slot — the legacy path then handles it exactly as before, including
    /// silently dropping attributes the runtime never supported (<c>role</c>, <c>aria-*</c>, …).
    /// </summary>
    public static Slot? GetSlot(string tagName, string attributeName)
    {
        if (tagName == null || attributeName == null)
        {
            return null;
        }

        if (s_tagSlots.TryGetValue(tagName, out var tagSlots) &&
            tagSlots.TryGetValue(attributeName, out var tagSlot))
        {
            return tagSlot;
        }

        return s_commonSlots.TryGetValue(attributeName, out var commonSlot) ? commonSlot : null;
    }

    /// <summary>
    /// Whether the slot needs a value at all. Minimized HTML boolean attributes
    /// (<c>&lt;video autoplay&gt;</c>) mean "true"; the writer supplies that literal.
    /// </summary>
    public static bool IsBooleanConverted(in Slot slot)
        => slot.Kind == SlotKind.Converted
        && slot.Converter == BuilderTypeName + ".ParseBooleanAttribute";

    /// <summary>
    /// Folds a literal attribute value into the constant the converter would produce, so
    /// <c>type="checkbox"</c> becomes <c>InputType.Checkbox</c> rather than a runtime
    /// <c>ToLowerInvariant()</c> plus switch. Mirrors
    /// <c>RenderTreeBuilder.ParseInputType</c>/<c>ParseBooleanAttribute</c> exactly — if the two
    /// ever diverge, the non-literal path is still the single source of truth.
    /// </summary>
    public static bool TryFoldConvertedLiteral(in Slot slot, string literal, out string folded)
    {
        if (IsBooleanConverted(slot))
        {
            // HTML 布尔属性：仅显式的 "false" 为假，其余（含空串与属性名本身）为真。
            folded = string.Equals(literal, "false", StringComparison.OrdinalIgnoreCase)
                ? "false"
                : "true";
            return true;
        }

        if (slot.Converter == BuilderTypeName + ".ParseInputType")
        {
            folded = "global::Miko.Common.InputType." + InputTypeMember(literal);
            return true;
        }

        folded = null;
        return false;
    }

    private static string InputTypeMember(string literal) => literal?.ToLowerInvariant() switch
    {
        "checkbox" => "Checkbox",
        "radio" => "Radio",
        "password" => "Password",
        "range" => "Range",
        "search" => "Search",
        // number/tel ask the platform for a numeric keypad; both edit as plain text.
        "number" or "tel" => "Number",
        _ => "Text",
    };
}
