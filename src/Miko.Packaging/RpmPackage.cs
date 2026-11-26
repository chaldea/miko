using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Miko.Packaging;

internal static class RpmPackage
{
    private static string Hex(byte[] data) => Convert.ToHexStringLower(data);

    public static void Write(DesktopPackage package, string path, string work)
    {
        var files = LinuxPackage.Payload(package).OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();
        var payload = Path.Combine(work, "rpm.cpio.gz");
        Archives.CpioGz(payload, files);
        var architecture = package.Rid == "linux-arm64" ? "aarch64" : "x86_64";
        var version = package.Version.Replace('-', '~');
        var size = files.Sum(f => f.Size);
        var header = new Header();
        header.Strings(100, ["C"]); // language table for i18n strings
        header.String(1000, package.Id);
        header.String(1001, version);
        header.String(1002, "1");
        header.Strings(1004, [package.Name], 9);
        header.Strings(1005, [package.Description], 9);
        header.Ints(1006, [DateTimeOffset.UtcNow.ToUnixTimeSeconds()]);
        header.String(1007, "localhost");
        header.Ints(1009, [size]);
        header.String(1014, package.License);
        header.Strings(1016, ["Applications"], 9);
        header.String(1021, "linux");
        header.String(1022, architecture);

        header.Ints(1028, files.Select(f => f.Size));
        header.Shorts(1030, files.Select(f => f.Mode));
        header.Shorts(1033, files.Select(_ => 0));
        header.Ints(1034, files.Select(_ => 0L));
        header.Strings(1035, files.Select(f =>
        {
            if (f.IsDirectory || f.IsLink) return "";
            using var data = f.Open();
            return Hex(SHA256.HashData(data));
        }));
        header.Strings(1036, files.Select(f => f.IsLink ? f.LinkTarget : ""));
        var zeroes = new long[files.Length];
        header.Ints(1037, zeroes);
        header.Strings(1039, files.Select(_ => "root"));
        header.Strings(1040, files.Select(_ => "root"));
        header.String(1044, $"{package.Id}-{version}-1.src.rpm");
        header.Ints(1045, files.Select(_ => (long)uint.MaxValue));
        header.Strings(1047, [package.Id]);
        if (package.RpmRequires.Length > 0)
        {
            var requirements = package.RpmRequires.Select(ParseRequirement).ToArray();
            header.Ints(1048, requirements.Select(r => r.Flags));
            header.Strings(1049, requirements.Select(r => r.Name));
            header.Strings(1050, requirements.Select(r => r.Version));
        }
        header.Ints(1095, files.Select(_ => 1L));
        header.Ints(1096, files.Select((_, i) => i + 1L));
        header.Strings(1097, files.Select(_ => ""));
        header.Ints(1112, [8]); // RPMSENSE_EQUAL
        header.Strings(1113, [version + "-1"]);
        var directories = files.Select(f => f.Name[1..(f.Name.LastIndexOf('/') + 1)]).Distinct().ToArray();
        var indices = directories.Select((d, i) => (d, i)).ToDictionary(pair => pair.d, pair => (long)pair.i);
        header.Ints(1116, files.Select(f => indices[f.Name[1..(f.Name.LastIndexOf('/') + 1)]]));
        header.Strings(1117, files.Select(f => Path.GetFileName(f.Name)));
        header.Strings(1118, directories);
        header.String(1124, "cpio");
        header.String(1125, "gzip");
        header.String(1126, "6");
        header.Ints(5011, [8]); // SHA-256 file digests
        using var archive = File.OpenRead(payload);
        header.Strings(5092, [Hex(SHA256.HashData(archive))]);
        header.Ints(5093, [8]);
        var metadata = header.Encode(63);

        var signature = new Header();
        signature.Ints(1000, [metadata.Length + archive.Length]);
        signature.String(269, Hex(SHA1.HashData(metadata)));
        signature.String(273, Hex(SHA256.HashData(metadata)));
        using (var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5))
        {
            md5.AppendData(metadata);
            archive.Position = 0;
            byte[] buffer = new byte[81920];
            int read;
            while ((read = archive.Read(buffer)) > 0) md5.AppendData(buffer, 0, read);
            signature.Binary(1004, md5.GetHashAndReset());
        }

