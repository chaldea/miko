using Miko.Platform.Video;
using Shouldly;

namespace Miko.Tests.Platform.Video;

public class NalUnitsTests
{
    // An H.265 key frame as the Ezviz cameras send it: VPS, SPS, PPS, then an IDR_N_LP slice (type 20).
    private static readonly byte[] HevcKeyFrame =
    [
        0, 0, 0, 1, 0x40, 0x01, 0x0c, 0x01,        // VPS (32)
        0, 0, 0, 1, 0x42, 0x01, 0x01, 0x01, 0x60,  // SPS (33)
        0, 0, 1, 0x44, 0x01, 0xc0, 0xf2,           // PPS (34), 3-byte start code
        0, 0, 0, 1, 0x28, 0x01, 0xaf, 0x00, 0x10,  // IDR_N_LP slice (20), a zero byte inside
    ];

    private static List<byte[]> Units(ReadOnlySpan<byte> data)
    {
        var units = new List<byte[]>();
        foreach (var nal in NalUnits.EnumerateAnnexB(data)) units.Add(nal.ToArray());
        return units;
    }

    [Fact]
    public void Should_Split_On_Three_And_Four_Byte_Start_Codes()
    {
        var units = Units(HevcKeyFrame);

        units.Select(u => NalUnits.GetType(VideoCodec.H265, u)).ShouldBe([32, 33, 34, 20]);
        // A 4-byte start code's leading zero is not the end of the previous unit; a zero inside a unit stays.
        units[1].ShouldBe(new byte[] { 0x42, 0x01, 0x01, 0x01, 0x60 });
        units[3].ShouldBe(new byte[] { 0x28, 0x01, 0xaf, 0x00, 0x10 });
    }

    [Fact]
    public void Should_Read_A_Bare_Unit_Without_Start_Code_As_One_Unit()
    {
        Units([0x65, 0x88, 0x84]).Single().ShouldBe(new byte[] { 0x65, 0x88, 0x84 });
        Units([]).ShouldBeEmpty();
    }

    [Fact]
    public void Should_Return_Parameter_Sets_In_Decoder_Order()
    {
        var sets = NalUnits.GetParameterSets(VideoCodec.H265, HevcKeyFrame);

        sets.Select(s => NalUnits.GetType(VideoCodec.H265, s)).ShouldBe([32, 33, 34]);
        NalUnits.ContainsKeyFrame(VideoCodec.H265, HevcKeyFrame).ShouldBeTrue();
    }

    [Fact]
    public void Should_Return_No_Parameter_Sets_When_One_Is_Missing()
    {
        // H.265 needs the VPS too.
        byte[] withoutVps = [0, 0, 0, 1, 0x42, 0x01, 0x01, 0, 0, 0, 1, 0x44, 0x01, 0xc0];
        NalUnits.GetParameterSets(VideoCodec.H265, withoutVps).ShouldBeEmpty();

        byte[] h264 = [0, 0, 0, 1, 0x67, 0x42, 0x00, 0x1f, 0, 0, 0, 1, 0x68, 0xce, 0x3c, 0x80, 0, 0, 0, 1, 0x65, 0x88];
        NalUnits.GetParameterSets(VideoCodec.H264, h264).Select(s => s[0] & 0x1f).ShouldBe([7, 8]);
        NalUnits.ContainsKeyFrame(VideoCodec.H264, h264).ShouldBeTrue();
    }

    [Fact]
    public void Should_Write_Slices_Length_Prefixed_Without_Parameter_Sets()
    {
        var output = new byte[HevcKeyFrame.Length + 4];

        int length = NalUnits.WriteLengthPrefixed(VideoCodec.H265, HevcKeyFrame, output);

        output.AsSpan(0, length).ToArray().ShouldBe(new byte[] { 0, 0, 0, 5, 0x28, 0x01, 0xaf, 0x00, 0x10 });
    }

    [Fact]
    public void Should_Classify_Key_Frame_Slices_Per_Codec()
    {
        NalUnits.IsKeyFrameSlice(VideoCodec.H264, 5).ShouldBeTrue();
        NalUnits.IsKeyFrameSlice(VideoCodec.H264, 1).ShouldBeFalse();
        // BLA, IDR and CRA are all random access points in H.265.
        new[] { 16, 19, 20, 21 }.ShouldAllBe(t => NalUnits.IsKeyFrameSlice(VideoCodec.H265, t));
        NalUnits.IsKeyFrameSlice(VideoCodec.H265, 1).ShouldBeFalse();
        NalUnits.IsSlice(VideoCodec.H265, 32).ShouldBeFalse();
    }
}
