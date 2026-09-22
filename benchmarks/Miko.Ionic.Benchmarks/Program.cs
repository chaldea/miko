using BenchmarkDotNet.Running;
using Miko.Core;
using Miko.Ionic.Benchmarks.Helpers;
using Miko.Ionic.Benchmarks.Pages;

// "--census" prints the shape of the benchmark page (element count, tag census, rule count) so it
// can be compared against the real application page it stands in for. A benchmark that has
// silently drifted away from the app's shape reports numbers nobody should act on (ISSUE-145).
if (args.Contains("--census"))
{
    using var harness = IonicAppHarness.Create<CatalogHomePage>();
    var page = harness.Build<CatalogHomePage>();
    var all = new List<Element>();
    Collect(page, all);

    Console.WriteLine($"elements = {all.Count}");
    Console.WriteLine($"rules    = {harness.RuleCount}");
    foreach (var group in all.GroupBy(element => element.TagName).OrderByDescending(group => group.Count()))
        Console.WriteLine($"  {group.Key,-12} {group.Count()}");
    return;

    static void Collect(Element element, List<Element> into)
    {
        into.Add(element);
        foreach (var child in element.Children) Collect(child, into);
    }
}

// "--stages" reports the same style/layout/paint split the on-device frame probe reports, so a
// benchmark number can be lined up against a device measurement without converting between them.
if (args.Contains("--stages"))
{
    StageBreakdown.Run();
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

/// <summary>Entry point marker for <see cref="BenchmarkSwitcher.FromAssembly"/>.</summary>
public partial class Program;
