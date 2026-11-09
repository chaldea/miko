namespace Miko.Components.Vnc.Protocol;

/// <summary>
/// A streaming zlib / deflate decompressor (RFC 1950 / 1951) in managed code.
/// </summary>
/// <remarks>
/// <para>
/// VNC's zlib-based encodings (Zlib, ZRLE, Tight) keep <b>one zlib stream for the whole connection</b> and
/// send it in pieces: each rectangle carries the bytes the server produced for it, ended by a sync (or
/// partial) flush. Each piece has to be decompressed as it arrives, with the 32 KB history of everything
/// before it, and the stream never ends. <c>System.IO.Compression</c> streams expect a source they can
/// read ahead from, and they are native code on most platforms; this one is fed piece by piece instead.
/// </para>
/// <para>
/// <see cref="Inflate"/> decodes every complete block of what it has been fed. A piece that ends inside a
/// block (no flush) keeps the block's bytes and decodes it once the rest arrives.
/// </para>
/// <para>Not thread-safe.</para>
/// </remarks>
internal sealed class ZlibInflater
{
    private const int WindowSize = 32 * 1024;

    // Input not decoded yet (the bytes of an unfinished block, then whatever was fed).
    private byte[] _input = new byte[64 * 1024];
    private int _inputStart, _inputEnd;

    // Bits taken from the input but not consumed. Fixed to the input: restoring a checkpoint restores both.
    private ulong _bitBuffer;
    private int _bitCount;

    // History (up to 32 KB of the latest output) followed by this call's output; back-references reach into both.
    private byte[] _output = new byte[256 * 1024];
    private int _historyLength;
    private int _outputLength;

    private bool _headerRead;
    private bool _finished;

    private readonly Huffman _lengths = new(288);
    private readonly Huffman _distances = new(32);
    private readonly Huffman _codeLengths = new(19);

    /// <summary>
    /// Whether the input starts with a 2-byte zlib header (RFC 1950). VNC streams do; raw deflate data does not.
    /// </summary>
    public bool ExpectHeader { get; init; } = true;

    /// <summary>Total bytes produced since the last reset.</summary>
    public long TotalOut { get; private set; }

    /// <summary>Forgets the stream: the next input is the start of a new zlib stream (Tight's reset bits).</summary>
    public void Reset()
    {
        _inputStart = _inputEnd = 0;
        _bitBuffer = 0;
        _bitCount = 0;
        _historyLength = 0;
        _outputLength = 0;
        _headerRead = false;
        _finished = false;
        _pendingCompact = false;
        TotalOut = 0;
    }

    /// <summary>
    /// Feeds <paramref name="compressed"/> and returns what it completes. The span is valid until the next
    /// call.
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a valid zlib stream.</exception>
    public ReadOnlySpan<byte> Inflate(ReadOnlySpan<byte> compressed)
    {
        Append(compressed);
        _outputLength = _historyLength;

        while (!_finished)
        {
            // Checkpoint at a block boundary: running out of input inside the block rolls back to it.
            int checkpointInput = _inputStart, checkpointBits = _bitCount, checkpointLength = _outputLength;
            ulong checkpointBuffer = _bitBuffer;
            try
            {
                if (!_headerRead)
                {
                    if (ExpectHeader) ReadHeader();
                    _headerRead = true;
                    continue;
                }
                if (!DecodeBlock()) break;
            }
            catch (NeedInputException)
            {
                _inputStart = checkpointInput;
                _bitBuffer = checkpointBuffer;
                _bitCount = checkpointBits;
                _outputLength = checkpointLength;
                break;
            }
        }

        int produced = _outputLength - _historyLength;
        TotalOut += produced;
        // The last 32 KB of output become the next call's history, moved to the front when that call
        // starts — not now, since the returned span points into the same buffer.
        _pendingCompact = true;
        return _output.AsSpan(_historyLength, produced);
    }

    private bool _pendingCompact;

    // Moves the latest 32 KB of output to the front (the previous call's result is no longer in use).
    private void CompactHistory()
    {
        if (!_pendingCompact) return;
        _pendingCompact = false;
        int keep = Math.Min(WindowSize, _outputLength);
        if (keep > 0 && _outputLength - keep > 0)
            Buffer.BlockCopy(_output, _outputLength - keep, _output, 0, keep);
        _historyLength = keep;
        _outputLength = keep;
    }

    private void Append(ReadOnlySpan<byte> data)
    {
        CompactHistory();
        int pending = _inputEnd - _inputStart;
        if (pending + data.Length > _input.Length)
        {
            var bigger = new byte[Math.Max(_input.Length * 2, pending + data.Length)];
            Buffer.BlockCopy(_input, _inputStart, bigger, 0, pending);
            _input = bigger;
        }
        else if (_inputStart > 0)
        {
            Buffer.BlockCopy(_input, _inputStart, _input, 0, pending);
        }
        _inputStart = 0;
        _inputEnd = pending;
        data.CopyTo(_input.AsSpan(_inputEnd));
        _inputEnd += data.Length;
    }

