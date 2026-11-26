using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Miko.Packaging;

internal static class MacPackage
{
    public static void Write(DesktopPackage package, string path, string work)
    {
        string identifier = Regex.Replace(package.Id, "[^A-Za-z0-9.-]", "-");
        if (identifier.Length == 0) throw new ArgumentException("Set MikoPackageId to a valid bundle identifier.");
        if (!identifier.Contains('.')) identifier = "app." + identifier;
        string contents = "./" + package.Name + ".app/Contents";
        string macOS = contents + "/MacOS";
        var files = new List<PackageEntry> { PackageEntry.Directory("."), PackageEntry.Directory("./" + package.Name + ".app"),
            PackageEntry.Directory(contents), PackageEntry.Directory(macOS) };
        string executable = package.Name;
        if (!package.Entries.Any(e => e.Name == executable && !e.IsDirectory))
        {
            if (!package.Entries.Any(e => e.Name == package.Name + ".dll"))
                throw new InvalidDataException($"Cannot find published apphost '{package.Name}' or its DLL.");
            executable = "miko-launch";
            string dll = package.Name.Replace("'", "'\\''") + ".dll";
            files.Add(PackageEntry.TextFile(macOS + "/" + executable, $"#!/bin/sh\ncd \"$(dirname \"$0\")\" || exit 1\nexec dotnet './{dll}' \"$@\"\n") with { Mode = 0x81ed });
        }
        var plist = Plist(new XElement("dict", Pair("CFBundlePackageType", "APPL"), Pair("CFBundleIdentifier", identifier),
            Pair("CFBundleName", package.Name), Pair("CFBundleExecutable", executable), Pair("CFBundleVersion", package.Version.Split('-', '+')[0]),
            Pair("CFBundleShortVersionString", package.Version.Split('-', '+')[0]), Pair("CFBundleInfoDictionaryVersion", "6.0")));
        files.Add(PackageEntry.TextFile(contents + "/Info.plist", plist));
        files.AddRange(package.Entries.Select(f => f with { Name = macOS + "/" + f.Name }));

        var payload = files.ToArray();
        string payloadPath = Path.Combine(work, "Payload"), bomPath = Path.Combine(work, "Bom"), infoPath = Path.Combine(work, "PackageInfo");
        WritePayload(payloadPath, payload);
        BillOfMaterials.Write(bomPath, payload);
        var info = new XElement("pkg-info", new XAttribute("format-version", 2), new XAttribute("identifier", identifier),
            new XAttribute("version", package.Version), new XAttribute("install-location", "/Applications"),
            new XAttribute("auth", "root"), new XAttribute("relocatable", "false"),
            new XElement("payload", new XAttribute("numberOfFiles", files.Count), new XAttribute("installKBytes", (files.Sum(f => f.Size) + 1023) / 1024)));
        info.Save(infoPath);
        Xar(path, [infoPath, bomPath, payloadPath]);
    }

    private static IEnumerable<XElement> Pair(string key, string value) => [new("key", key), new("string", value)];
    internal static string Plist(XElement root) => new XDocument(
        new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
        new XElement("plist", new XAttribute("version", "1.0"), root)).ToString();

    // Installer's pax reader expects the portable ASCII (odc) cpio header with octal fields.
    private static void WritePayload(string path, PackageEntry[] files)
    {
        using var output = File.Create(path);
        using var gzip = new GZipStream(output, CompressionLevel.Optimal);
        long written = 0;
        int inode = 0;
        foreach (var entry in files.Append(PackageEntry.TextFile("TRAILER!!!", "")))
        {
            var name = Encoding.UTF8.GetBytes(entry.Name + "\0");
            var header = "070707000000" + Octal(inode++, 6) + Octal(entry.Mode, 6) +
                "00000000000000000100000000000000000" + Octal(name.Length, 6) + Octal(entry.Size, 11);
            gzip.Write(Encoding.ASCII.GetBytes(header));
            gzip.Write(name);
            using var input = entry.Open();
            input.CopyTo(gzip);
            written += header.Length + name.Length + entry.Size;
        }
        Archives.Pad(gzip, written, 512);
    }

    private static string Octal(long value, int width)
    {
        string digits = Convert.ToString(value, 8);
        if (digits.Length > width) throw new InvalidDataException("Package exceeds the odc payload size limits.");
        return digits.PadLeft(width, '0');
    }

    private static void Xar(string path, string[] files)
    {
        var toc = new XElement("toc", new XElement("checksum", new XAttribute("style", "sha1"), new XElement("offset", 0), new XElement("size", 20)));
        long offset = 20;
        for (int i = 0; i < files.Length; i++)
        {
            using var input = File.OpenRead(files[i]);
            var digest = Convert.ToHexStringLower(SHA1.HashData(input));
            toc.Add(new XElement("file", new XAttribute("id", i + 1), new XElement("name", Path.GetFileName(files[i])),
                new XElement("type", "file"), new XElement("mode", "0644"), new XElement("data",
                    new XElement("offset", offset), new XElement("length", input.Length), new XElement("size", input.Length),
                    new XElement("encoding", new XAttribute("style", "application/octet-stream")),
                    new XElement("archived-checksum", new XAttribute("style", "sha1"), digest),
                    new XElement("extracted-checksum", new XAttribute("style", "sha1"), digest))));
            offset += input.Length;
        }
        var xml = Encoding.UTF8.GetBytes(new XElement("xar", toc).ToString());
        byte[] compressed = NetworkOrder.Bytes(s => { using var z = new ZLibStream(s, CompressionLevel.Optimal, true); z.Write(xml); });
        using var output = File.Create(path);
        output.Write("xar!"u8); output.U16(28); output.U16(1);
        output.U64(compressed.Length); output.U64(xml.Length); output.U32(1);
        output.Write(compressed); output.Write(SHA1.HashData(compressed));
        foreach (var file in files) { using var input = File.OpenRead(file); input.CopyTo(output); }
    }
}
