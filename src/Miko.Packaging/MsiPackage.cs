using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using OpenMcdf;

namespace Miko.Packaging;

internal static class MsiPackage
{
    public static void Write(DesktopPackage package, string path)
    {
        if (package.Entries.Any(e => e.IsLink))
            throw new InvalidDataException("Windows Installer requires regular published files. Resolve symbolic links before packaging.");
        var parts = package.Version.Split('-', '+')[0].Split('.');
        if (parts.Length > 4 || !parts.All(p => ushort.TryParse(p, out _)) ||
            int.Parse(parts[0]) > 255 || (parts.Length > 1 && int.Parse(parts[1]) > 255))
            throw new ArgumentException($"Version '{package.Version}' exceeds Windows Installer's major.minor.build limits.");
        string version = string.Join('.', parts.Concat(["0", "0"]).Take(3));
        using var root = RootStorage.Create(path);
        root.CLSID = new Guid("000c1084-0000-0000-c000-000000000046");
        var db = new MsiDatabase(root);
        var properties = db.Define("Property", "Property:s72* Value:l0");
        string identity = $"{package.Author}/{package.Id}/{package.Rid}";
        properties.Add("ProductName", package.Name);
        properties.Add("ProductVersion", version);
        properties.Add("ProductCode", StableId(identity + "/" + package.Version));
        properties.Add("UpgradeCode", StableId(identity));
        properties.Add("Manufacturer", package.Author);
        properties.Add("ProductLanguage", "1033");
        properties.Add("ALLUSERS", "1");

        var dirs = db.Define("Directory", "Directory:s72* Directory_Parent:S72 DefaultDir:l255");
        dirs.Add("TARGETDIR", null, "SourceDir");
        dirs.Add("ProgramFiles64Folder", "TARGETDIR", ".");
        dirs.Add("INSTALLDIR", "ProgramFiles64Folder", FileName(package.Name, 0));
        var components = db.Define("Component", "Component:s72* ComponentId:S38 Directory_:s72 Attributes:i2 Condition:S255 KeyPath:S72");
        var files = db.Define("File", "File:s72* Component_:s72 FileName:l255 FileSize:i4 Version:S72 Language:S20 Attributes:I2 Sequence:i4");
        var media = db.Define("Media", "DiskId:i2* LastSequence:i4 DiskPrompt:L64 Cabinet:S255 VolumeLabel:S32 Source:S72");
        var feature = db.Define("Feature", "Feature:s38* Feature_Parent:S38 Title:L64 Description:L255 Display:I2 Level:i2 Directory_:S72 Attributes:i2");
        feature.Add("Main", null, null, null, 1, 1, "INSTALLDIR", 0);
        var featureComponents = db.Define("FeatureComponents", "Feature_:s38* Component_:s72*");
        var directories = new Dictionary<string, string> { [""] = "INSTALLDIR" };
        var cab = new List<(string Id, PackageEntry File)>();
        int serial = 0;
        var createFolder = db.Define("CreateFolder", "Directory_:s72* Component_:s72*");
        foreach (var entry in package.Entries)
        {
            serial++;
            var slash = entry.Name.LastIndexOf('/');
            var parent = directories[slash < 0 ? "" : entry.Name[..slash]];
            var name = FileName(entry.Name[(slash + 1)..], serial);
            var component = "C" + serial;
            var id = "F" + serial;
            if (entry.IsDirectory)
            {
                parent = "D" + serial;
                dirs.Add(parent, directories[slash < 0 ? "" : entry.Name[..slash]], name);
                directories.Add(entry.Name, parent);
                createFolder.Add(parent, component);
            }
            else
            {
                cab.Add((id, entry));
                files.Add(id, component, name, checked((int)entry.Size), null, null, 512, cab.Count);
            }
            featureComponents.Add("Main", component);
            components.Add(component, StableId(identity + "/" + entry.Name), parent, 256, null, entry.IsDirectory ? null : id);
        }
        media.Add(1, cab.Count, null, "#payload.cab", null, null);
        const string sequenceSchema = "Action:s72* Condition:S255 Sequence:I2";
        var execute = db.Define("InstallExecuteSequence", sequenceSchema);
        (string Action, int Sequence)[] steps = [("CostInitialize", 800), ("FileCost", 900), ("CostFinalize", 1000),
            ("InstallValidate", 1400), ("InstallInitialize", 1500), ("ProcessComponents", 1600), ("UnpublishFeatures", 1800),
            ("RemoveFiles", 3500), ("RemoveFolders", 3600), ("CreateFolders", 3700), ("InstallFiles", 4000),
            ("RegisterUser", 6000), ("RegisterProduct", 6100), ("PublishFeatures", 6300), ("PublishProduct", 6400), ("InstallFinalize", 6600)];
        foreach (var step in steps) execute.Add(step.Action, null, step.Sequence);
        // A full-UI launch delegates to the execution sequence. Administrative installs
        // (msiexec /a) extract a network image without registering the product on the machine.
        var ui = db.Define("InstallUISequence", sequenceSchema);
        var adminUi = db.Define("AdminUISequence", sequenceSchema);
        var admin = db.Define("AdminExecuteSequence", sequenceSchema);
        foreach (var (action, sequence) in steps)
        {
            if (sequence <= 1000)
            {
                ui.Add(action, null, sequence);
                adminUi.Add(action, null, sequence);
            }
            if (sequence <= 1500 || action is "InstallFiles" or "InstallFinalize")
                admin.Add(action, null, sequence);
        }
        ui.Add("ExecuteAction", null, 1300);
        adminUi.Add("ExecuteAction", null, 1300);
        admin.Add("InstallAdminPackage", null, 3900);
        db.Save();
        using (var cabinet = root.CreateStream(MsiDatabase.StreamName("payload.cab", false))) Cabinet(cabinet, cab);
        using var summary = root.CreateStream("\u0005SummaryInformation");
        Summary(summary, package);
    }

