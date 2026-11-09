using System.Buffers.Binary;

namespace Miko.Components.Vnc.Protocol;

/// <summary>
/// Blocking, buffered big-endian reads over the connection (RFB numbers are big-endian). Used by one
/// thread; a closed connection surfaces as <see cref="EndOfStreamException"/> or <see cref="IOException"/>.
/// </summary>
internal sealed class VncStreamReader
{
    private readonly Stream _stream;
    private readonly byte[] _buffer = new byte[64 * 1024];
    private int _start, _end;

    public VncStreamReader(Stream stream) => _stream = stream;

    /// <summary>Bytes read from the network so far.</summary>
    public long BytesRead { get; private set; }

    public byte ReadByte()
    {
        if (_start == _end) Fill(1);
        return _buffer[_start++];
    }

    public ushort ReadUInt16()
    {
        if (_end - _start < 2) Fill(2);
        var v = BinaryPrimitives.ReadUInt16BigEndian(_buffer.AsSpan(_start));
        _start += 2;
        return v;
    }

    public uint ReadUInt32()
    {
        if (_end - _start < 4) Fill(4);
        var v = BinaryPrimitives.ReadUInt32BigEndian(_buffer.AsSpan(_start));
        _start += 4;
        return v;
    }

    public int ReadInt32() => unchecked((int)ReadUInt32());

    /// <summary>Four bytes as they are on the wire (a pixel in the client's format), little-endian.</summary>
    public uint ReadPixel()
    {
        if (_end - _start < 4) Fill(4);
        var v = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(_start));
        _start += 4;
        return v;
    }

    public void ReadExactly(Span<byte> destination)
    {
        int buffered = Math.Min(_end - _start, destination.Length);
        _buffer.AsSpan(_start, buffered).CopyTo(destination);
        _start += buffered;
        destination = destination[buffered..];
        while (destination.Length > 0)
        {
            int n = _stream.Read(destination);
            if (n <= 0) throw new EndOfStreamException("The server closed the connection.");
            BytesRead += n;
            destination = destination[n..];
        }
    }

    public byte[] ReadBytes(int count)
    {
        var bytes = new byte[count];
        ReadExactly(bytes);
        return bytes;
    }

    public void Skip(long count)
    {
        while (count > 0)
        {
            if (_start == _end) Fill(1);
            int n = (int)Math.Min(count, _end - _start);
            _start += n;
            count -= n;
        }
    }

    private void Fill(int needed)
    {
        if (_start > 0)
        {
            Buffer.BlockCopy(_buffer, _start, _buffer, 0, _end - _start);
            _end -= _start;
            _start = 0;
        }
        while (_end < needed)
        {
            int n = _stream.Read(_buffer, _end, _buffer.Length - _end);
            if (n <= 0) throw new EndOfStreamException("The server closed the connection.");
            BytesRead += n;
            _end += n;
        }
    }
}
