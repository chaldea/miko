using System.Security;

namespace Miko.TemplateE2E;

/// <summary>Everything one run writes: the package feed, the template hive, a private NuGet
/// packages folder, one directory per scaffolded app and one log per step.</summary>
sealed class WorkDirectory
{
    private WorkDirectory(string root)
    {
        Root = root;
        foreach (var directory in new[] { Feed, Hive, Packages, Apps, Logs })
            Directory.CreateDirectory(directory);
    }

    public string Root { get; }
    public string Feed => Path.Combine(Root, "feed");
    public string Hive => Path.Combine(Root, "hive");
    public string Packages => Path.Combine(Root, "packages");
    public string Apps => Path.Combine(Root, "apps");
    public string Logs => Path.Combine(Root, "logs");

    /// <summary>Uses <paramref name="path"/>, which must be empty or missing, or a new temp directory.</summary>
    public static WorkDirectory Create(string? path)
    {
        string root;
        if (path is null)
        {
            root = Directory.CreateTempSubdirectory("miko-template-e2e-").FullName;
        }
        else
        {
            // A reused directory would scaffold over old apps and restore stale packages of the same version.
            if (Directory.Exists(path) && Directory.EnumerateFileSystemEntries(path).Any())
                throw new UsageException($"The work directory {path} is not empty.");
            root = Directory.CreateDirectory(path).FullName;
        }

        // Resolve symlinks (macOS: /tmp and the temp directory under /var/folders both are). Building
        // through a symlinked path makes the Razor SDK compute TargetPath without the folder, so a page
        // under Pages/ without an @namespace lands in the root namespace and the `full` layout fails.
        return new WorkDirectory(RealPath(root));
    }

    // "miko-multiplatform/full/Android" → apps/multiplatform-full-Android. Short: an Android head's
    // obj/ paths are deep, and Windows still limits paths to 260 characters by default.
    public string AppDirectory(BuildJob job) => Path.Combine(Apps, Slug(job));

    public string LogFile(BuildJob job) => LogFile(Slug(job));

    public string LogFile(string step) => Path.Combine(Logs, step + ".log");

    /// <summary>Points the app at the private feed for Miko* and at a private packages folder: the
    /// user's global folder would hold earlier local packs under the same version.</summary>
    public void WriteNuGetConfig(string appDirectory) =>
        File.WriteAllText(Path.Combine(appDirectory, "nuget.config"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <config><add key="globalPackagesFolder" value="{SecurityElement.Escape(Packages)}" /></config>
              <packageSources>
                <add key="miko-e2e" value="{SecurityElement.Escape(Feed)}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <packageSourceMapping>
                <packageSource key="miko-e2e"><package pattern="Miko*" /></packageSource>
                <packageSource key="nuget.org"><package pattern="*" /></packageSource>
              </packageSourceMapping>
            </configuration>

            """);

    /// <summary>Deletes what it can: on Windows a build server may still hold a file open.</summary>
    public static void TryDelete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string Slug(BuildJob job) =>
        job.Label.Replace('/', '-') is var slug && slug.StartsWith("miko-", StringComparison.Ordinal) ? slug["miko-".Length..] : slug;

    // Path.GetFullPath keeps symlinks; resolve each component, as `pwd -P` does.
    private static string RealPath(string path)
    {
        var full = Path.GetFullPath(path);
        var real = Path.GetPathRoot(full)!;
        foreach (var segment in full[real.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            var next = Path.Combine(real, segment);
            real = Directory.ResolveLinkTarget(next, returnFinalTarget: true) is { } target ? RealPath(target.FullName) : next;
        }
        return real;
    }
}
