using System.Text;
using Microsoft.AspNetCore.Razor.Language.Components.Css;
using Shouldly;
using static Miko.Razor.Compiler.Tests.CssTestHelpers;

namespace Miko.Razor.Compiler.Tests;

/// <summary>
/// ISSUE-161: every property the translator supports has a sample here, and every sample's C#
/// compiles against Miko and builds a Style. A property added to the table without a sample fails.
/// </summary>
public class CssCoverageTests
{
    private static readonly Dictionary<string, string> s_samples = new()
    {
        ["width"] = "10px", ["height"] = "50%", ["inline-size"] = "auto", ["block-size"] = "fit-content",
        ["min-width"] = "auto", ["min-height"] = "10px", ["min-inline-size"] = "0", ["min-block-size"] = "1em",
        ["max-width"] = "none", ["max-height"] = "100vh", ["max-inline-size"] = "50%", ["max-block-size"] = "min-content",
        ["padding"] = "1px 2px", ["margin"] = "0 auto", ["inset"] = "0", ["border-width"] = "thin medium",
        ["border-color"] = "red blue", ["border-style"] = "solid dashed", ["border-radius"] = "4px 50%",
        ["border"] = "1px solid red", ["outline"] = "auto", ["flex"] = "1 0 auto", ["flex-flow"] = "row wrap",
        ["gap"] = "4px", ["grid-gap"] = "1px 2px", ["overflow"] = "auto", ["font"] = "bold 12px/1 serif",
        ["background"] = "red", ["text-decoration"] = "underline", ["text-decoration-line"] = "line-through",
        ["place-items"] = "center", ["place-self"] = "start end", ["place-content"] = "space-between",
        ["outline-width"] = "2px", ["outline-offset"] = "-1px", ["outline-color"] = "#000", ["outline-style"] = "dotted",
        ["font-size"] = "small", ["line-height"] = "1.5", ["letter-spacing"] = "normal", ["color"] = "currentcolor",
        ["caret-color"] = "blue", ["font-style"] = "oblique", ["text-align"] = "start", ["text-transform"] = "capitalize",
        ["white-space"] = "pre-wrap", ["overflow-wrap"] = "anywhere", ["word-wrap"] = "break-word",
        ["text-overflow"] = "clip", ["vertical-align"] = "middle", ["direction"] = "ltr", ["writing-mode"] = "vertical-rl",
        ["font-weight"] = "lighter", ["font-family"] = "\"Segoe UI\", sans-serif", ["content"] = "\"→\"",
        ["word-break"] = "break-word", ["display"] = "block flex", ["position"] = "absolute", ["box-sizing"] = "content-box",
        ["visibility"] = "collapse", ["overflow-x"] = "scroll", ["overflow-y"] = "clip", ["table-layout"] = "fixed",
        ["scrollbar-width"] = "thin", ["pointer-events"] = "auto", ["user-select"] = "text", ["cursor"] = "url(x.png), pointer",
        ["z-index"] = "auto", ["order"] = "-1", ["opacity"] = "50%", ["aspect-ratio"] = "auto 16 / 9",
        ["flex-direction"] = "column", ["flex-wrap"] = "wrap-reverse", ["flex-grow"] = "2", ["flex-shrink"] = "0",
        ["flex-basis"] = "content", ["row-gap"] = "1px", ["column-gap"] = "normal", ["grid-row-gap"] = "2px",
        ["grid-column-gap"] = "3px", ["justify-content"] = "end", ["align-items"] = "first baseline",
        ["align-self"] = "self-start", ["align-content"] = "stretch", ["justify-items"] = "legacy", ["justify-self"] = "center",
        ["background-color"] = "hsl(0 0% 50%)", ["background-image"] = "none", ["background-repeat"] = "repeat no-repeat",
        ["background-position"] = "right bottom", ["background-size"] = "cover",
        ["transform"] = "skew(10deg) matrix(1, 0, 0, 1, 0, 0)", ["transform-origin"] = "50% 0",
        ["box-shadow"] = "1px 2px 3px 4px red", ["transition"] = "all 1s", ["transition-property"] = "width, height",
        ["transition-duration"] = "1s, 2s", ["transition-delay"] = "0s", ["transition-timing-function"] = "steps(4, end)",
        ["animation"] = "none", ["animation-name"] = "none", ["animation-duration"] = "1s", ["animation-delay"] = "-1s",
        ["animation-timing-function"] = "ease", ["animation-iteration-count"] = "infinite", ["animation-direction"] = "alternate",
        ["animation-fill-mode"] = "both", ["animation-play-state"] = "paused",
        ["grid-template-columns"] = "100px repeat(2, 1fr)", ["grid-template-rows"] = "none", ["grid-auto-columns"] = "1fr",
        ["grid-auto-rows"] = "max-content", ["grid-template-areas"] = "\"a\"", ["grid-row-start"] = "span name",
        ["grid-row-end"] = "-1", ["grid-column-start"] = "a 2", ["grid-column-end"] = "auto", ["grid-row"] = "1 / 3",
        ["grid-column"] = "span 2", ["grid-area"] = "a / 1 / b", ["grid-template"] = "auto / 1fr 1fr", ["grid-auto-flow"] = "row",
    };

    private static IEnumerable<string> Supported()
    {
        foreach (var name in CssPropertyTranslator.SupportedProperties)
        {
            // Generated per side / corner: one sample per family is enough, but check them all.
            yield return name;
        }
    }

    private static string Sample(string property)
    {
        if (s_samples.TryGetValue(property, out var sample)) return sample;
        if (property.EndsWith("-color", StringComparison.Ordinal)) return "red";
        if (property.EndsWith("-style", StringComparison.Ordinal)) return "solid";
        if (property.EndsWith("-radius", StringComparison.Ordinal)) return "4px";
        if (property.StartsWith("border", StringComparison.Ordinal) && property.EndsWith("-width", StringComparison.Ordinal)) return "1px";
        if (property.StartsWith("border-", StringComparison.Ordinal)) return "1px solid red";
        if (property.StartsWith("padding", StringComparison.Ordinal) || property.StartsWith("margin", StringComparison.Ordinal)
            || property.StartsWith("inset", StringComparison.Ordinal) || property is "top" or "right" or "bottom" or "left")
            return "2px";
        throw new ShouldAssertException($"No sample for the supported property '{property}'. Add one to CssCoverageTests.");
    }

    [Fact]
    public void EverySupportedProperty_CompilesAndBuilds()
    {
        var members = new StringBuilder();
        var names = new List<string>();
        foreach (var property in Supported())
        {
            var css = $"{property}: {Sample(property)}";
            var (block, diagnostics) = Declarations(css);
            diagnostics.Where(d => d.Kind != CssDiagnosticKind.Approximated)
                .ShouldBeEmpty($"'{css}': {string.Join("; ", diagnostics.Select(d => d.Message))}");
            var method = "P" + names.Count;
            names.Add(css);
            members.Append($"    public static global::Miko.Styling.Style {method}() => {block.ToInitializer("global::Miko.Styling.Style", "        ")};\n");
        }

        var type = Compile(members.ToString());
        for (var i = 0; i < names.Count; i++)
        {
            Should.NotThrow(() => type.GetMethod("P" + i)!.Invoke(null, null), names[i]);
        }
    }
}
