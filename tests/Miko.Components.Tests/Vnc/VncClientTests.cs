using Miko.Components.Vnc.Protocol;
using Shouldly;

namespace Miko.Components.Tests.Vnc;

/// <summary>
/// The RFB client against a scripted server on loopback: handshake and security, every encoding (the
/// expected pixels are built independently by the test), pseudo-encodings and the input messages.
/// </summary>
public sealed class VncClientTests : IDisposable
{
    private readonly FakeVncServer _server = new();
    private VncClient? _client;

    public void Dispose()
    {
        _client?.Dispose();
        _server.Dispose();
    }

    private async Task<VncClient> Connect(Func<VncConnectOptions, VncConnectOptions>? configure = null)
    {
        var options = new VncConnectOptions("127.0.0.1", _server.Port) { Password = "secret", ConnectTimeout = TimeSpan.FromSeconds(5) };
        if (configure != null) options = configure(options);
        _client = await VncClient.ConnectAsync(options);
        _server.HandshakeError.ShouldBeNull();
        return _client;
    }

    private async Task<VncClient> Started(Func<VncConnectOptions, VncConnectOptions>? configure = null)
    {
        var client = await Connect(configure);
        client.Start();
        _server.Next(m => m.Kind == "Update" && !m.Incremental).ShouldNotBeNull("the first request asks for the whole screen");
        return client;
    }

    // Sends an update and waits until the client has decoded it.
    private void Apply(VncClient client, params (int, int, int, int, int, byte[])[] rects)
    {
        long before = client.UpdateCount;
        _server.Update(rects);
        Wait(() => client.UpdateCount > before);
    }

    private static void Wait(Func<bool> condition, int ms = 5000)
    {
        var until = Environment.TickCount64 + ms;
        while (!condition())
        {
            if (Environment.TickCount64 > until) throw new TimeoutException("condition not met");
            Thread.Sleep(5);
        }
    }

    private static (byte R, byte G, byte B) At(VncFramebuffer fb, int x, int y)
    {
        int o = (y * fb.Width + x) * 4;
        fb.Pixels[o + 3].ShouldBe((byte)255, "the X byte is made opaque");
        return (fb.Pixels[o + 2], fb.Pixels[o + 1], fb.Pixels[o]);
    }

    // ---- handshake -----------------------------------------------------------------

