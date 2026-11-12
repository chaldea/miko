namespace Miko.Docs.Tests;

/// <summary>A disposable physical directory for generator tests, whose build paths reject links.</summary>
internal sealed class DocsTestDirectory : IDisposable
{
    public string Root { get; }

    public DocsTestDirectory()
    {
        // macOS temp paths start with /var, a symlink to /private/var. Resolve every
        // ancestor, not only the final directory, before creating an isolated fixture.
        var temporaryRoot = PhysicalPath(new DirectoryInfo(Path.GetTempPath()));
        Root = Path.Combine(temporaryRoot, "miko-docs-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    private static string PhysicalPath(DirectoryInfo directory)
    {
        // ResolveLinkTarget cannot inspect a Windows drive root.
        if (directory.Parent is not { } parent) return directory.FullName;
        if (directory.ResolveLinkTarget(returnFinalTarget: true) is DirectoryInfo target)
            return PhysicalPath(target);
        return Path.Combine(PhysicalPath(parent), directory.Name);
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
