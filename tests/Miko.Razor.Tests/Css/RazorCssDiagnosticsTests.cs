using Shouldly;

namespace Miko.Razor.Tests.Css;

/// <summary>
/// ISSUE-161: CSS that cannot be translated is reported at compile time, at its position in the
/// .razor or .razor.css file. Read from this project's SARIF log; the fixture is
/// <see cref="UnsupportedCssFixture"/> (+ its .razor.css).
/// </summary>
public class RazorCssDiagnosticsTests
{
    private const string Fixture = "UnsupportedCssFixture.razor";

    [Fact]
    public void UnsupportedProperty_InStyleAttribute_IsReportedOnItsLine()
    {
        RazorTestFiles.Diagnostics("RZ10102", Fixture).ShouldContain(d => d.Line == 5 && d.Message.Contains("'float'"));
    }

    [Fact]
    public void UnsupportedProperty_InRazorCss_IsReportedInTheCssFile()
    {
        RazorTestFiles.Diagnostics("RZ10102", "UnsupportedCssFixture.razor.css")
            .ShouldContain(d => d.Line == 2 && d.Message.Contains("'text-shadow'"));
    }

    [Fact]
    public void ExpressionWhereOnlyKeywordsAreAllowed_IsReported()
    {
        RazorTestFiles.Diagnostics("RZ10107", Fixture).ShouldContain(d => d.Line == 6 && d.Message.Contains("display"));
    }

    [Fact]
    public void LengthExpressionWithoutUnit_IsReported()
    {
        RazorTestFiles.Diagnostics("RZ10107", Fixture).ShouldContain(d => d.Line == 6 && d.Message.Contains("needs a unit"));
    }

    [Fact]
    public void UnsupportedSelector_IsReported()
    {
        RazorTestFiles.Diagnostics("RZ10104", Fixture).ShouldContain(d => d.Line == 8 && d.Message.Contains(":visited"));
    }

    [Fact]
    public void Important_IsReportedAsApproximated()
    {
        RazorTestFiles.Diagnostics("RZ10108", Fixture).ShouldContain(d => d.Line == 9 && d.Message.Contains("!important"));
    }

    [Fact]
    public void UnsupportedUnit_IsReported()
    {
        RazorTestFiles.Diagnostics("RZ10103", Fixture).ShouldContain(d => d.Line == 10 && d.Message.Contains("vmin"));
    }

    [Fact]
    public void UnsupportedAtRule_IsReported()
    {
        RazorTestFiles.Diagnostics("RZ10105", Fixture).ShouldContain(d => d.Line == 11 && d.Message.Contains("@font-face"));
    }

    [Fact]
    public void InvalidCss_IsReported()
    {
        RazorTestFiles.Diagnostics("RZ10106", Fixture).ShouldContain(d => d.Line == 12 && d.Message.Contains("'color' has no value"));
    }

    [Theory]
    [InlineData("InlineCssFixture.razor")]
    [InlineData("StyleElementFixture.razor")]
    [InlineData("ScopedCssFixture.razor")]
    [InlineData("ScopedCssFixture.razor.css")]
    [InlineData("ScopedCssChild.razor")]
    public void SupportedCss_ProducesNoDiagnostics(string file)
    {
        foreach (var id in new[] { "RZ10100", "RZ10101", "RZ10102", "RZ10103", "RZ10104", "RZ10105", "RZ10106", "RZ10107", "RZ10108" })
        {
            RazorTestFiles.Diagnostics(id, file).ShouldBeEmpty($"{id} in {file}");
        }
    }

    [Fact]
    public void StyleElement_IsNoLongerAnUnknownTag()
    {
        RazorTestFiles.Diagnostics("RZ10101", Fixture).ShouldNotContain(d => d.Message.Contains("<style>"));
    }
}
