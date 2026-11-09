using System.IO.Compression;
using Miko.Components.Vnc;
using Miko.Components.Vnc.Protocol;
using Miko.Platform;
using Shouldly;

namespace Miko.Components.Tests.Vnc;

/// <summary>The managed building blocks: zlib inflater, DES (VNC authentication), keysyms, addresses.</summary>
public class VncPrimitivesTests
{
    // ---- inflater: against the BCL's zlib, fed piece by piece as VNC servers send it ------------

    [Theory]
    [InlineData(CompressionLevel.NoCompression)]   // stored blocks
    [InlineData(CompressionLevel.Fastest)]         // mostly fixed Huffman
    [InlineData(CompressionLevel.SmallestSize)]    // dynamic Huffman, long matches
    public void Inflater_Should_Decode_A_Persistent_Stream_Sent_In_Flushed_Pieces(CompressionLevel level)
    {
        var rng = new Random(7);
        var compressed = new MemoryStream();
        using var deflate = new ZLibStream(compressed, level, leaveOpen: true);
        var inflater = new ZlibInflater();
        long sent = 0;
        for (int piece = 0; piece < 40; piece++)
        {
            var data = Sample(rng, rng.Next(0, piece % 7 == 0 ? 120_000 : 3_000));
            deflate.Write(data);
            deflate.Flush();   // Z_SYNC_FLUSH: what servers do at the end of each rectangle
            var chunk = compressed.ToArray()[(int)sent..];
            sent = compressed.Length;
            // Back-references reach into earlier pieces: the 32 KB history must carry over.
            inflater.Inflate(chunk).ToArray().ShouldBe(data, $"piece {piece}");
        }
    }

