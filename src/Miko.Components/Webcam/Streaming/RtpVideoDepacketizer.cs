using System.Buffers.Binary;
using Miko.Platform.Video;

namespace Miko.Components.Webcam.Streaming;

/// <summary>
/// Reassembles H.264 (RFC 6184) and H.265 (RFC 7798) RTP payloads into Annex-B access units.
/// </summary>
/// <remarks>
/// <para>
/// An access unit ends when the RTP timestamp changes or when a packet with the marker bit closes
/// one that holds a slice. Cameras do not all set the marker where the RFCs put it — Hikvision /
/// Ezviz set it after the PPS that precedes a key frame — so a marker on parameter sets alone does
/// not end anything.
/// </para>
/// <para>
/// The type of a fragmented NAL unit is taken from its start fragment only: the same cameras write
/// another type (38) into the continuation fragments of an IDR slice.
/// </para>
/// <para>
/// A gap in the sequence numbers drops the fragmented NAL unit it cuts; the rest of the access unit
/// is kept and the decoder conceals the damage, as players do. Parameter sets from the SDP
/// (<c>sprop-*</c>) are put in front of a key frame that does not carry its own.
/// </para>
/// </remarks>
internal sealed class RtpVideoDepacketizer
{
    private readonly VideoCodec _codec;
    private readonly int _payloadType;
    private readonly double _clockRate;
    private readonly byte[][] _sdpParameterSets;
    private readonly Queue<AccessUnit> _completed = new();
    private readonly Stack<byte[]> _spare = new();

    private byte[] _unit = new byte[64 * 1024];
    private int _unitLength;
    private uint _unitTimestamp;
    private bool _unitHasSlice, _unitHasKeySlice, _unitHasParameterSets;
    private bool _haveUnit;

    // Fragmented NAL unit being reassembled into _unit (its start offset), or -1.
    private int _fragmentStart = -1;
    private int _lastSequence = -1;
    private long _firstTimestamp = -1;
    private long _timestampBase;
    private uint _lastTimestamp;

    public RtpVideoDepacketizer(VideoCodec codec, int payloadType, int clockRate = 90_000, IEnumerable<byte[]>? sdpParameterSets = null)
    {
        _codec = codec;
        _payloadType = payloadType;
        _clockRate = clockRate;
        _sdpParameterSets = sdpParameterSets?.Where(p => p.Length > 0).ToArray() ?? [];
    }

    /// <summary>Access units completed by the packets pushed so far.</summary>
    public int CompletedCount => _completed.Count;

    /// <summary>Packets that carried another payload type (audio on a shared channel, …) and were ignored.</summary>
    public long IgnoredPackets { get; private set; }

    /// <summary>Sequence gaps seen.</summary>
    public long LostPackets { get; private set; }

    /// <summary>
    /// Takes the oldest completed access unit. Its buffer stays valid until it is handed back with
    /// <see cref="Recycle"/>.
    /// </summary>
    public AccessUnit Dequeue() => _completed.Dequeue();

    /// <summary>Returns the buffer of an access unit taken with <see cref="Dequeue"/>.</summary>
    public void Recycle(AccessUnit unit)
    {
        if (unit.Buffer != null && _spare.Count < 4) _spare.Push(unit.Buffer);
    }

    /// <summary>Feeds one RTP packet (header included).</summary>
    public void Push(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 12 || (packet[0] >> 6) != 2) return;
        int csrcCount = packet[0] & 0x0f;
        bool padding = (packet[0] & 0x20) != 0;
        bool extension = (packet[0] & 0x10) != 0;
        bool marker = (packet[1] & 0x80) != 0;
        int payloadType = packet[1] & 0x7f;
        int sequence = BinaryPrimitives.ReadUInt16BigEndian(packet[2..]);
        uint timestamp = BinaryPrimitives.ReadUInt32BigEndian(packet[4..]);

        if (payloadType != _payloadType)
        {
            IgnoredPackets++;
            return;
        }

        int offset = 12 + csrcCount * 4;
        if (extension)
        {
            if (packet.Length < offset + 4) return;
            offset += 4 + BinaryPrimitives.ReadUInt16BigEndian(packet[(offset + 2)..]) * 4;
        }
        int end = packet.Length;
        if (padding) end -= packet[^1];
        if (offset >= end) return;
        var payload = packet[offset..end];

        if (_lastSequence >= 0 && sequence != ((_lastSequence + 1) & 0xffff))
        {
            LostPackets++;
            DropFragment();
        }
        _lastSequence = sequence;

        if (_haveUnit && timestamp != _unitTimestamp) CompleteUnit();
        if (!_haveUnit)
        {
            _haveUnit = true;
            _unitTimestamp = timestamp;
        }

        if (_codec == VideoCodec.H264) PushH264(payload);
        else PushH265(payload);