    private void ReadHeader()
    {
        int cmf = (int)Bits(8), flg = (int)Bits(8);
        if ((cmf & 0x0F) != 8 || (cmf >> 4) > 7) throw new InvalidDataException("Not a zlib stream (compression method).");
        if (((cmf << 8) | flg) % 31 != 0) throw new InvalidDataException("Corrupt zlib header.");
        if ((flg & 0x20) != 0) throw new InvalidDataException("zlib preset dictionaries are not supported.");
    }

    // Decodes one block. False when there is not even a block header to read (the input ended at a boundary).
    private bool DecodeBlock()
    {
        if (_bitCount < 3 && _inputStart >= _inputEnd) return false;
        bool final = Bits(1) == 1;
        int type = (int)Bits(2);
        switch (type)
        {
            case 0: StoredBlock(); break;
            case 1: CompressedBlock(FixedTables.Lengths, FixedTables.Distances); break;
            case 2: DynamicBlock(); break;
            default: throw new InvalidDataException("Invalid deflate block type.");
        }
        if (final) _finished = true;
        return true;
    }

    private void StoredBlock()
    {
        // Skip to the byte boundary; what is left in the bit buffer is whole bytes.
        _bitBuffer >>= _bitCount & 7;
        _bitCount &= ~7;
        int length = (int)Bits(16);
        int complement = (int)Bits(16);
        if ((length ^ 0xFFFF) != complement) throw new InvalidDataException("Corrupt stored block length.");
        EnsureOutput(length);
        while (length > 0 && _bitCount >= 8)
        {
            _output[_outputLength++] = (byte)_bitBuffer;
            _bitBuffer >>= 8;
            _bitCount -= 8;
            length--;
        }
        if (_inputEnd - _inputStart < length) throw NeedInputException.Instance;
        Buffer.BlockCopy(_input, _inputStart, _output, _outputLength, length);
        _inputStart += length;
        _outputLength += length;
    }

    private void DynamicBlock()
    {
        int literalCount = (int)Bits(5) + 257;
        int distanceCount = (int)Bits(5) + 1;
        int codeLengthCount = (int)Bits(4) + 4;
        if (literalCount > 286 || distanceCount > 30) throw new InvalidDataException("Too many deflate codes.");

        Span<byte> codeLengthLengths = stackalloc byte[19];
        codeLengthLengths.Clear();
        for (int i = 0; i < codeLengthCount; i++) codeLengthLengths[CodeLengthOrder[i]] = (byte)Bits(3);
        _codeLengths.Build(codeLengthLengths);

        Span<byte> lengths = stackalloc byte[286 + 30];
        int total = literalCount + distanceCount;
        int n = 0;
        while (n < total)
        {
            int symbol = Decode(_codeLengths);
            if (symbol < 16)
            {
                lengths[n++] = (byte)symbol;
                continue;
            }
            int repeat;
            byte value = 0;
            if (symbol == 16)
            {
                if (n == 0) throw new InvalidDataException("Deflate repeat with no previous length.");
                value = lengths[n - 1];
                repeat = 3 + (int)Bits(2);
            }
            else if (symbol == 17) repeat = 3 + (int)Bits(3);
            else repeat = 11 + (int)Bits(7);
            if (n + repeat > total) throw new InvalidDataException("Deflate code lengths overflow.");
            lengths.Slice(n, repeat).Fill(value);
            n += repeat;
        }
        if (lengths[256] == 0) throw new InvalidDataException("Deflate block without an end code.");
        _lengths.Build(lengths[..literalCount]);
        _distances.Build(lengths.Slice(literalCount, distanceCount));
        CompressedBlock(_lengths, _distances);
    }

    private void CompressedBlock(Huffman lengths, Huffman distances)
    {
        while (true)
        {
            int symbol = Decode(lengths);
            if (symbol < 256)
            {
                if (_outputLength == _output.Length) EnsureOutput(1);
                _output[_outputLength++] = (byte)symbol;
                continue;
            }
            if (symbol == 256) return;
            symbol -= 257;
            if (symbol >= 29) throw new InvalidDataException("Invalid deflate length code.");
            int length = LengthBase[symbol] + (int)Bits(LengthExtra[symbol]);
            int distanceSymbol = Decode(distances);
            if (distanceSymbol >= 30) throw new InvalidDataException("Invalid deflate distance code.");
            int distance = DistanceBase[distanceSymbol] + (int)Bits(DistanceExtra[distanceSymbol]);
            int from = _outputLength - distance;
            if (from < 0) throw new InvalidDataException("Deflate distance reaches before the stream.");
            EnsureOutput(length);
            var output = _output;
            int to = _outputLength;
            if (distance >= length)
            {
                Buffer.BlockCopy(output, from, output, to, length);
            }
            else
            {
                // Overlapping (a run): copy forward, byte by byte.
                for (int i = 0; i < length; i++) output[to + i] = output[from + i];
            }
            _outputLength += length;
        }
    }

    private void EnsureOutput(int extra)
    {
        if (_outputLength + extra <= _output.Length) return;
        var bigger = new byte[Math.Max(_output.Length * 2, _outputLength + extra)];
        Buffer.BlockCopy(_output, 0, bigger, 0, _outputLength);
        _output = bigger;
    }