    [Fact]
    public void Inflater_Should_Finish_A_Block_Split_Across_Two_Feeds()
    {
        var rng = new Random(3);
        var data = Sample(rng, 50_000);
        var compressed = new MemoryStream();
        using (var z = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true)) { z.Write(data); z.Flush(); }
        var bytes = compressed.ToArray();
        for (int cut = 1; cut < bytes.Length; cut += bytes.Length / 23)
        {
            var inflater = new ZlibInflater();
            var first = inflater.Inflate(bytes.AsSpan(0, cut)).ToArray();
            var second = inflater.Inflate(bytes.AsSpan(cut)).ToArray();
            first.Concat(second).ToArray().ShouldBe(data, $"cut at {cut}");
        }
    }

    [Fact]
    public void Inflater_Should_Start_Over_After_Reset()
    {
        var inflater = new ZlibInflater();
        var a = Compress([1, 2, 3, 1, 2, 3, 1, 2, 3]);
        var b = Compress([9, 9, 9, 9]);
        inflater.Inflate(a).ToArray().ShouldBe(new byte[] { 1, 2, 3, 1, 2, 3, 1, 2, 3 });
        inflater.Reset();
        // A second stream starts with its own zlib header.
        inflater.Inflate(b).ToArray().ShouldBe(new byte[] { 9, 9, 9, 9 });
    }

    [Fact]
    public void Inflater_Should_Reject_Garbage()
    {
        var inflater = new ZlibInflater();
        Should.Throw<InvalidDataException>(() => inflater.Inflate(new byte[] { 0x12, 0x34, 0x56 }).ToArray());
    }

    private static byte[] Compress(byte[] data)
    {
        var ms = new MemoryStream();
        using (var z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) { z.Write(data); z.Flush(); }
        return ms.ToArray();
    }

    // Screen-like data: runs, repeated rows, noise.
    private static byte[] Sample(Random rng, int length)
    {
        var data = new byte[length];
        int kind = rng.Next(3);
        for (int i = 0; i < length; i++)
            data[i] = kind switch
            {
                0 => (byte)rng.Next(256),
                1 => (byte)((i / 64) % 5 * 40),
                _ => (byte)(rng.Next(8) == 0 ? rng.Next(256) : (i % 1920) / 8),
            };
        return data;
    }

    // ---- DES / VNC authentication ----------------------------------------------------------

    [Fact]
    public void Des_Should_Match_The_Fips_Test_Vector()
    {
        var output = new byte[8];
        VncDes.Encrypt(Convert.FromHexString("133457799BBCDFF1"), Convert.FromHexString("0123456789ABCDEF"), output);
        Convert.ToHexString(output).ShouldBe("85E813540F0AB405");
    }

    // Expected responses computed with `openssl enc -des-ecb` and the bit-mirrored key.
    [Theory]
    [InlineData("Pa$$w0rd", "3FB75707949C6C39CD4133F30A1E15D8")]
    [InlineData("abc", "9C22B4F2088C3465A1562C4B9D6EDB04")]                 // zero-padded
    [InlineData("longpassword", "5931256585FD62106D317E09FC963BAF")]        // cut to 8 characters
    public void Should_Answer_The_Vnc_Challenge(string password, string expected)
    {
        var challenge = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        Convert.ToHexString(VncDes.EncryptChallenge(password, challenge)).ShouldBe(expected);
    }

    // ---- keysyms -------------------------------------------------------------------

    [Theory]
    [InlineData(MikoKey.Enter, 0xFF0D)]
    [InlineData(MikoKey.Backspace, 0xFF08)]
    [InlineData(MikoKey.Escape, 0xFF1B)]
    [InlineData(MikoKey.Up, 0xFF52)]
    [InlineData(MikoKey.F5, 0xFFC2)]
    [InlineData(MikoKey.ControlLeft, 0xFFE3)]
    [InlineData(MikoKey.MetaLeft, 0xFFEB)]
    [InlineData(MikoKey.C, 'c')]
    [InlineData(MikoKey.D7, '7')]
    [InlineData(MikoKey.Slash, '/')]
    [InlineData(MikoKey.KeypadAdd, '+')]
    public void Should_Map_Keys_To_Keysyms(MikoKey key, uint keysym) => VncKeys.FromKey(key).ShouldBe(keysym);

    [Theory]
    [InlineData('a', 0x61)]
    [InlineData('é', 0xE9)]                 // Latin-1: its own keysym
    [InlineData('中', 0x01004E2D)]          // beyond: 0x01000000 + code point
    [InlineData('\n', 0xFF0D)]
    [InlineData(0x1F600, 0x0101F600)]       // outside the BMP
    public void Should_Map_Characters_To_Keysyms(int codePoint, uint keysym) => VncKeys.FromChar(codePoint).ShouldBe(keysym);

    [Fact]
    public void Shifted_Us_Characters_Follow_The_Layout()
    {
        VncKeys.UsCharacter(MikoKey.D2, shift: true).ShouldBe('@');
        VncKeys.UsCharacter(MikoKey.A, shift: true).ShouldBe('A');
        VncKeys.UsCharacter(MikoKey.F1).ShouldBeNull();
    }

    // ---- addresses -----------------------------------------------------------------

    [Theory]
    [InlineData("192.168.1.10", "192.168.1.10", 5900)]
    [InlineData("desktop:1", "desktop", 5901)]              // display number
    [InlineData("desktop:5905", "desktop", 5905)]           // a port
    [InlineData("desktop::5900", "desktop", 5900)]
    [InlineData("desktop::22", "desktop", 22)]              // :: is always a port
    [InlineData("[fe80::1]:2", "fe80::1", 5902)]
    [InlineData("[fe80::1]::6000", "fe80::1", 6000)]
    [InlineData("vnc://host:3/", "host", 5903)]
    [InlineData("  host  ", "host", 5900)]
    public void Should_Parse_Vnc_Addresses(string text, string host, int port)
    {
        VncAddress.TryParse(text, out var h, out var p).ShouldBeTrue();
        h.ShouldBe(host);
        p.ShouldBe(port);
    }

    [Theory]
    [InlineData("")]
    [InlineData(":1")]
    [InlineData("host:abc")]
    [InlineData("host:99999")]
    [InlineData("[::1")]
    public void Should_Reject_Bad_Addresses(string text) => VncAddress.TryParse(text, out _, out _).ShouldBeFalse();

    [Fact]
    public void Should_Format_Addresses_For_The_Form()
    {
        VncAddress.Format("host", 5900).ShouldBe("host");
        VncAddress.Format("host", 5901).ShouldBe("host::5901");
        VncAddress.Format("fe80::1", 5900).ShouldBe("[fe80::1]");
    }
}