        // The marker closes an access unit once it holds a picture (see the remarks).
        if (marker && _unitHasSlice && _fragmentStart < 0) CompleteUnit();
    }

    private void PushH264(ReadOnlySpan<byte> payload)
    {
        int type = payload[0] & 0x1f;
        switch (type)
        {
            case 24: // STAP-A
                PushAggregate(payload[1..]);
                break;
            case 28: // FU-A
                if (payload.Length < 2) return;
                PushFragment(payload[1], payload[2..], (byte)((payload[0] & 0xe0) | (payload[1] & 0x1f)), 0, 1);
                break;
            case >= 1 and <= 23:
                AppendNal(payload);
                break;
        }
    }

    private void PushH265(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 2) return;
        int type = (payload[0] >> 1) & 0x3f;
        switch (type)
        {
            case 48: // aggregation packet
                PushAggregate(payload[2..]);
                break;
            case 49: // fragmentation unit
                if (payload.Length < 3) return;
                PushFragment(payload[2], payload[3..], (byte)((payload[0] & 0x81) | ((payload[2] & 0x3f) << 1)), payload[1], 2);
                break;
            case 50: // PACI: not used by cameras
                break;
            default:
                AppendNal(payload);
                break;
        }
    }

    private void PushAggregate(ReadOnlySpan<byte> units)
    {
        while (units.Length >= 2)
        {
            int size = BinaryPrimitives.ReadUInt16BigEndian(units);
            units = units[2..];
            if (size == 0 || size > units.Length) return;
            AppendNal(units[..size]);
            units = units[size..];
        }
    }

    // header0/header1: the NAL unit header rebuilt from the payload header and the FU header (one
    // byte for H.264, two for H.265); used only by the start fragment.
    private void PushFragment(byte fuHeader, ReadOnlySpan<byte> data, byte header0, byte header1, int headerLength)
    {
        bool start = (fuHeader & 0x80) != 0;
        bool end = (fuHeader & 0x40) != 0;
        if (start)
        {
            DropFragment();
            _fragmentStart = _unitLength;
            Append(NalUnits.StartCode);
            Span<byte> header = [header0, header1];
            Append(header[..headerLength]);
        }
        else if (_fragmentStart < 0)
        {
            return; // the start was lost
        }
        Append(data);
        if (end)
        {
            int nalStart = _fragmentStart + 4;
            _fragmentStart = -1;
            NoteNal(_unit.AsSpan(nalStart, _unitLength - nalStart));
        }
    }

    private void DropFragment()
    {
        if (_fragmentStart < 0) return;
        _unitLength = _fragmentStart;
        _fragmentStart = -1;
    }

    private void AppendNal(ReadOnlySpan<byte> nal)
    {
        if (nal.IsEmpty) return;
        Append(NalUnits.StartCode);
        Append(nal);
        NoteNal(nal);
    }

    private void NoteNal(ReadOnlySpan<byte> nal)
    {
        int type = NalUnits.GetType(_codec, nal);
        if (NalUnits.IsParameterSet(_codec, type)) _unitHasParameterSets = true;
        if (NalUnits.IsSlice(_codec, type)) _unitHasSlice = true;
        if (NalUnits.IsKeyFrameSlice(_codec, type)) _unitHasKeySlice = true;
    }

    private void Append(ReadOnlySpan<byte> data)
    {
        if (_unitLength + data.Length > _unit.Length)
            Array.Resize(ref _unit, Math.Max(_unit.Length * 2, _unitLength + data.Length));
        data.CopyTo(_unit.AsSpan(_unitLength));
        _unitLength += data.Length;
    }

    private void CompleteUnit()
    {
        DropFragment();
        if (_unitHasSlice && _unitLength > 0)
        {
            byte[] data;
            int length;
            if (_unitHasKeySlice && !_unitHasParameterSets && _sdpParameterSets.Length > 0)
            {
                length = _sdpParameterSets.Sum(p => p.Length + 4) + _unitLength;
                data = Rent(length);
                int at = 0;
                foreach (var set in _sdpParameterSets)
                {
                    NalUnits.StartCode.CopyTo(data.AsSpan(at));
                    set.CopyTo(data.AsSpan(at + 4));
                    at += 4 + set.Length;
                }
                _unit.AsSpan(0, _unitLength).CopyTo(data.AsSpan(at));
            }
            else
            {
                // Hand the assembly buffer over and assemble the next unit into a spare one.
                data = _unit;
                length = _unitLength;
                _unit = Rent(Math.Max(64 * 1024, _unitLength));
            }
            _completed.Enqueue(new AccessUnit(data, length, ToTime(_unitTimestamp), _unitHasKeySlice));
        }
        _haveUnit = false;
        _unitLength = 0;
        _unitHasSlice = _unitHasKeySlice = _unitHasParameterSets = false;
    }

    private byte[] Rent(int size)
    {
        while (_spare.TryPop(out var spare))
            if (spare.Length >= size) return spare;
        return new byte[size];
    }

    // RTP timestamps are 32-bit and wrap; the stream clock starts at the first packet.
    private TimeSpan ToTime(uint timestamp)
    {
        if (_firstTimestamp < 0)
        {
            _firstTimestamp = timestamp;
            _lastTimestamp = timestamp;
        }
        if (timestamp < _lastTimestamp && _lastTimestamp - timestamp > 0x8000_0000u) _timestampBase += 1L << 32;
        _lastTimestamp = timestamp;
        long ticks = _timestampBase + timestamp - _firstTimestamp;
        return TimeSpan.FromSeconds(ticks / _clockRate);
    }

    /// <summary>A completed Annex-B access unit; <see cref="Buffer"/> holds <see cref="Length"/> bytes.</summary>
    public readonly record struct AccessUnit(byte[] Buffer, int Length, TimeSpan Timestamp, bool IsKeyFrame)
    {
        public ReadOnlyMemory<byte> Data => Buffer.AsMemory(0, Length);
    }
}
