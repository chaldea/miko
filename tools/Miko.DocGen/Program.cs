using Miko.DocGen;

// Regenerates the generated regions of docs/ and agent/ (see DocsGenerator).
//
//   dotnet run --project tools/Miko.DocGen              update the docs in place
//   dotnet run --project tools/Miko.DocGen -- --check   verify only; exit 1 on drift or problems
//   … -- --only docs/components/ionic/button.md         write just the named file(s)
//
// Building this project runs the update automatically (outside CI); see Miko.DocGen.csproj.

var check = args.Contains("--check");
var repoIndex = Array.IndexOf(args, "--repo");
var repoRoot = repoIndex >= 0 && repoIndex + 1 < args.Length ? args[repoIndex + 1] : FindRepoRoot();

var only = args.Select((arg, i) => (arg, i)).Where(a => a.arg == "--only" && a.i + 1 < args.Length)
    .Select(a => args[a.i + 1].Replace('\\', '/')).ToList();

var result = DocsGenerator.Run(repoRoot, write: !check, only.Count > 0 ? only : null);

foreach (var file in result.StaleFiles)
{
    if (check)
        Console.WriteLine($"{file}: error MIKODOC01: generated docs are out of date; run `dotnet run --project tools/Miko.DocGen`.");
    else if (only.Count == 0 || only.Contains(file))
        Console.WriteLine($"Updated {file}");
}

// Problems surface as MSBuild warnings during a local build (the Exec task recognises the
// canonical format) so a half-documented work in progress still builds; --check fails on them.
var severity = check ? "error" : "warning";
foreach (var problem in result.Problems)
{
    var colon = problem.IndexOf(": ", StringComparison.Ordinal);
    var location = colon > 0 && !problem[..colon].Contains(' ') ? problem[..colon] : "docs";
    var message = colon > 0 && location != "docs" ? problem[(colon + 2)..] : problem;
    Console.WriteLine($"{location}: {severity} MIKODOC02: {message}");
}

if (!check && result.StaleFiles.Count == 0)
    Console.WriteLine("Docs are up to date.");

return check && !result.IsClean ? 1 : 0;

static string FindRepoRoot()
{
    foreach (var start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "miko.slnx")))
                return directory.FullName;
        }
    }
    throw new InvalidOperationException("Could not find the repository root (miko.slnx); pass --repo <path>.");
}
