using Miko.Fonts;
using Shouldly;

namespace Miko.Tests.Fonts;

public class Woff2DecoderTests
{
    [Fact]
    public void IsWoff2_ValidWoff2Data_ShouldReturnTrue()
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Fonts", "bootstrap-icons.woff2");

        if (!File.Exists(fontPath))
        {
            return;
        }

        byte[] woff2Data = File.ReadAllBytes(fontPath);

        Woff2Decoder.IsWoff2(woff2Data).ShouldBeTrue();
    }

    [Fact]
    public void IsWoff2_TtfSignature_ShouldReturnFalse()
    {
        // TTF signature: 0x00010000
        byte[] ttfData = new byte[] { 0x00, 0x01, 0x00, 0x00 };

        Woff2Decoder.IsWoff2(ttfData).ShouldBeFalse();
    }

    [Fact]
    public void IsWoff2_WoffSignature_ShouldReturnFalse()
    {
        // WOFF signature: 'wOFF'
        byte[] woffData = new byte[] { 0x77, 0x4F, 0x46, 0x46 };

        Woff2Decoder.IsWoff2(woffData).ShouldBeFalse();
    }

    [Fact]
    public void IsWoff2_EmptyData_ShouldReturnFalse()
    {
        byte[] emptyData = Array.Empty<byte>();

        Woff2Decoder.IsWoff2(emptyData).ShouldBeFalse();
    }

    [Fact]
    public void IsWoff2_NullData_ShouldReturnFalse()
    {
        byte[]? nullData = null;
        Woff2Decoder.IsWoff2(nullData!).ShouldBeFalse();
    }

    [Fact]
    public void IsWoff2_ShortData_ShouldReturnFalse()
    {
        byte[] shortData = new byte[] { 0x77, 0x4F };

        Woff2Decoder.IsWoff2(shortData).ShouldBeFalse();
    }

    [Fact]
    public void Decode_ValidWoff2_ShouldReturnTtfData()
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "Fonts", "bootstrap-icons.woff2");

        if (!File.Exists(fontPath))
        {
            return;
        }

        var decoder = new Woff2Decoder();
        byte[] woff2Data = File.ReadAllBytes(fontPath);

        byte[] ttfData = decoder.Decode(woff2Data);

        ttfData.ShouldNotBeNull();
        ttfData.Length.ShouldBeGreaterThan(0);

        // Verify it's valid TTF/OTF by checking signature
        // TTF signature: 0x00010000 or 'OTTO' for OTF (0x4F54544F)
        uint signature = (uint)((ttfData[0] << 24) | (ttfData[1] << 16) | (ttfData[2] << 8) | ttfData[3]);
        bool isValidFont = signature == 0x00010000 || signature == 0x4F54544F || signature == 0x74727565; // 'true'

        isValidFont.ShouldBeTrue($"Invalid font signature: 0x{signature:X8}");
    }

    [Fact]
    public void Decode_InvalidData_ShouldThrowException()
    {
        var decoder = new Woff2Decoder();
        byte[] invalidData = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        Should.Throw<InvalidDataException>(() => decoder.Decode(invalidData));
    }

    [Fact]
    public void Decode_EmptyData_ShouldThrowException()
    {
        var decoder = new Woff2Decoder();
        byte[] emptyData = Array.Empty<byte>();

        Should.Throw<ArgumentException>(() => decoder.Decode(emptyData));
    }
}
