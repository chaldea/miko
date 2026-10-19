namespace Miko.Docs.Tests;

internal static class RepoPaths
{
    /// <summary>The repository root (the directory holding miko.slnx).</summary>
    public static string Root { get; } = FindRoot();

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "miko.slnx")))
                return directory.FullName;
        }
        throw new InvalidOperationException("Tests must run from inside the repository.");
    }
}
