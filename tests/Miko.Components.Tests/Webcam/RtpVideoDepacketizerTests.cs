using Miko.Components.Webcam.Streaming;
using Miko.Platform.Video;
using Shouldly;

namespace Miko.Components.Tests.Webcam;

/// <summary>
/// RTP → Annex-B access units. The packet layouts are the ones captured from an Ezviz C6c
/// (H.265 over RTSP): parameter sets as single packets, the PPS carrying the marker bit, and the
/// continuation fragments of an IDR slice labelled type 38.
/// </summary>
public class RtpVideoDepacketizerTests
{
    private const int PayloadType = 96;
    private ushort _sequence = 1000;

    private byte[] Packet(uint timestamp, bool marker, params byte[] payload)
    {
        var packet = new byte[12 + payload.Length];
        packet[0] = 0x80;
        packet[1] = (byte)((marker ? 0x80 : 0) | PayloadType);
        packet[2] = (byte)(_sequence >> 8);
        packet[3] = (byte)_sequence;
        _sequence++;
        packet[4] = (byte)(timestamp >> 24);
        packet[5] = (byte)(timestamp >> 16);
        packet[6] = (byte)(timestamp >> 8);
        packet[7] = (byte)timestamp;
        payload.CopyTo(packet, 12);
        return packet;
    }

    private static readonly byte[] Vps = [0x40, 0x01, 0x0c, 0x01];
    private static readonly byte[] Sps = [0x42, 0x01, 0x01, 0x01, 0x60];
    private static readonly byte[] Pps = [0x44, 0x01, 0xc0, 0xf2];

    // H.265 FU: payload header type 49 (0x62 0x01), FU header S/E bits + type.
    private static byte[] Fu(bool start, bool end, int type, params byte[] data) =>
        [0x62, 0x01, (byte)((start ? 0x80 : 0) | (end ? 0x40 : 0) | type), .. data];

    private static List<int> Types(RtpVideoDepacketizer.AccessUnit unit)
    {
        var types = new List<int>();
        foreach (var nal in NalUnits.EnumerateAnnexB(unit.Data.Span)) types.Add(NalUnits.GetType(VideoCodec.H265, nal));
        return types;
    }

    [Fact]
    public void Should_Assemble_A_Camera_Key_Frame_Despite_Its_Marker_And_Fragment_Quirks()
    {
        var depacketizer = new RtpVideoDepacketizer(VideoCodec.H265, PayloadType);

        depacketizer.Push(Packet(9000, false, Vps));
        depacketizer.Push(Packet(9000, false, Sps));
        // The camera sets the marker after the PPS: no picture yet, so nothing may complete here.
        depacketizer.Push(Packet(9000, true, Pps));
        depacketizer.CompletedCount.ShouldBe(0);
        depacketizer.Push(Packet(9000, false, Fu(true, false, 19, 0xaf, 0x01)));
        // Continuations say type 38; the unit's type is the start fragment's (19, IDR_W_RADL).
        depacketizer.Push(Packet(9000, false, Fu(false, false, 38, 0x02, 0x03)));
        depacketizer.Push(Packet(9000, true, Fu(false, true, 38, 0x04)));

        depacketizer.CompletedCount.ShouldBe(1);
        var unit = depacketizer.Dequeue();
        unit.IsKeyFrame.ShouldBeTrue();
        Types(unit).ShouldBe([32, 33, 34, 19]);
        var slice = unit.Data.Span[^7..].ToArray();
        slice.ShouldBe(new byte[] { 0x26, 0x01, 0xaf, 0x01, 0x02, 0x03, 0x04 });
    }

    [Fact]
    public void Should_End_An_Access_Unit_When_The_Timestamp_Changes()
    {
        var depacketizer = new RtpVideoDepacketizer(VideoCodec.H265, PayloadType);

        // A delta slice (TRAIL_R, type 1) without a marker, then the next picture.
        depacketizer.Push(Packet(0, false, 0x02, 0x01, 0xd0));
        depacketizer.Push(Packet(9000, false, 0x02, 0x01, 0xd1));

        depacketizer.CompletedCount.ShouldBe(1);
        var first = depacketizer.Dequeue();
        first.IsKeyFrame.ShouldBeFalse();
        first.Timestamp.ShouldBe(TimeSpan.Zero);
        depacketizer.Push(Packet(18000, false, 0x02, 0x01, 0xd2));
        depacketizer.Dequeue().Timestamp.ShouldBe(TimeSpan.FromSeconds(.1));
    }

