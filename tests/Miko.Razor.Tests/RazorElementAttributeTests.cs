using System.Text.Json;
using Miko.Common;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Events;
using Miko.Testing;
using Shouldly;

namespace Miko.Razor.Tests;

/// <summary>
/// ISSUE-153 problem 3: element attributes outside the compiler's element table compiled, had no
/// effect, and produced no warning. The common ones now have slots
/// (<see cref="ElementAttributeSlotsFixture"/>); the rest are reported at compile time
/// (<see cref="UnsupportedMarkupFixture"/>).
/// </summary>
public class RazorElementAttributeTests : IDisposable
{
    private readonly TestContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    private T Single<T>(string className) where T : Element
        => _ctx.Render<ElementAttributeSlotsFixture>().FindByClass(className).ShouldHaveSingleItem().ShouldBeOfType<T>();

    [Fact]
    public void InputPlaceholder_AndLabelFor_ReachTheElements()
    {
        Single<InputElement>("slot-input").Placeholder.ShouldBe("Your name");
        Single<LabelElement>("slot-label").For.ShouldBe("name-input");
    }

    [Fact]
    public void Disabled_MarksTheElementDisabled_AndFalseLeavesItEnabled()
    {
        Single<InputElement>("slot-input").IsDisabled.ShouldBeTrue();
        Single<InputElement>("slot-enabled").IsDisabled.ShouldBeFalse();
    }

    [Fact]
    public void RangeMinMax_AcceptLiteralsAndExpressions()
    {
        var range = Single<InputElement>("slot-range");
        range.Min.ShouldBe(10f);
        range.Max.ShouldBe(50f);
        range.NumericValue.ShouldBe(20f);
    }

    [Fact]
    public void ReadonlyAndMaxlength_ReachTheTextControls()
    {
        var input = Single<InputElement>("slot-limits");
        input.ReadOnly.ShouldBeTrue();
        input.MaxLength.ShouldBe(5);
        input.IsEditable.ShouldBeFalse();

        var area = Single<TextAreaElement>("slot-area-limits");
        area.ReadOnly.ShouldBeFalse();
        area.MaxLength.ShouldBe(50);

        area.InsertText(new string('x', 60));
        area.Value!.Length.ShouldBe(50, "maxlength cuts off inserted text");
    }

    [Fact]
    public void OptionValue_OptgroupLabel_AndSelected_ReachTheElements()
    {
        var cut = _ctx.Render<ElementAttributeSlotsFixture>();
        var options = cut.FindByTagName("option").Cast<OptionElement>().ToList();
        options.Select(o => o.Value).ShouldBe(["a", "2", "c"]);
        options[2].Selected.ShouldBeTrue();
        cut.FindByTagName("optgroup").Cast<OptGroupElement>().Single().Label.ShouldBe("Letters");
    }

    [Fact]
    public void SelectBind_ShowsTheBoundValue_AndWritesBackOnChange()
    {
        ElementAttributeSlotsFixture.Choice = "2";
        var cut = _ctx.Render<ElementAttributeSlotsFixture>();
        var select = cut.FindByClass("slot-select").Single().ShouldBeOfType<SelectElement>();

        // Bound to "2": the numeric option value, applied once the options exist.
        select.Value.ShouldBe("2");
        select.GetDisplayText().ShouldBe("Beta");

        select.SelectOption(0).ShouldBeTrue();
        select.OnChange!.Invoke(new ChangeEventArgs { Target = select });
        ElementAttributeSlotsFixture.Choice.ShouldBe("a");
    }

    [Fact]
    public void TableSpans_FollowHtmlRules()
    {
        var td = Single<TdElement>("slot-td");
        td.ColSpan.ShouldBe(2);
        td.RowSpan.ShouldBe(3);
        Single<ThElement>("slot-th").ColSpan.ShouldBe(1, "an invalid span is 1, as in HTML");
    }

    [Fact]
    public void ListStart_AndItemValue_ReachTheElements()
    {
        Single<OlElement>("slot-ol").Start.ShouldBe(5);
        Single<LiElement>("slot-li").Value.ShouldBe(9);
    }

    [Fact]
    public void Canvas_IsAKnownTag()
    {
        // Used to throw "Unknown tag: canvas" when rendered.
        Single<CanvasElement>("slot-canvas");
    }