    private static string FileName(string name, int serial)
    {
        if (name.Length > 255 || name.Any(c => c < ' ' || "<>:\"/\\|?*".Contains(c)) || name.EndsWith('.') || name.EndsWith(' '))
            throw new ArgumentException($"Invalid Windows Installer filename '{name}'.");
        return $"M{serial:X7}|{name}";
    }

    private static string StableId(string value) =>
        new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(value)).AsSpan(0, 16)).ToString("B").ToUpperInvariant();

    // Single-folder cabinet, with up to 32 KiB of contiguous file content per uncompressed CFDATA record.
    private static void Cabinet(Stream stream, List<(string Id, PackageEntry File)> files)
    {
        using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
        long size = files.Sum(e => e.File.Size);
        long blocks = (size + 32767) / 32768;
        long dataOffset = 44 + files.Sum(e => 17L + e.Id.Length);
        writer.Write("MSCF"u8); writer.Write(0); writer.Write(checked((uint)(dataOffset + size + blocks * 8)));
        writer.Write(0); writer.Write(44); writer.Write(0);
        writer.Write((byte)3); writer.Write((byte)1); writer.Write((ushort)1); writer.Write(checked((ushort)files.Count));
        writer.Write((ushort)0); writer.Write((ushort)0); writer.Write((ushort)0);
        writer.Write(checked((uint)dataOffset)); writer.Write(checked((ushort)blocks)); writer.Write((ushort)0);
        long offset = 0;
        foreach (var (id, file) in files)
        {
            writer.Write(checked((uint)file.Size)); writer.Write(checked((uint)offset)); writer.Write((ushort)0);
            writer.Write((ushort)0x2821); writer.Write((ushort)0); writer.Write((ushort)32);
            stream.CString(id); offset += file.Size;
        }
        byte[] buffer = new byte[32768];
        int used = 0;
        void FlushBlock()
        {
            uint checksum = (uint)used | ((uint)used << 16);
            int i = 0;
            for (; i + 4 <= used; i += 4)
                checksum ^= BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(i));
            uint tail = 0;
            for (; i < used; i++) tail = (tail << 8) | buffer[i];
            writer.Write(checksum ^ tail); writer.Write((ushort)used); writer.Write((ushort)used); writer.Write(buffer, 0, used);
            used = 0;
        }
        foreach (var (_, file) in files)
        {
            using var input = file.Open();
            int read;
            while ((read = input.Read(buffer, used, buffer.Length - used)) != 0)
            {
                used += read;
                if (used == buffer.Length) FlushBlock();
            }
        }
        if (used > 0) FlushBlock();
    }

    // MS-OLEPS SummaryInformation property set, required by Windows Installer.
    private static void Summary(Stream output, DesktopPackage package)
    {
        var values = new List<(int Id, byte[] Bytes)>();
        void Property(int id, int type, Action<BinaryWriter> write) => values.Add((id, NetworkOrder.Bytes(s =>
        {
            using var w = new BinaryWriter(s, Encoding.UTF8, true);
            w.Write(type); write(w); Archives.Pad(s, s.Position, 4);
        })));
        void Text(int id, string text) => Property(id, 30, w =>
        {
            var bytes = Encoding.UTF8.GetBytes(text + "\0");
            w.Write(bytes.Length); w.Write(bytes);
        });
        Property(1, 2, w => w.Write((ushort)65001));
        Text(2, "Installation Database"); Text(3, package.Name); Text(4, package.Author);
        Text(7, package.Rid == "win-arm64" ? "Arm64;1033" : "x64;1033");
        Text(9, Guid.NewGuid().ToString("B").ToUpperInvariant());
        Property(14, 3, w => w.Write(500)); Property(15, 3, w => w.Write(2)); Text(18, "Miko.Packaging");
        using var writer = new BinaryWriter(output, Encoding.UTF8, true);
        writer.Write((ushort)0xfffe); writer.Write((ushort)0); writer.Write(0x00020006); writer.Write(new byte[16]);
        writer.Write(1); writer.Write(new Guid("f29f85e0-4ff9-1068-ab91-08002b27b3d9").ToByteArray()); writer.Write(48);
        int offset = 8 + values.Count * 8;
        writer.Write(offset + values.Sum(p => p.Bytes.Length)); writer.Write(values.Count);
        foreach (var (id, bytes) in values) { writer.Write(id); writer.Write(offset); offset += bytes.Length; }
        foreach (var (_, bytes) in values) writer.Write(bytes);
    }
}
