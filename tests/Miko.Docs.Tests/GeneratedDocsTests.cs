using Miko.DocGen;
using Shouldly;

namespace Miko.Docs.Tests;

/// <summary>
/// The docs' generated regions (parameter tables, theme tokens, inlined examples, indexes) must
/// match the current code, and the docs must pass their consistency checks.
/// <para>
/// Locally, building tools/Miko.DocGen regenerates the docs, so these pass after any build. On
/// CI the regeneration is skipped on purpose: a component change committed without its docs
/// fails here instead of being silently fixed up in a throwaway checkout.
/// </para>
/// </summary>
public class GeneratedDocsTests
{
    private static readonly Lazy<DocsRunResult> Result = new(() => DocsGenerator.Run(RepoPaths.Root, write: false));

    [Fact]
    public void Should_have_up_to_date_generated_regions()
    {
        Result.Value.StaleFiles.ShouldBeEmpty(
            "Generated docs are out of date. Run `dotnet run --project tools/Miko.DocGen` (or build the solution) and commit the result.");
    }

    [Fact]
    public void Should_pass_docs_consistency_checks()
    {
        Result.Value.Problems.ShouldBeEmpty();
    }

    /// <summary>
    /// 1.0 rule for the Ionic public surface: every public parameter takes effect. A summary that
    /// says "kept for API parity", "has no effect" and the like documents a parameter that does
    /// not, so it fails here — implement the parameter or remove it.
    /// </summary>
    [Fact]
    public void Should_not_expose_ionic_parameters_documented_as_inert()
    {
        DocsChecks.InertParameters(new ComponentCatalog(RepoPaths.Root)).ShouldBeEmpty();
    }
}