    [Fact]
    public async Task Should_Authenticate_With_The_Vnc_Challenge_And_Read_ServerInit()
    {
        var client = await Connect();

        _server.Response.ShouldBe(VncDes.EncryptChallenge("secret", _server.Challenge));
        client.DesktopName.ShouldBe("fake-desktop");
        client.Framebuffer.Width.ShouldBe(64);
        client.Framebuffer.Height.ShouldBe(32);
        client.ProtocolVersion.ShouldBe("3.8");
        client.SecurityType.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Report_A_Rejected_Password()
    {
        var ex = await Should.ThrowAsync<VncAuthenticationException>(() => Connect(o => o with { Password = "wrong" }));
        ex.PasswordRejected.ShouldBeTrue();
        ex.Message.ShouldBe("Authentication failed");   // the server's reason (3.8)
    }

    [Fact]
    public async Task Should_Ask_For_The_Password_Only_When_The_Server_Wants_One()
    {
        int asked = 0;
        var client = await Connect(o => o with { Password = null, PasswordProvider = _ => { asked++; return Task.FromResult<string?>("secret"); } });
        asked.ShouldBe(1);
        client.SecurityType.ShouldBe(2);

        await Should.ThrowAsync<VncPasswordRequiredException>(() =>
            VncClient.ConnectAsync(new VncConnectOptions("127.0.0.1", _server.Port) { PasswordProvider = _ => Task.FromResult<string?>(null) }));
    }

    [Fact]
    public async Task Should_Speak_Rfb_3_3_Without_Security()
    {
        _server.Version = "RFB 003.003\n";
        _server.SecurityTypes = [1];
        var client = await Connect(o => o with { Password = null });
        client.ProtocolVersion.ShouldBe("3.3");
        client.SecurityType.ShouldBe(1);
    }

    [Theory]
    [InlineData("RFB 003.889\n", "3.8")]   // Apple Screen Sharing
    [InlineData("RFB 004.001\n", "3.8")]   // RealVNC 4.x and later
    [InlineData("RFB 003.007\n", "3.7")]
    public async Task Should_Answer_Newer_Servers_With_3_8(string version, string spoken)
    {
        _server.Version = version;
        var client = await Connect();
        client.ProtocolVersion.ShouldBe(spoken);
    }

    [Fact]
    public async Task Should_Prefer_No_Security_When_Offered()
    {
        _server.SecurityTypes = [2, 1];
        var client = await Connect(o => o with { Password = null });
        client.SecurityType.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Name_Unsupported_Security()
    {
        _server.SecurityTypes = [19, 30];
        var ex = await Should.ThrowAsync<VncAuthenticationException>(() => Connect());
        ex.Message.ShouldContain("VeNCrypt");
        ex.Message.ShouldContain("Apple Remote Desktop");
        ex.PasswordRejected.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Offer_Jpeg_Only_With_A_Decoder()
    {
        var options = new VncConnectOptions("127.0.0.1", _server.Port) { Password = "secret" };
        using var plain = await VncClient.ConnectAsync(options);
        plain.Start();
        var encodings = _server.Next(m => m.Kind == "SetEncodings")!.Encodings!;
        encodings[0].ShouldBe(VncEncoding.CopyRect);
        encodings.ShouldContain(VncEncoding.Tight);
        encodings.ShouldContain(VncEncoding.Cursor);
        encodings.ShouldNotContain(VncEncoding.JpegQuality(7));

        using var server2 = new FakeVncServer();
        using var withJpeg = await VncClient.ConnectAsync(options with { Port = server2.Port, JpegDecoder = new FillJpeg(0, 0, 0) });
        withJpeg.Start();
        server2.Next(m => m.Kind == "SetEncodings")!.Encodings!.ShouldContain(VncEncoding.JpegQuality(7));
    }

    // ---- encodings -----------------------------------------------------------------

    [Fact]
    public async Task Should_Decode_Raw()
    {
        var client = await Started();
        Apply(client, (1, 1, 4, 2, VncEncoding.Raw, FakeVncServer.RawPixels(4, 2, (x, y) => ((byte)(x * 50), (byte)(y * 100), 7))));
        At(client.Framebuffer, 1, 1).ShouldBe(((byte)0, (byte)0, (byte)7));
        At(client.Framebuffer, 4, 2).ShouldBe(((byte)150, (byte)100, (byte)7));
        At(client.Framebuffer, 0, 0).ShouldBe(((byte)0, (byte)0, (byte)0));
    }

    [Fact]
    public async Task Should_Decode_CopyRect_With_Overlap()
    {
        var client = await Started();
        Apply(client, (0, 0, 8, 1, VncEncoding.Raw, FakeVncServer.RawPixels(8, 1, (x, _) => ((byte)(x * 10), 0, 0))));
        // Shift the first six pixels two to the right (source and destination overlap).
        Apply(client, (2, 0, 6, 1, VncEncoding.CopyRect, [0, 0, 0, 0]));
        Enumerable.Range(0, 8).Select(x => At(client.Framebuffer, x, 0).R).ShouldBe(new byte[] { 0, 10, 0, 10, 20, 30, 40, 50 });
    }

    [Fact]
    public async Task Should_Decode_Rre()
    {
        var client = await Started();
        var payload = new MemoryStream();
        payload.Write([0, 0, 0, 1]);                              // one subrectangle
        payload.Write(FakeVncServer.Pixel(0, 0, 255));            // background blue
        payload.Write(FakeVncServer.Pixel(255, 0, 0));            // red
        payload.Write([0, 2, 0, 1, 0, 3, 0, 2]);                  // x 2, y 1, 3 × 2
        Apply(client, (10, 10, 8, 4, VncEncoding.Rre, payload.ToArray()));
        At(client.Framebuffer, 10, 10).ShouldBe(((byte)0, (byte)0, (byte)255));
        At(client.Framebuffer, 12, 11).ShouldBe(((byte)255, (byte)0, (byte)0));
        At(client.Framebuffer, 14, 12).ShouldBe(((byte)255, (byte)0, (byte)0));
        At(client.Framebuffer, 15, 12).ShouldBe(((byte)0, (byte)0, (byte)255));
    }

    [Fact]
    public async Task Should_Decode_Hextile_Raw_Background_And_Subrects()
    {
        var client = await Started();
        var p = new MemoryStream();
        // Tile (0,0) 16×16: background green, foreground white, one subrect at (4,4) 2×3.
        p.WriteByte(2 | 4 | 8);
        p.Write(FakeVncServer.Pixel(0, 200, 0));
        p.Write(FakeVncServer.Pixel(255, 255, 255));
        p.Write([1, 0x44, 0x12]);
        // Tile (16,0) 4×16: raw.
        p.WriteByte(1);
        p.Write(FakeVncServer.RawPixels(4, 16, (x, y) => ((byte)x, (byte)y, 9)));
        // Tile (0,16) 16×4: background kept from before, two coloured subrects.
        p.WriteByte(8 | 16);
        p.Write([2]);
        p.Write(FakeVncServer.Pixel(255, 0, 0)); p.Write([0x00, 0x00]);
        p.Write(FakeVncServer.Pixel(0, 0, 255)); p.Write([0xF3, 0x00]);
        // Tile (16,16) 4×4: nothing set, the background again.
        p.WriteByte(0);
        Apply(client, (0, 0, 20, 20, VncEncoding.Hextile, p.ToArray()));

        var fb = client.Framebuffer;
        At(fb, 0, 0).ShouldBe(((byte)0, (byte)200, (byte)0));
        At(fb, 4, 4).ShouldBe(((byte)255, (byte)255, (byte)255));
        At(fb, 5, 6).ShouldBe(((byte)255, (byte)255, (byte)255));
        At(fb, 6, 4).ShouldBe(((byte)0, (byte)200, (byte)0));
        At(fb, 19, 15).ShouldBe(((byte)3, (byte)15, (byte)9));
        At(fb, 0, 16).ShouldBe(((byte)255, (byte)0, (byte)0));
        At(fb, 1, 16).ShouldBe(((byte)0, (byte)200, (byte)0));
        At(fb, 15, 19).ShouldBe(((byte)0, (byte)0, (byte)255));
        At(fb, 18, 18).ShouldBe(((byte)0, (byte)200, (byte)0));
    }

    [Fact]
    public async Task Should_Decode_Zrle_Subencodings_Over_One_Stream()
    {
        _server.Width = 80;
        _server.Height = 10;
        var client = await Started();
        var server = _server;
        // First rectangle, 80 × 3: two tiles across (64 + 16). Tile 1 solid, tile 2 raw.
        var t = new MemoryStream();
        t.WriteByte(1); t.Write(Cp(10, 20, 30));
        t.WriteByte(0);
        for (int y = 0; y < 3; y++) for (int x = 0; x < 16; x++) t.Write(Cp((byte)x, (byte)y, 0));
        Apply(client, (0, 0, 80, 3, VncEncoding.Zrle, server.Zrle(t.ToArray())));
        At(client.Framebuffer, 63, 2).ShouldBe(((byte)10, (byte)20, (byte)30));
        At(client.Framebuffer, 79, 2).ShouldBe(((byte)15, (byte)2, (byte)0));

        // Second rectangle on the same stream: packed palette (2 colours, 1 bit), plain RLE, palette RLE.
        var u = new MemoryStream();
        u.WriteByte(2); u.Write(Cp(255, 0, 0)); u.Write(Cp(0, 0, 255));
        u.Write([0b1010_0000, 0b0101_0000]);                                  // 4 × 2: rows 1010, 0101
        Apply(client, (0, 4, 4, 2, VncEncoding.Zrle, server.Zrle(u.ToArray())));
        var fb = client.Framebuffer;
        At(fb, 0, 4).ShouldBe(((byte)0, (byte)0, (byte)255));
        At(fb, 1, 4).ShouldBe(((byte)255, (byte)0, (byte)0));
        At(fb, 0, 5).ShouldBe(((byte)255, (byte)0, (byte)0));

        var r = new MemoryStream();
        r.WriteByte(128); r.Write(Cp(1, 2, 3)); r.WriteByte(2); r.Write(Cp(4, 5, 6)); r.WriteByte(4);   // runs of 3 and 5
        Apply(client, (0, 8, 8, 1, VncEncoding.Zrle, server.Zrle(r.ToArray())));
        At(fb, 2, 8).ShouldBe(((byte)1, (byte)2, (byte)3));
        At(fb, 3, 8).ShouldBe(((byte)4, (byte)5, (byte)6));
    }

    [Fact]
    public async Task Should_Drop_The_Connection_On_A_Zrle_Run_Longer_Than_Its_Tile()
    {
        var client = await Started();
        var failed = new TaskCompletionSource<Exception?>();
        client.Disconnected += ex => failed.TrySetResult(ex);
        var pr = new MemoryStream();
        pr.WriteByte(130); pr.Write(Cp(9, 9, 9)); pr.Write(Cp(7, 7, 7));
        pr.Write([0x80 | 1, 255, 1]);                     // a run of 1 + 255 + 1 = 257 in an 8 × 1 tile
        _server.Update((0, 0, 8, 1, VncEncoding.Zrle, _server.Zrle(pr.ToArray())));
        (await failed.Task.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeOfType<VncProtocolException>();
    }

    [Fact]
    public async Task Should_Decode_Zrle_Palette_Rle()
    {
        var client = await Started();
        var pr = new MemoryStream();
        pr.WriteByte(130); pr.Write(Cp(9, 9, 9)); pr.Write(Cp(7, 7, 7));
        pr.Write([0, 0x80 | 1, 5, 0]);                    // one 9, six 7s (a run byte n is n + 1 pixels), one 9
        Apply(client, (0, 0, 8, 1, VncEncoding.Zrle, _server.Zrle(pr.ToArray())));
        Enumerable.Range(0, 8).Select(x => At(client.Framebuffer, x, 0).R).ShouldBe(new byte[] { 9, 7, 7, 7, 7, 7, 7, 9 });
    }

    // ZRLE CPIXEL in the client format: B, G, R.
    private static byte[] Cp(byte r, byte g, byte b) => [b, g, r];

    [Fact]
    public async Task Should_Decode_Tight_Fill_Copy_Palette_Gradient_And_Resets()
    {
        var client = await Started();
        var fb = client.Framebuffer;

        // Fill: one RGB TPIXEL.
        Apply(client, (0, 0, 64, 32, VncEncoding.Tight, [0x80, 40, 50, 60]));
        At(fb, 33, 17).ShouldBe(((byte)40, (byte)50, (byte)60));

        // Basic copy, under 12 bytes: sent as is.
        Apply(client, (0, 0, 2, 1, VncEncoding.Tight, [0x00, 1, 2, 3, 4, 5, 6]));
        At(fb, 0, 0).ShouldBe(((byte)1, (byte)2, (byte)3));
        At(fb, 1, 0).ShouldBe(((byte)4, (byte)5, (byte)6));

        // Basic copy through zlib stream 0.
        var rgb = new byte[4 * 4 * 3];
        for (int i = 0; i < 16; i++) { rgb[i * 3] = (byte)(i * 10); rgb[i * 3 + 1] = 100; rgb[i * 3 + 2] = (byte)(255 - i); }
        Apply(client, (8, 8, 4, 4, VncEncoding.Tight, Tight(0x00, [], 0, rgb)));
        At(fb, 11, 11).ShouldBe(((byte)150, (byte)100, (byte)240));

        // Palette, two colours (1 bit per pixel, rows padded), on stream 1 with the filter id.
        var bits = new byte[] { 0b1000_0000, 0b0100_0000, 0b0010_0000, 0b0001_0000, 0, 0, 0, 0, 0, 0, 0, 0 };   // 4 × 12, a diagonal
        Apply(client, (20, 0, 4, 12, VncEncoding.Tight, Tight(0x40 | 0x10, [1, 1, 255, 255, 255, 0, 0, 0], 1, bits)));
        At(fb, 20, 0).ShouldBe(((byte)0, (byte)0, (byte)0));         // bit set: colour 1 (black)
        At(fb, 21, 0).ShouldBe(((byte)255, (byte)255, (byte)255));   // bit clear: colour 0 (white)
        At(fb, 23, 3).ShouldBe(((byte)0, (byte)0, (byte)0));
        At(fb, 23, 4).ShouldBe(((byte)255, (byte)255, (byte)255));

        // Palette, four colours (a byte per pixel), small enough to be sent as is.
        Apply(client, (30, 0, 3, 1, VncEncoding.Tight, [0x40, 1, 3, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 3, 0, 2]));
        At(fb, 30, 0).ShouldBe(((byte)4, (byte)4, (byte)4));
        At(fb, 31, 0).ShouldBe(((byte)1, (byte)1, (byte)1));
        At(fb, 32, 0).ShouldBe(((byte)3, (byte)3, (byte)3));

        // Gradient on stream 2: the server sends each value minus left + above - above-left.
        var image = new byte[6 * 5 * 3];
        for (int y = 0; y < 5; y++) for (int x = 0; x < 6; x++)
        {
            int o = (y * 6 + x) * 3;
            image[o] = (byte)(x * 30 + y * 5); image[o + 1] = (byte)(200 - y * 20); image[o + 2] = (byte)((x * y * 13) & 0xFF);
        }
        Apply(client, (40, 10, 6, 5, VncEncoding.Tight, Tight(0x40 | 0x20, [2], 2, GradientEncode(image, 6, 5))));
        At(fb, 45, 14).ShouldBe(((byte)(5 * 30 + 4 * 5), (byte)(200 - 80), (byte)((5 * 4 * 13) & 0xFF)));
        At(fb, 40, 10).ShouldBe(((byte)0, (byte)200, (byte)0));

        // Reset bit 0: stream 0 starts over with a new zlib header.
        Apply(client, (8, 8, 4, 4, VncEncoding.Tight, Tight(0x01, [], 0, rgb.Select(b => (byte)(255 - b)).ToArray(), reset: true)));
        At(fb, 8, 8).ShouldBe(((byte)255, (byte)155, (byte)0));
    }

    [Fact]
    public async Task Should_Hand_Tight_Jpeg_To_The_Decoder()
    {
        var jpeg = new FillJpeg(12, 34, 56);
        var client = await Started(o => o with { JpegDecoder = jpeg });
        byte[] data = [0xFF, 0xD8, 1, 2, 3, 0xFF, 0xD9];
        Apply(client, (4, 4, 10, 6, VncEncoding.Tight, [0x90, .. FakeVncServer.CompactLength(data.Length), .. data]));
        jpeg.Received.ShouldBe(data);
        jpeg.Size.ShouldBe((10, 6));
        At(client.Framebuffer, 13, 9).ShouldBe(((byte)12, (byte)34, (byte)56));
        At(client.Framebuffer, 14, 9).ShouldBe(((byte)0, (byte)0, (byte)0));
    }

    private byte[] Tight(byte control, byte[] header, int stream, byte[] data, bool reset = false)
    {
        var compressed = _server.TightCompress(stream, data, reset);
        return [control, .. header, .. FakeVncServer.CompactLength(compressed.Length), .. compressed];
    }

    private static byte[] GradientEncode(byte[] image, int w, int h)
    {
        var sent = new byte[image.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                for (int c = 0; c < 3; c++)
                {
                    int i = (y * w + x) * 3 + c;
                    int left = x > 0 ? image[i - 3] : 0, above = y > 0 ? image[i - w * 3] : 0, aboveLeft = x > 0 && y > 0 ? image[i - w * 3 - 3] : 0;
                    sent[i] = (byte)(image[i] - Math.Clamp(left + above - aboveLeft, 0, 255));
                }
        return sent;
    }

    private sealed class FillJpeg(byte r, byte g, byte b) : IVncJpegDecoder
    {
        public byte[]? Received;
        public (int, int) Size;

        public bool Decode(ReadOnlySpan<byte> jpeg, int width, int height, Span<byte> destination, int stride)
        {
            Received = jpeg.ToArray();
            Size = (width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var p = destination.Slice(y * stride + x * 4, 4);
                    p[0] = b; p[1] = g; p[2] = r; p[3] = 255;
                }
            return true;
        }
    }

    // ---- pseudo-encodings and server messages ----------------------------------------

    [Fact]
    public async Task Should_Resize_And_Ask_For_The_Whole_New_Screen()
    {
        var client = await Started();
        VncFramebuffer? resized = null;
        client.FramebufferResized += fb => resized = fb;
        Apply(client, (0, 0, 80, 40, VncEncoding.DesktopSize, []));
        resized.ShouldNotBeNull();
        client.Framebuffer.Width.ShouldBe(80);
        _server.Next(m => m.Kind == "Update" && !m.Incremental && m.Width == 80 && m.Height == 40).ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Read_The_Pointer_Shape_With_Its_Mask()
    {
        var client = await Started();
        VncCursorShape? shape = null;
        client.CursorChanged += s => shape = s;
        var pixels = FakeVncServer.RawPixels(2, 2, (x, y) => (255, (byte)(x * 100), (byte)(y * 100)));
        Apply(client, (1, 0, 2, 2, VncEncoding.Cursor, [.. pixels, 0b1100_0000, 0b1000_0000]));
        shape.ShouldNotBeNull();
        (shape.HotspotX, shape.HotspotY, shape.Width, shape.Height).ShouldBe((1, 0, 2, 2));
        Enumerable.Range(0, 4).Select(i => shape.Pixels[i * 4 + 3]).ShouldBe(new byte[] { 255, 255, 255, 0 });
        shape.Pixels[4 * 1 + 1].ShouldBe((byte)100);   // pixel (1, 0): B, G, R → green 100
    }

    [Fact]
    public async Task Should_End_An_Update_At_LastRect()
    {
        var client = await Started();
        long before = client.UpdateCount;
        _server.UpdateWithCount(0xFFFF,
            (0, 0, 1, 1, VncEncoding.Raw, FakeVncServer.Pixel(9, 8, 7)),
            (0, 0, 0, 0, VncEncoding.LastRect, []));
        Wait(() => client.UpdateCount > before);
        Apply(client, (1, 0, 1, 1, VncEncoding.Raw, FakeVncServer.Pixel(1, 2, 3)));
        At(client.Framebuffer, 0, 0).ShouldBe(((byte)9, (byte)8, (byte)7));
        At(client.Framebuffer, 1, 0).ShouldBe(((byte)1, (byte)2, (byte)3));
    }

    [Fact]
    public async Task Should_Receive_The_Server_Clipboard()
    {
        var client = await Started();
        string? text = null;
        client.ClipboardReceived += t => text = t;
        _server.CutText("copied on the server");
        Wait(() => text != null);
        text.ShouldBe("copied on the server");
    }

    [Fact]
    public async Task Should_Report_The_Server_Closing_And_Protocol_Errors()
    {
        var client = await Started();
        var closed = new TaskCompletionSource<Exception?>();
        client.Disconnected += ex => closed.TrySetResult(ex);
        _server.Drop();
        (await closed.Task.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeNull("a clean close has no failure");

        var second = await Started();
        var failed = new TaskCompletionSource<Exception?>();
        second.Disconnected += ex => failed.TrySetResult(ex);
        // A rectangle outside the screen must not write outside the framebuffer.
        _server.Update((60, 0, 10, 1, VncEncoding.Raw, new byte[40]));
        (await failed.Task.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBeOfType<VncProtocolException>();
    }

    // ---- input ---------------------------------------------------------------------

    [Fact]
    public async Task Should_Send_Keys_Pointer_And_Clipboard()
    {
        var client = await Started();
        _server.Drain(100);
        client.SendKey(VncKeys.Return, true);
        client.SendKey(VncKeys.Return, false);
        client.SendPointer(10, 20, 4);
        client.SendPointer(500, -3, 0);     // clamped to the screen
        client.SendClipboard("hello\r\nworld");

        _server.Next(m => m.Kind == "Key").ShouldBe(new ClientMessage("Key", Down: true, Keysym: 0xFF0D));
        _server.Next(m => m.Kind == "Key").ShouldBe(new ClientMessage("Key", Down: false, Keysym: 0xFF0D));
        _server.Next(m => m.Kind == "Pointer").ShouldBe(new ClientMessage("Pointer", Buttons: 4, X: 10, Y: 20));
        _server.Next(m => m.Kind == "Pointer").ShouldBe(new ClientMessage("Pointer", Buttons: 0, X: 63, Y: 0));
        _server.Next(m => m.Kind == "CutText")!.Text.ShouldBe("hello\nworld");
    }

    [Fact]
    public async Task Should_Send_A_Press_Where_It_Happened_Even_If_Moves_Follow_At_Once()
    {
        var client = await Started();
        _server.Drain(100);
        client.SendPointer(10, 10, 1);      // press here…
        client.SendPointer(20, 10, 1);      // …then drag
        client.SendPointer(30, 10, 1);
        client.SendPointer(30, 10, 0);
        var pointers = new List<ClientMessage>();
        while (pointers.LastOrDefault()?.Buttons != 0 && _server.Next(m => m.Kind == "Pointer", 1000) is { } m) pointers.Add(m);
        pointers[0].ShouldBe(new ClientMessage("Pointer", Buttons: 1, X: 10, Y: 10));
        pointers[^2].ShouldBe(new ClientMessage("Pointer", Buttons: 1, X: 30, Y: 10));
        pointers[^1].ShouldBe(new ClientMessage("Pointer", Buttons: 0, X: 30, Y: 10));
    }

    [Fact]
    public async Task Should_Coalesce_Moves_But_Never_Lose_A_Button_Change()
    {
        var client = await Started();
        _server.Drain(100);
        for (int x = 0; x < 60; x++) client.SendPointer(x, 1, 0);
        client.SendPointer(59, 1, 1);
        client.SendPointer(59, 1, 0);

        var pointers = new List<ClientMessage>();
        while (_server.Next(m => m.Kind == "Pointer", 1000) is { } m)
        {
            pointers.Add(m);
            if (m.Buttons == 0 && pointers.Count >= 2 && pointers[^2].Buttons == 1) break;
        }
        pointers.Count.ShouldBeLessThanOrEqualTo(62);
        pointers[^3].ShouldBe(new ClientMessage("Pointer", Buttons: 0, X: 59, Y: 1));
        pointers[^2].ShouldBe(new ClientMessage("Pointer", Buttons: 1, X: 59, Y: 1));
        pointers[^1].ShouldBe(new ClientMessage("Pointer", Buttons: 0, X: 59, Y: 1));
        pointers.Select(p => p.X).ShouldBeInOrder();
    }
}
