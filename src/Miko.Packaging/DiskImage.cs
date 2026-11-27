using System.Buffers.Binary;
using System.Text;
using System.Xml.Linq;

namespace Miko.Packaging;

// A read-only UDIF image containing an ISO 9660 volume. The installer inside the volume
// carries the original app bundle, Unicode filenames, executable modes and symbolic links.
internal static class DiskImage
{
    private const int Sector = 2048;
    private const int RootSector = 20;
    private const int InstallerSector = 21;

    public static void Write(string path, string installer)
    {
        using var pkg = File.OpenRead(installer);
        using var output = File.Create(path);
        long sectors = InstallerSector + (pkg.Length + Sector - 1) / Sector;
        output.SetLength(sectors * Sector);
        WriteVolume(output, checked((uint)sectors), checked((uint)pkg.Length));
        output.Position = InstallerSector * Sector;
        pkg.CopyTo(output);
        output.Position = 0;
        uint crc = 0;
        byte[] buffer = new byte[81920];
        int read;
        while ((read = output.Read(buffer)) > 0) crc = Checksum.Crc32(buffer.AsSpan(0, read), crc);
        long dataLength = output.Position;
        long diskSectors = dataLength / 512;
        var blockMap = NetworkOrder.Bytes(s =>
        {
            s.Write("mish"u8); s.U32(1); s.U64(0); s.U64(diskSectors); s.U64(0);
            s.U32(0x800); s.U32(0xfffffffe); s.Write(new byte[24]);
            WriteChecksum(s, crc);
            s.U32(2);
            s.U32(1); s.U32(0); s.U64(0); s.U64(diskSectors); s.U64(0); s.U64(dataLength);
            s.U32(uint.MaxValue); s.U32(0); s.U64(diskSectors); s.U64(0); s.U64(dataLength); s.U64(0);
        });
        var plist = MacPackage.Plist(new XElement("dict", new XElement("key", "resource-fork"), new XElement("dict",
            new XElement("key", "blkx"), new XElement("array", new XElement("dict", new XElement("key", "Attributes"),
                new XElement("string", "0x0050"), new XElement("key", "Data"), new XElement("data", Convert.ToBase64String(blockMap)),
                new XElement("key", "ID"), new XElement("string", "-1"), new XElement("key", "Name"), new XElement("string", "whole disk"))))));
        var xml = Encoding.UTF8.GetBytes(plist);
        output.Write(xml);
        output.Write("koly"u8); output.U32(4); output.U32(512); output.U32(1);
        output.U64(0); output.U64(0); output.U64(dataLength); output.U64(0); output.U64(0);
        output.U32(1); output.U32(1); output.Write(Guid.NewGuid().ToByteArray());
        WriteChecksum(output, crc);
        output.U64(dataLength); output.U64(xml.Length); output.Write(new byte[120]);
        WriteChecksum(output, Checksum.Crc32(NetworkOrder.Bytes(s => s.U32(crc))));
        output.U32(1); output.U64(diskSectors); output.Write(new byte[12]);
    }

    private static void WriteChecksum(Stream s, uint crc)
    {
        s.U32(2); s.U32(32); s.U32(crc); s.Write(new byte[124]);
    }

    private static void WriteVolume(Stream image, uint sectors, uint size)
    {
        var pvd = new byte[Sector];
        pvd[0] = 1;
        "CD001"u8.CopyTo(pvd.AsSpan(1)); pvd[6] = 1;
        Array.Fill(pvd, (byte)' ', 8, 64);
        "MIKO_INSTALL"u8.CopyTo(pvd.AsSpan(40));
        Both32(pvd.AsSpan(80), sectors);
        Both16(pvd.AsSpan(120), 1); Both16(pvd.AsSpan(124), 1); Both16(pvd.AsSpan(128), Sector);
        Both32(pvd.AsSpan(132), 10);
        BinaryPrimitives.WriteUInt32LittleEndian(pvd.AsSpan(140), 18);
        BinaryPrimitives.WriteUInt32BigEndian(pvd.AsSpan(148), 19);
        var root = Record([0], RootSector, Sector, true);
        root.CopyTo(pvd, 156);
        Array.Fill(pvd, (byte)' ', 190, 623);
        for (int i = 813; i < 881; i += 17) "0000000000000000"u8.CopyTo(pvd.AsSpan(i));
        pvd[881] = 1;
        image.Position = 16 * Sector;
        image.Write(pvd);
        image.WriteByte(255); image.Write("CD001"u8); image.WriteByte(1);
        for (int sector = 18; sector <= 19; sector++)
        {
            byte[] pathTable = new byte[10];
            pathTable[0] = 1;
            if (sector == 18) { pathTable[2] = RootSector; pathTable[6] = 1; }
            else { pathTable[5] = RootSector; pathTable[7] = 1; }
            image.Position = sector * Sector;
            image.Write(pathTable);
        }
        image.Position = RootSector * Sector;
        image.Write(root);
        image.Write(Record([1], RootSector, Sector, true));
        image.Write(Record("INSTALL.PKG;1"u8, InstallerSector, size, false));
    }

    private static byte[] Record(ReadOnlySpan<byte> name, uint sector, uint size, bool directory)
    {
        var record = new byte[(33 + name.Length + 1) & ~1];
        record[0] = (byte)record.Length;
        Both32(record.AsSpan(2), sector); Both32(record.AsSpan(10), size);
        record[18] = 100; record[19] = 1; record[20] = 1;
        record[25] = directory ? (byte)2 : (byte)0;
        Both16(record.AsSpan(28), 1);
        record[32] = (byte)name.Length;
        name.CopyTo(record.AsSpan(33));
        return record;
    }

    private static void Both32(Span<byte> target, uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(target, value);
        BinaryPrimitives.WriteUInt32BigEndian(target[4..], value);
    }
    private static void Both16(Span<byte> target, ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(target, value);
        BinaryPrimitives.WriteUInt16BigEndian(target[2..], value);
    }
}
