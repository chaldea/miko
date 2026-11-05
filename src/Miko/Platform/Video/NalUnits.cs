namespace Miko.Platform.Video;

/// <summary>
/// H.264 / H.265 NAL unit helpers shared by stream sources (depacketizers) and platform decoders
/// (which need the parameter sets out of band and the slices length-prefixed).
/// </summary>
public static class NalUnits
{
    /// <summary>The 4-byte Annex-B start code.</summary>
    public static ReadOnlySpan<byte> StartCode => [0, 0, 0, 1];

    /// <summary>Enumerates the NAL units of an Annex-B buffer, without their start codes.</summary>
    public static AnnexBEnumerator EnumerateAnnexB(ReadOnlySpan<byte> data) => new(data);

    /// <summary>The NAL unit type from its header (first byte for H.264, first two for H.265).</summary>
    public static int GetType(VideoCodec codec, ReadOnlySpan<byte> nal)
    {
        if (nal.IsEmpty) return -1;
        return codec == VideoCodec.H264 ? nal[0] & 0x1f : (nal[0] >> 1) & 0x3f;
    }

    /// <summary>SPS / PPS (H.264) or VPS / SPS / PPS (H.265).</summary>
    public static bool IsParameterSet(VideoCodec codec, int type) =>
        codec == VideoCodec.H264 ? type is 7 or 8 : type is 32 or 33 or 34;

    /// <summary>A slice of a random access point: IDR (H.264) or IRAP — BLA / IDR / CRA (H.265).</summary>
    public static bool IsKeyFrameSlice(VideoCodec codec, int type) =>
        codec == VideoCodec.H264 ? type == 5 : type is >= 16 and <= 21;

    /// <summary>A coded slice (video coding layer) as opposed to parameter sets, SEI, delimiters.</summary>
    public static bool IsSlice(VideoCodec codec, int type) =>
        codec == VideoCodec.H264 ? type is >= 1 and <= 5 : type is >= 0 and <= 31;

    /// <summary>Whether the Annex-B access unit holds a key frame slice.</summary>
    public static bool ContainsKeyFrame(VideoCodec codec, ReadOnlySpan<byte> accessUnit)
    {
        foreach (var nal in EnumerateAnnexB(accessUnit))
            if (IsKeyFrameSlice(codec, GetType(codec, nal))) return true;
        return false;
    }

    /// <summary>
    /// The parameter sets of an Annex-B access unit in decoder order (VPS, SPS, PPS), each without
    /// its start code; empty when the access unit carries none (or not all of them).
    /// </summary>
    public static List<byte[]> GetParameterSets(VideoCodec codec, ReadOnlySpan<byte> accessUnit)
    {
        byte[]? vps = null, sps = null, pps = null;
        foreach (var nal in EnumerateAnnexB(accessUnit))
        {
            switch (GetType(codec, nal))
            {
                case 32 when codec == VideoCodec.H265: vps ??= nal.ToArray(); break;
                case 33 when codec == VideoCodec.H265: sps ??= nal.ToArray(); break;
                case 34 when codec == VideoCodec.H265: pps ??= nal.ToArray(); break;
                case 7 when codec == VideoCodec.H264: sps ??= nal.ToArray(); break;
                case 8 when codec == VideoCodec.H264: pps ??= nal.ToArray(); break;
            }
        }
        var sets = new List<byte[]>(3);
        if (sps == null || pps == null || (codec == VideoCodec.H265 && vps == null)) return sets;
        if (vps != null) sets.Add(vps);
        sets.Add(sps);
        sets.Add(pps);
        return sets;
    }

    /// <summary>
    /// Writes the access unit's NAL units except parameter sets and access unit delimiters as
    /// 4-byte big-endian length-prefixed units (the AVCC / HVCC sample layout VideoToolbox and MP4
    /// use). Returns the number of bytes written; <paramref name="destination"/> needs at most
    /// <c>accessUnit.Length + 4</c> bytes (every start code is at least 3 bytes, every prefix 4).
    /// </summary>
    public static int WriteLengthPrefixed(VideoCodec codec, ReadOnlySpan<byte> accessUnit, Span<byte> destination)
    {
        int written = 0;
        foreach (var nal in EnumerateAnnexB(accessUnit))
        {
            int type = GetType(codec, nal);
            if (IsParameterSet(codec, type) || type == (codec == VideoCodec.H264 ? 9 : 35)) continue;
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(destination[written..], nal.Length);
            nal.CopyTo(destination[(written + 4)..]);
            written += 4 + nal.Length;
        }
        return written;
    }

    /// <summary>Enumerator over the NAL units of an Annex-B buffer (see <see cref="EnumerateAnnexB"/>).</summary>
    public ref struct AnnexBEnumerator
    {
        private readonly ReadOnlySpan<byte> _data;
        private int _next;

        internal AnnexBEnumerator(ReadOnlySpan<byte> data)
        {
            _data = data;
            _next = FindStart(data, 0);
            Current = default;
        }

        public ReadOnlySpan<byte> Current { get; private set; }

        public readonly AnnexBEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            while (_next >= 0 && _next < _data.Length)
            {
                int start = _next;
                int following = FindStartCode(_data, start, out int codeLength);
                int end = following < 0 ? _data.Length : following;
                _next = following < 0 ? -1 : following + codeLength;
                // A 4-byte start code's leading zero belongs to it, not to the previous NAL unit
                // (trailing zero bytes are not part of a NAL unit either).
                while (end > start && _data[end - 1] == 0) end--;
                if (end > start)
                {
                    Current = _data[start..end];
                    return true;
                }
            }
            return false;
        }

        // The first byte after the first start code, or 0 when the buffer has no start code at all
        // (a bare NAL unit is enumerated as one unit).
        private static int FindStart(ReadOnlySpan<byte> data, int from)
        {
            int code = FindStartCode(data, from, out int length);
            return code < 0 ? (data.IsEmpty ? -1 : 0) : code + length;
        }

        // Index of the next 00 00 01 at or after `from` (length 3); -1 when none.
        private static int FindStartCode(ReadOnlySpan<byte> data, int from, out int length)
        {
            length = 3;
            for (int i = from; i + 2 < data.Length; i++)
            {
                if (data[i + 2] > 1) { i += 2; continue; }
                if (data[i] == 0 && data[i + 1] == 0 && data[i + 2] == 1) return i;
            }
            return -1;
        }
    }
}
