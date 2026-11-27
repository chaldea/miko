namespace Miko.Packaging;

internal static class Checksum
{
    private static readonly uint[] Posix = Table(false);
    private static readonly uint[] Reflected = Table(true);

    private static uint[] Table(bool reflected)
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            uint value = reflected ? i : i << 24;
            for (int bit = 0; bit < 8; bit++)
                value = reflected ? (value >> 1) ^ ((value & 1) != 0 ? 0xedb88320u : 0) :
                    (value << 1) ^ ((value >> 31) != 0 ? 0x04c11db7u : 0);
            table[i] = value;
        }
        return table;
    }

    public static uint Crc32(ReadOnlySpan<byte> data, uint previous = 0)
    {
        uint value = ~previous;
        foreach (byte b in data) value = Reflected[(value ^ b) & 255] ^ (value >> 8);
        return ~value;
    }

    // POSIX cksum includes the little-endian length after the file bytes.
    public static uint Bom(PackageEntry file)
    {
        uint value = 0;
        using var input = file.Open();
        byte[] buffer = new byte[81920];
        int read;
        while ((read = input.Read(buffer)) > 0)
            foreach (byte b in buffer.AsSpan(0, read)) value = Posix[(value >> 24) ^ b] ^ (value << 8);
        for (long size = file.Size; size != 0; size >>= 8)
            value = Posix[(value >> 24) ^ (byte)size] ^ (value << 8);
        return ~value;
    }
}
