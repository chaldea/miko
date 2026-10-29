using System.Text.Json;
using Shouldly;

namespace Miko.Razor.Tests.Css;

/// <summary>The generated code and the compiler diagnostics of this test project's build.</summary>
internal static class RazorTestFiles
{
    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "miko.slnx")))
            dir = dir.Parent;

        dir.ShouldNotBeNull("Could not locate the repository root (miko.slnx).");
        return dir!.FullName;
    }

    public static string GeneratedCode(string fixtureName)
    {
        var generated = Path.Combine(RepoRoot(), "tests", "Miko.Razor.Tests", "obj", "GeneratedFiles");
        var file = Directory.EnumerateFiles(generated, $"*{fixtureName}_razor.g.cs", SearchOption.AllDirectories)
            .FirstOrDefault();
        file.ShouldNotBeNull($"No generated file for {fixtureName} under {generated}.");
        return File.ReadAllText(file!);
    }

    /// <summary>(line, message) of every <paramref name="ruleId"/> diagnostic in <paramref name="fileName"/>, 1-based lines.</summary>
    public static List<(int Line, string Message)> Diagnostics(string ruleId, string fileName)
    {
        var sarif = Path.Combine(RepoRoot(), "tests", "Miko.Razor.Tests", "obj", "razor-diagnostics.sarif");
        File.Exists(sarif).ShouldBeTrue($"No SARIF log at {sarif}. The project's ErrorLog setting must stay enabled.");

        using var document = JsonDocument.Parse(File.ReadAllText(sarif));
        var messages = new List<(int, string)>();
        foreach (var run in document.RootElement.GetProperty("runs").EnumerateArray())
        {
            if (!run.TryGetProperty("results", out var results)) continue;
            foreach (var result in results.EnumerateArray())
            {
                if (result.GetProperty("ruleId").GetString() != ruleId) continue;
                if (!result.TryGetProperty("locations", out var locations) || locations.GetArrayLength() == 0) continue;
                var physical = locations[0].GetProperty("physicalLocation");
                var uri = physical.GetProperty("artifactLocation").GetProperty("uri").GetString() ?? string.Empty;
                if (!uri.EndsWith("/" + fileName, StringComparison.Ordinal)) continue;
                var line = physical.TryGetProperty("region", out var region) && region.TryGetProperty("startLine", out var start) ? start.GetInt32() : 0;
                messages.Add((line, result.GetProperty("message").GetProperty("text").GetString() ?? string.Empty));
            }
        }

        return messages;
    }
}
