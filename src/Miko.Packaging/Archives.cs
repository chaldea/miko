using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Miko.Packaging;

internal static class Archives
{
    public static void Zip(string path, IReadOnlyList<PackageEntry> entries)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var file in entries)
        {
            var entry = archive.CreateEntry(file.IsDirectory ? file.Name + "/" : file.Name);
            entry.ExternalAttributes = file.Mode << 16;
            using var output = entry.Open();
            using var input = file.Open();
            input.CopyTo(output);
        }
    }

    public static void TarGz(string path, IEnumerable<PackageEntry> entries)
    {
        using var output = File.Create(path);
        using var gzip = new GZipStream(output, CompressionLevel.Optimal);
        // dpkg's tar reader supports GNU long names. It does not accept pax extension headers.
        using var tar = new TarWriter(gzip, TarEntryFormat.Gnu);
        foreach (var file in entries)
        {
            var type = file.IsDirectory ? TarEntryType.Directory : file.IsLink ? TarEntryType.SymbolicLink : TarEntryType.RegularFile;
            var entry = new GnuTarEntry(type, file.Name) { Mode = (UnixFileMode)(file.Mode & 0x1ff), Uid = 0, Gid = 0 };
            using var input = file.Open();
            if (file.IsLink) entry.LinkName = file.LinkTarget;
            if (type == TarEntryType.RegularFile) entry.DataStream = input;
            tar.WriteEntry(entry);
        }
    }

    // SVR4 newc records align both the name and the data to a four-byte boundary.
    public static void CpioGz(string path, IReadOnlyList<PackageEntry> entries)
    {
        using var output = File.Create(path);
        using var gzip = new GZipStream(output, CompressionLevel.Optimal);
        uint inode = 1;
        foreach (var entry in entries.Append(PackageEntry.TextFile("TRAILER!!!", "")))
        {
            var name = Encoding.UTF8.GetBytes(entry.Name + "\0");
            uint[] fields = [inode++, (uint)entry.Mode, 0, 0, 1, 0, checked((uint)entry.Size), 0, 0, 0, 0, (uint)name.Length, 0];
            gzip.Write("070701"u8);
            foreach (var field in fields) gzip.Write(Encoding.ASCII.GetBytes(field.ToString("x8", CultureInfo.InvariantCulture)));
            gzip.Write(name);
            Pad(gzip, 110L + name.Length, 4);
            using var input = entry.Open();
            input.CopyTo(gzip);
            Pad(gzip, entry.Size, 4);
        }
    }

    public static void Pad(Stream stream, long size, int alignment)
    {
        int padding = (int)((alignment - size % alignment) % alignment);
        if (padding > 0) stream.Write(new byte[padding]);
    }
}
