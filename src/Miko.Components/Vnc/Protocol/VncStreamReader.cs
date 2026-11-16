using System.Buffers.Binary;

namespace Miko.Components.Vnc.Protocol;

/// <summary>
/// Buffered big-endian reads (RFB numbers are big-endian), used by one receive loop.
/// Async prefetch keeps WebSocket reads off the blocking decoder path. A closed connection surfaces
/// as <see cref="EndOfStreamException"/> or <see cref="IOException"/>.
/// </summary>
internal sealed class VncStreamReader
{
    private readonly Stream _stream;
    private byte[] _buffer = new byte[64 * 1024];
    private int _start, _end;

    public VncStreamReader(Stream stream) => _stream = stream;

    /// <summary>Bytes read from the network so far.</summary>
    public long BytesRead { get; private set; }

    // Prefetch before using the synchronous pixel decoders on a browser's event loop.
    public async ValueTask EnsureAsync(int count, CancellationToken ct = default)
    {
        if (count < 0 || count > 128 * 1024 * 1024) throw new InvalidDataException("RFB payload is too large.");
        if (_end - _start >= count) return;
        Buffer.BlockCopy(_buffer, _start, _buffer, 0, _end - _start);
        _end -= _start;
        _start = 0;
        if (_buffer.Length < count) Array.Resize(ref _buffer, count);
        while (_end < count)
        {
            int n = await _stream.ReadAsync(_buffer.AsMemory(_end), ct).ConfigureAwait(false);
            if (n == 0) throw new EndOfStreamException("The server closed the connection.");
            _end += n;
            BytesRead += n;
        }
    }

    public async ValueTask<byte[]> ReadBytesAsync(int count, CancellationToken ct = default)
    { await EnsureAsync(count, ct).ConfigureAwait(false); return ReadBytes(count); }
    public async ValueTask<byte> ReadByteAsync(CancellationToken ct = default)
    { await EnsureAsync(1, ct).ConfigureAwait(false); return ReadByte(); }
    public async ValueTask<ushort> ReadUInt16Async(CancellationToken ct = default)
    { await EnsureAsync(2, ct).ConfigureAwait(false); return ReadUInt16(); }
    public async ValueTask<uint> ReadUInt32Async(CancellationToken ct = default)
    { await EnsureAsync(4, ct).ConfigureAwait(false); return ReadUInt32(); }
    public async ValueTask<int> ReadInt32Async(CancellationToken ct = default)
    { await EnsureAsync(4, ct).ConfigureAwait(false); return ReadInt32(); }
    public async ValueTask SkipAsync(long count, CancellationToken ct = default)
    {
        while (count > 0)
        {
            int n = (int)Math.Min(count, 64 * 1024);
            await EnsureAsync(n, ct).ConfigureAwait(false);
            Skip(n);
            count -= n;
        }
    }

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
