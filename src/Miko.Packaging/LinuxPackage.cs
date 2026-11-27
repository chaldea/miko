using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Miko.Packaging;

internal static class LinuxPackage
{
    public static PackageEntry[] Payload(DesktopPackage package)
    {
        if (!Regex.IsMatch(package.Id, @"\A[a-z0-9][a-z0-9.+-]+\z"))
            throw new ArgumentException($"Invalid Linux package id '{package.Id}'. Set MikoPackageId to a lowercase package name.");
        var prefix = "./opt/" + package.Id;
        return [PackageEntry.Directory(prefix), .. package.Entries.Select(e => e with { Name = prefix + "/" + e.Name })];
    }

    public static void Deb(DesktopPackage package, string path, string work)
    {
        var payload = Payload(package);
        var architecture = package.Rid == "linux-arm64" ? "arm64" : "amd64";
        var description = string.Join("\n ", package.Description.Trim().ReplaceLineEndings("\n").Split('\n').Select(s => s.Length == 0 ? "." : s));
        var control = $"Package: {package.Id}\nVersion: {package.Version.Replace('-', '~')}\nArchitecture: {architecture}\n" +
            $"Maintainer: {package.Author.ReplaceLineEndings(" ")}\nDescription: {description}\n";
        if (package.DebDepends.Length > 0) control += $"Depends: {package.DebDepends.ReplaceLineEndings(" ")}\n";
        string metadata = Path.Combine(work, "control.tar.gz"), data = Path.Combine(work, "data.tar.gz");
        Archives.TarGz(metadata, [PackageEntry.TextFile("./control", control)]);
        Archives.TarGz(data, payload);
        using var output = File.Create(path);
        output.Write("!<arch>\n"u8);
        using var version = new MemoryStream("2.0\n"u8.ToArray());
        Member("debian-binary", version);
        using var controlStream = File.OpenRead(metadata);
        Member("control.tar.gz", controlStream);
        using var dataStream = File.OpenRead(data);
        Member("data.tar.gz", dataStream);

        void Member(string name, Stream contents)
        {
            var header = string.Create(CultureInfo.InvariantCulture, $"{name,-16}{0,-12}{0,-6}{0,-6}{"100644",-8}{contents.Length,-10}`\n");
            if (header.Length != 60) throw new InvalidDataException("Debian ar member is too large.");
            output.Write(Encoding.ASCII.GetBytes(header));
            contents.CopyTo(output);
            if ((contents.Length & 1) != 0) output.WriteByte((byte)'\n');
        }
    }
}