    [Fact]
    public void NewSlots_AreEmittedAsTypedAssignments()
    {
        // The runtime's name switch knows these attributes too (it backs AddMarkupContent), so the
        // behaviour tests above cannot tell the two paths apart — assert on the emitted code.
        var code = GeneratedCode("ElementAttributeSlotsFixture");
        code.ShouldContain(".Placeholder = global::Miko.Components.RenderTreeBuilder.FormatAttributeValue(\"Your name\")");
        code.ShouldContain(".For = global::Miko.Components.RenderTreeBuilder.FormatAttributeValue(\"name-input\")");
        code.ShouldContain("global::Miko.Components.RenderTreeBuilder.SetDisabled(");
        code.ShouldContain("global::Miko.Components.RenderTreeBuilder.SetSelectValue(");
        code.ShouldContain(".ColSpan = global::Miko.Components.RenderTreeBuilder.ParseSpanAttribute(\"2\")");
        code.ShouldContain("OpenElement<global::Miko.Core.DomElements.CanvasElement>()");
        code.ShouldNotContain("\"placeholder\",");
        code.ShouldNotContain("\"colspan\",");
    }

    private static string GeneratedCode(string fixtureName)
    {
        var generated = Path.Combine(FindRepoRoot(), "tests", "Miko.Razor.Tests", "obj", "GeneratedFiles");
        var file = Directory.EnumerateFiles(generated, $"{fixtureName}_razor.g.cs", SearchOption.AllDirectories)
            .FirstOrDefault();
        file.ShouldNotBeNull($"No generated file for {fixtureName} under {generated}.");
        return File.ReadAllText(file!);
    }

    // -----------------------------------------------------------------
    // Compile-time diagnostics, read from the SARIF error log of this project's build.
    // -----------------------------------------------------------------

    [Fact]
    public void UnsupportedAttribute_IsReported()
    {
        var messages = Diagnostics("RZ10100", "UnsupportedMarkupFixture.razor");
        messages.ShouldContain(m => m.Contains("'tabindex'") && m.Contains("<input>"));
    }

    [Fact]
    public void AccessibilityAndDataAttributes_AreNotReported()
    {
        var messages = Diagnostics("RZ10100", "UnsupportedMarkupFixture.razor");
        messages.ShouldNotContain(m => m.Contains("'aria-label'"));
        messages.ShouldNotContain(m => m.Contains("'data-test'"));
        messages.ShouldNotContain(m => m.Contains("'role'"));
    }

    [Fact]
    public void UnknownLowercaseTag_IsReported()
    {
        Diagnostics("RZ10101", "UnsupportedMarkupFixture.razor")
            .ShouldContain(m => m.Contains("<section>"));
    }

    [Fact]
    public void SupportedMarkup_ProducesNoElementDiagnostics()
    {
        Diagnostics("RZ10100", "ElementAttributeSlotsFixture.razor").ShouldBeEmpty();
        Diagnostics("RZ10101", "ElementAttributeSlotsFixture.razor").ShouldBeEmpty();
    }

    private static List<string> Diagnostics(string ruleId, string fileName)
    {
        var sarif = Path.Combine(FindRepoRoot(), "tests", "Miko.Razor.Tests", "obj", "razor-diagnostics.sarif");
        File.Exists(sarif).ShouldBeTrue($"No SARIF log at {sarif}. The project's ErrorLog setting must stay enabled.");

        using var document = JsonDocument.Parse(File.ReadAllText(sarif));
        var messages = new List<string>();
        foreach (var run in document.RootElement.GetProperty("runs").EnumerateArray())
        {
            if (!run.TryGetProperty("results", out var results)) continue;
            foreach (var result in results.EnumerateArray())
            {
                if (result.GetProperty("ruleId").GetString() != ruleId) continue;
                var uri = result.GetProperty("locations")[0].GetProperty("physicalLocation")
                    .GetProperty("artifactLocation").GetProperty("uri").GetString() ?? string.Empty;
                if (!uri.EndsWith(fileName, StringComparison.Ordinal)) continue;
                messages.Add(result.GetProperty("message").GetProperty("text").GetString() ?? string.Empty);
            }
        }
        return messages;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "miko.slnx")))
            dir = dir.Parent;

        dir.ShouldNotBeNull("Could not locate the repository root (miko.slnx).");
        return dir!.FullName;
    }
}
