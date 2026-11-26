using System.Text.RegularExpressions;

namespace Miko.Packaging;

internal sealed class DesktopPackage
{
    public string Name { get; }
    public string Id { get; }
    public string Version { get; }
    public string Rid { get; }
    public string License { get; }
    public string Author { get; }
    public string Description { get; }
    public string DebDepends { get; init; } = "";
    public string[] RpmRequires { get; init; } = [];
    public IReadOnlyList<PackageEntry> Entries { get; }
    private readonly string source;

    public DesktopPackage(string source, string name, string version, string rid, string author, string description,
        string packageId = "", string license = "")
    {
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." || name.Any(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c)))
            throw new ArgumentException($"Invalid desktop application name '{name}'.");
        if (!Regex.IsMatch(version, @"\A[0-9][0-9A-Za-z.+~-]*\z"))
            throw new ArgumentException($"Invalid package version '{version}'.");
        this.source = Path.GetFullPath(source);
        Name = name;
        Id = packageId.Length == 0 ? Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9.+-]+", "-").Trim('-', '.') : packageId;
        Version = version;
        Rid = rid;
        License = license.Length == 0 ? "Unspecified" : license;
        Author = string.IsNullOrWhiteSpace(author) ? name : author;
        Description = string.IsNullOrWhiteSpace(description) ? name : description;
        Entries = PackageEntry.Read(new DirectoryInfo(source), name).ToArray();
        if (!Entries.Any(e => !e.IsDirectory)) throw new InvalidDataException($"Publish directory '{source}' has no files.");
    }

    public static string[] Formats(string rid, string requested)
    {
        string[] allowed = rid switch
        {
            "win-x64" or "win-arm64" => ["msi", "zip"],
            "linux-x64" or "linux-arm64" => ["deb", "rpm", "tar.gz"],
            "osx-x64" or "osx-arm64" => ["dmg", "pkg"],
            _ => throw new ArgumentException($"Unsupported desktop runtime identifier '{rid}'.")
        };
        if (string.IsNullOrWhiteSpace(requested)) return [allowed[0]];
        var formats = requested.ToLowerInvariant().Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct().ToArray();
        if (formats.Length == 0 || formats.Any(f => !allowed.Contains(f)))
            throw new ArgumentException($"Invalid package formats '{requested}' for {rid}. Choose from {string.Join(", ", allowed)}.");
        return formats;
    }

    public string[] Build(string output, string requested)
    {
        var formats = Formats(Rid, requested);
        var relative = Path.GetRelativePath(source, output);
        if (!Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException("MikoPackageOutputDirectory must be outside the publish directory.");
        Directory.CreateDirectory(output);
        var work = Directory.CreateDirectory(Path.Combine(output, ".miko-" + Guid.NewGuid().ToString("N")));
        try
        {
            string Staged(string format) => Path.Combine(work.FullName, $"{Name}-{Version}-{Rid}.{format}");
            if (formats.Contains("pkg") || formats.Contains("dmg"))
                MacPackage.Write(this, Staged("pkg"), work.FullName);
            foreach (var format in formats)
            {
                var path = Staged(format);
                switch (format)
                {
                    case "zip": Archives.Zip(path, Entries); break;
                    case "tar.gz": Archives.TarGz(path, Entries); break;
                    case "deb": LinuxPackage.Deb(this, path, work.FullName); break;
                    case "rpm": RpmPackage.Write(this, path, work.FullName); break;
                    case "msi": MsiPackage.Write(this, path); break;
                    case "dmg": DiskImage.Write(path, Staged("pkg")); break;
                }
            }
            var outputs = formats.Select(f => Path.Combine(output, Path.GetFileName(Staged(f)))).ToArray();
            for (int i = 0; i < formats.Length; i++) File.Move(Staged(formats[i]), outputs[i], true);
            return outputs;
        }
        finally { work.Delete(true); }
    }
}