        using var output = File.Create(path);
        output.Write(new byte[] { 0xed, 0xab, 0xee, 0xdb, 3, 0 });
        output.U16(0);
        output.U16(architecture == "aarch64" ? 19 : 1);
        var name = Encoding.ASCII.GetBytes(package.Id + "-" + version + "-1");
        var leadName = new byte[66];
        name.AsSpan(0, Math.Min(name.Length, 65)).CopyTo(leadName);
        output.Write(leadName);
        output.U16(1);
        output.U16(5);
        output.Write(new byte[16]);
        output.Write(signature.Encode(62));
        Archives.Pad(output, output.Position, 8);
        output.Write(metadata);
        archive.Position = 0;
        archive.CopyTo(output);
    }

    private static (string Name, long Flags, string Version) ParseRequirement(string expression)
    {
        var match = Regex.Match(expression.Trim(), @"^([^\s<>=]+)(?:\s*(<=|>=|=|<|>)\s*([^\s<>=]+))?$");
        if (!match.Success) throw new ArgumentException($"Invalid RPM requirement '{expression}'. Expected a name and optional version comparison.");
        long flags = match.Groups[2].Value switch { "<" => 2, ">" => 4, "=" => 8, "<=" => 10, ">=" => 12, _ => 0 };
        return (match.Groups[1].Value, flags, match.Groups[3].Value);
    }

    // A v4 immutable region ends in a copy of its index entry with a negative offset
    // spanning the complete index. Tag offsets in the other entries are relative to the data store.
    private sealed class Header
    {
        private sealed record Field(int Type, int Count, int Alignment, byte[] Data);
        private readonly SortedDictionary<int, Field> fields = [];
        public void String(int tag, string value) => Strings(tag, [value], 6);
        public void Strings(int tag, IEnumerable<string> values, int type = 8)
        {
            var items = values.ToArray();
            fields.Add(tag, new(type, items.Length, 1, NetworkOrder.Bytes(s => { foreach (var v in items) s.CString(v); })));
        }
        public void Ints(int tag, IEnumerable<long> values)
        {
            var items = values.ToArray();
            fields.Add(tag, new(4, items.Length, 4, NetworkOrder.Bytes(s => { foreach (var v in items) s.U32(v); })));
        }
        public void Shorts(int tag, IEnumerable<int> values)
        {
            var items = values.ToArray();
            fields.Add(tag, new(3, items.Length, 2, NetworkOrder.Bytes(s => { foreach (var v in items) s.U16(v); })));
        }
        public void Binary(int tag, byte[] bytes) => fields.Add(tag, new(7, bytes.Length, 1, bytes));

        public byte[] Encode(int region)
        {
            using var data = new MemoryStream();
            using var index = new MemoryStream();
            void Entry(Stream stream, int tag, int type, long offset, int count)
            {
                stream.U32(tag);
                stream.U32(type);
                stream.U32(offset);
                stream.U32(count);
            }
            foreach (var (tag, field) in fields)
            {
                Archives.Pad(data, data.Length, field.Alignment);
                Entry(index, tag, field.Type, data.Length, field.Count);
                data.Write(field.Data);
            }
            var end = data.Length;
            int count = fields.Count + 1;
            Entry(data, region, 7, unchecked((uint)(-16 * count)), 16);
            return NetworkOrder.Bytes(s =>
            {
                s.Write(new byte[] { 0x8e, 0xad, 0xe8, 1, 0, 0, 0, 0 });
                s.U32(count);
                s.U32(data.Length);
                Entry(s, region, 7, end, 16);
                index.WriteTo(s);
                data.WriteTo(s);
            });
        }
    }
}