    [Fact]
    public void Should_Drop_Only_The_Fragment_A_Lost_Packet_Cuts()
    {
        var depacketizer = new RtpVideoDepacketizer(VideoCodec.H265, PayloadType);

        depacketizer.Push(Packet(0, false, Fu(true, false, 1, 0xaa)));
        _sequence++; // lost: the middle of the fragment
        depacketizer.Push(Packet(0, false, Fu(false, true, 1, 0xcc)));
        depacketizer.Push(Packet(0, true, 0x02, 0x01, 0xee));

        depacketizer.LostPackets.ShouldBe(1);
        var unit = depacketizer.Dequeue();
        // The broken fragment is gone; the complete slice after it is kept.
        unit.Data.ToArray().ShouldBe(new byte[] { 0, 0, 0, 1, 0x02, 0x01, 0xee });
    }

    [Fact]
    public void Should_Put_Sdp_Parameter_Sets_In_Front_Of_A_Key_Frame_Without_Them()
    {
        var depacketizer = new RtpVideoDepacketizer(VideoCodec.H265, PayloadType, sdpParameterSets: [Vps, Sps, Pps]);

        depacketizer.Push(Packet(0, true, 0x26, 0x01, 0xaf));

        var unit = depacketizer.Dequeue();
        Types(unit).ShouldBe([32, 33, 34, 19]);
        NalUnits.GetParameterSets(VideoCodec.H265, unit.Data.Span).Count.ShouldBe(3);
    }

    [Fact]
    public void Should_Unpack_H264_Aggregation_And_Fragmentation_Packets()
    {
        var depacketizer = new RtpVideoDepacketizer(VideoCodec.H264, PayloadType);

        // STAP-A with SPS (7) and PPS (8), then an IDR (5) in two FU-A fragments.
        depacketizer.Push(Packet(0, false, 0x18, 0x00, 0x03, 0x67, 0x42, 0x1f, 0x00, 0x02, 0x68, 0xce));
        depacketizer.Push(Packet(0, false, 0x7c, 0x85, 0x88, 0x84));
        depacketizer.Push(Packet(0, true, 0x7c, 0x45, 0x21));

        var unit = depacketizer.Dequeue();
        unit.IsKeyFrame.ShouldBeTrue();
        unit.Data.ToArray().ShouldBe(new byte[]
        {
            0, 0, 0, 1, 0x67, 0x42, 0x1f,
            0, 0, 0, 1, 0x68, 0xce,
            0, 0, 0, 1, 0x65, 0x88, 0x84, 0x21,
        });
    }

    [Fact]
    public void Should_Ignore_Other_Payload_Types()
    {
        var depacketizer = new RtpVideoDepacketizer(VideoCodec.H265, PayloadType);
        var audio = Packet(0, true, 0x02, 0x01, 0xd0);
        audio[1] = 0x80 | 104;

        depacketizer.Push(audio);

        depacketizer.IgnoredPackets.ShouldBe(1);
        depacketizer.CompletedCount.ShouldBe(0);
    }

    [Fact]
    public void Should_Recycle_Access_Unit_Buffers()
    {
        var depacketizer = new RtpVideoDepacketizer(VideoCodec.H265, PayloadType);
        depacketizer.Push(Packet(0, true, 0x02, 0x01, 0xd0));
        var first = depacketizer.Dequeue();
        depacketizer.Recycle(first);

        depacketizer.Push(Packet(3000, true, 0x02, 0x01, 0xd1));
        depacketizer.Push(Packet(6000, true, 0x02, 0x01, 0xd2));

        // Steady state: the units cycle through the same few buffers.
        var second = depacketizer.Dequeue();
        var third = depacketizer.Dequeue();
        new[] { second.Buffer, third.Buffer }.ShouldContain(first.Buffer);
    }
}
