using System.Buffers.Binary;
using System.Text;

namespace Miko.Packaging;

// RPM, BOM, XAR and UDIF all store integers most significant byte first.
internal static class NetworkOrder
{
    public static void U16(this Stream output, int value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, checked((ushort)value));
        output.Write(buffer);
    }

    public static void U32(this Stream output, long value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, checked((uint)value));
        output.Write(buffer);
    }

    public static void U64(this Stream output, long value)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, checked((ulong)value));
        output.Write(buffer);
    }

    public static void CString(this Stream output, string value)
    {
        output.Write(Encoding.UTF8.GetBytes(value));
        output.WriteByte(0);
    }

    public static byte[] Bytes(Action<Stream> write)
    {
        using var buffer = new MemoryStream();
        write(buffer);
        return buffer.ToArray();
    }
}
