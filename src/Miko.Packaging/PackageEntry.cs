using System.Text;

namespace Miko.Packaging;

// Paths are relative to the root of the archive and always use '/'.
internal sealed record PackageEntry(string Name, int Mode, long Size, string? Source = null, byte[]? Bytes = null)
{
    public bool IsDirectory => (Mode & 0xf000) == 0x4000;
    public bool IsLink => (Mode & 0xf000) == 0xa000;
    public string LinkTarget => Encoding.UTF8.GetString(Bytes!);
    public Stream Open() => Source is null ? new MemoryStream(Bytes ?? [], false) : File.OpenRead(Source);

    public static PackageEntry Directory(string name) => new(name, 0x41ed, 0);
    public static PackageEntry TextFile(string name, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return new(name, 0x81a4, bytes.Length, Bytes: bytes);
    }

    public static IEnumerable<PackageEntry> Read(DirectoryInfo root, string appName, string prefix = "")
    {
        foreach (var item in root.EnumerateFileSystemInfos().OrderBy(i => i.Name, StringComparer.Ordinal))
        {
            var name = prefix + item.Name;
            if (item.LinkTarget is string target)
            {
                var bytes = Encoding.UTF8.GetBytes(target);
                yield return new(name, 0xa1ff, bytes.Length, Bytes: bytes);
            }
            else if (item is DirectoryInfo dir)
            {
                yield return Directory(name);
                foreach (var entry in Read(dir, appName, name + "/")) yield return entry;
            }
            else
            {
                int permissions = OperatingSystem.IsWindows() ? 0x1a4 : (int)File.GetUnixFileMode(item.FullName) & 0x1ff;
                if (name == appName) permissions |= 0x49; // Cross-publishing from Windows has no Unix execute bits.
                yield return new(name, 0x8000 | permissions, ((FileInfo)item).Length, item.FullName);
            }
        }
    }
}