    // ---- bits ----------------------------------------------------------------------

    private uint Bits(int count)
    {
        if (count == 0) return 0;
        while (_bitCount < count)
        {
            if (_inputStart >= _inputEnd) throw NeedInputException.Instance;
            _bitBuffer |= (ulong)_input[_inputStart++] << _bitCount;
            _bitCount += 8;
        }
        uint value = (uint)(_bitBuffer & ((1UL << count) - 1));
        _bitBuffer >>= count;
        _bitCount -= count;
        return value;
    }

    private void Refill()
    {
        while (_bitCount <= 56 && _inputStart < _inputEnd)
        {
            _bitBuffer |= (ulong)_input[_inputStart++] << _bitCount;
            _bitCount += 8;
        }
    }

    private int Decode(Huffman table)
    {
        if (_bitCount < Huffman.FastBits) Refill();
        // Peek with whatever is buffered: missing bits read as zero, and a match longer than what is really
        // there is not taken.
        int entry = table.Fast[(int)(_bitBuffer & Huffman.FastMask)];
        int length = entry & 0xF;
        if (length != 0 && length <= _bitCount)
        {
            _bitBuffer >>= length;
            _bitCount -= length;
            return entry >> 4;
        }
        return DecodeSlow(table);
    }

    // Canonical decoding one bit at a time (codes longer than the fast table, and the end of the input).
    private int DecodeSlow(Huffman table)
    {
        int code = 0, first = 0, index = 0;
        for (int len = 1; len <= 15; len++)
        {
            code |= (int)Bits(1);
            int count = table.Counts[len];
            if (code - first < count) return table.Symbols[index + code - first];
            index += count;
            first = (first + count) << 1;
            code <<= 1;
        }
        throw new InvalidDataException("Invalid deflate code.");
    }

    private sealed class NeedInputException : Exception
    {
        public static readonly NeedInputException Instance = new();
    }

    // ---- tables --------------------------------------------------------------------

    private sealed class Huffman
    {
        public const int FastBits = 9;
        public const int FastMask = (1 << FastBits) - 1;

        public readonly int[] Counts = new int[16];
        public readonly int[] Symbols;
        // symbol << 4 | code length; 0 = longer than FastBits (slow path).
        public readonly int[] Fast = new int[1 << FastBits];

        public Huffman(int maxSymbols) => Symbols = new int[maxSymbols];

        public void Build(ReadOnlySpan<byte> lengths)
        {
            Array.Clear(Counts);
            foreach (byte l in lengths) Counts[l]++;
            Counts[0] = 0;

            // Over-subscribed sets are invalid; incomplete ones occur (a single distance code) and are allowed.
            int left = 1;
            for (int len = 1; len <= 15; len++)
            {
                left = (left << 1) - Counts[len];
                if (left < 0) throw new InvalidDataException("Over-subscribed deflate code.");
            }

            Span<int> offsets = stackalloc int[16];
            offsets[1] = 0;
            for (int len = 1; len < 15; len++) offsets[len + 1] = offsets[len] + Counts[len];
            for (int symbol = 0; symbol < lengths.Length; symbol++)
                if (lengths[symbol] != 0) Symbols[offsets[lengths[symbol]]++] = symbol;

            Array.Clear(Fast);
            int code = 0, index = 0;
            for (int len = 1; len <= 15; len++)
            {
                for (int i = 0; i < Counts[len]; i++, code++, index++)
                {
                    if (len > FastBits) continue;
                    int reversed = Reverse(code, len);
                    int entry = (Symbols[index] << 4) | len;
                    for (int slot = reversed; slot < Fast.Length; slot += 1 << len) Fast[slot] = entry;
                }
                code <<= 1;
            }
        }

        private static int Reverse(int code, int length)
        {
            int r = 0;
            for (int i = 0; i < length; i++)
            {
                r = (r << 1) | (code & 1);
                code >>= 1;
            }
            return r;
        }
    }

    private static class FixedTables
    {
        public static readonly Huffman Lengths = BuildLengths();
        public static readonly Huffman Distances = BuildDistances();

        private static Huffman BuildLengths()
        {
            var lengths = new byte[288];
            for (int i = 0; i < 144; i++) lengths[i] = 8;
            for (int i = 144; i < 256; i++) lengths[i] = 9;
            for (int i = 256; i < 280; i++) lengths[i] = 7;
            for (int i = 280; i < 288; i++) lengths[i] = 8;
            var table = new Huffman(288);
            table.Build(lengths);
            return table;
        }

        private static Huffman BuildDistances()
        {
            var lengths = new byte[30];
            Array.Fill(lengths, (byte)5);
            var table = new Huffman(32);
            table.Build(lengths);
            return table;
        }
    }

    private static ReadOnlySpan<byte> CodeLengthOrder => [16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15];

    private static readonly int[] LengthBase =
        [3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258];

    private static readonly int[] LengthExtra =
        [0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0];

    private static readonly int[] DistanceBase =
        [1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577];

    private static readonly int[] DistanceExtra =
        [0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13];
}
