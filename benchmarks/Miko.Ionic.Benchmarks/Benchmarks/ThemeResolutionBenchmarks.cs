using BenchmarkDotNet.Attributes;
using Miko.Ionic;
using Miko.Ionic.Components;

namespace Miko.Ionic.Benchmarks.Benchmarks;

/// <summary>
/// ISSUE-145: the cost of resolving a theme, which every Ionic component does on every build.
///
/// <para>Resolving means constructing a complete mode theme from scratch — several hundred token
/// assignments across ~37 token groups — and then copying the application's specified values over
/// it. One resolution allocates roughly 38 KB. Doing that once per component instance made it the
/// single largest term in the build stage: on a page with 63 Ionic components it was ~2.3 MB and
/// ~1.0 ms per build, and the build stage is what a route navigation pays.</para>
///
/// <para>Components now share one resolved theme per (application theme, mode, cascading theme),
/// so the cost below is paid once per configuration rather than once per component. These
/// benchmarks keep both sides visible: what one resolution costs, and what a page's worth of
/// component builds costs now that they share.</para>
/// </summary>
[MemoryDiagnoser]
public class ThemeResolutionBenchmarks
{
    /// <summary>
    /// Building a complete mode theme — the work a resolution wraps. Shown on its own so a change
    /// in the theme's size is visible even if the sharing above keeps total build cost flat.
    /// </summary>
    [Benchmark(Description = "Build one complete Material Design theme")]
    public IonicTheme CreateCompleteTheme() => IonicTheme.CreateMd();

    // The other half of a resolution — the style key, a SHA-256 over every token the component
    // depends on — is not benchmarked directly: GetStyleKey is internal to Miko.Ionic, and
    // widening a shipped library's surface for a benchmark is the wrong trade. It is exercised
    // through the page-level build benchmark in AppPageFrameBenchmarks instead.

    [GlobalSetup]
    public void Setup() => IonicComponentBase.InvalidateResolvedThemes();
}
