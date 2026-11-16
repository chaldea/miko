using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Miko.MockServer.Tests;

public sealed class ProtocolTests
{
    [Theory]
    [InlineData(3, 8, false, true)]
    [InlineData(7, 16, false, true)]
    [InlineData(8, 16, true, true)]
    [InlineData(8, 32, true, true)]
    [InlineData(8, 8, false, false)]
    public async Task Should_negotiate_rfb_versions_pixel_formats_and_partial_updates(int minor, int bits, bool bigEndian, bool trueColor)
    {
        await using var app = MockApplication.Build(["--urls=http://127.0.0.1:0", "--Mock:VncPort=0", "--Mock:RtspPort=0"]);
        await app.StartAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var token = timeout.Token;
        using var client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", app.Services.GetRequiredService<VncServer>().Port, token);
        var stream = client.GetStream();
        Encoding.ASCII.GetString(await Read(stream, 12, token)).ShouldBe("RFB 003.008\n");
        await stream.WriteAsync(Encoding.ASCII.GetBytes($"RFB 003.00{minor}\n"), token);
        if (minor == 3) (await Read(stream, 4, token)).ShouldBe(new byte[] { 0, 0, 0, 1 });
        else
        {
            (await Read(stream, 2, token)).ShouldBe(new byte[] { 1, 1 });
            await stream.WriteAsync(new byte[] { 1 }, token);
            if (minor == 8) (await Read(stream, 4, token)).ShouldBe(new byte[4]);
        }
        await stream.WriteAsync(new byte[] { 1 }, token);
        var init = await Read(stream, 24, token);
        await Read(stream, (int)BinaryPrimitives.ReadUInt32BigEndian(init.AsSpan(20)), token);

        byte[] format = bits switch
        {
            8 => [8, 8, 0, 1, 0, 7, 0, 7, 0, 3, 5, 2, 0, 0, 0, 0],
            16 => [16, 16, 0, 1, 0, 31, 0, 63, 0, 31, 11, 5, 0, 0, 0, 0],
            _ => [32, 24, 0, 1, 0, 255, 0, 255, 0, 255, 16, 8, 0, 0, 0, 0],
        };
        format[2] = bigEndian ? (byte)1 : (byte)0; format[3] = trueColor ? (byte)1 : (byte)0;
        byte[] set = new byte[20]; format.CopyTo(set, 4);
        // Deliberately fragment a message across TCP writes.
        await stream.WriteAsync(set.AsMemory(0, 7), token); await stream.WriteAsync(set.AsMemory(7), token);
        if (!trueColor)
        {
            var palette = await Read(stream, 6 + 256 * 6, token);
            palette[0].ShouldBe((byte)1);
            BinaryPrimitives.ReadUInt16BigEndian(palette.AsSpan(4)).ShouldBe((ushort)256);
        }
        await stream.WriteAsync(new byte[] { 2, 0, 0, 1, 0, 0, 0, 0 }, token); // SetEncodings Raw
        byte[] request = [3, 0, 0, 0, 0, 0, 0, 2, 0, 1];
        await stream.WriteAsync(request, token);
        var update = await Read(stream, 16 + 2 * bits / 8, token);
        BinaryPrimitives.ReadUInt16BigEndian(update.AsSpan(8)).ShouldBe((ushort)2);
        BinaryPrimitives.ReadUInt16BigEndian(update.AsSpan(10)).ShouldBe((ushort)1);
        uint expected = bits switch { 8 => trueColor ? 4u : 5u, 16 => (3u << 11) | (11u << 5) | 9u, _ => 0x1d304eu };
        uint actual = 0;
        for (int i = 0; i < bits / 8; i++) actual |= (uint)update[16 + i] << (8 * (bigEndian ? bits / 8 - i - 1 : i));
        actual.ShouldBe(expected);

        // The previous partial request must not acknowledge pixels elsewhere on the screen.
        request[1] = 1; request[3] = 3;
        await stream.WriteAsync(request, token);
        BinaryPrimitives.ReadUInt16BigEndian((await Read(stream, 16 + 2 * bits / 8, token)).AsSpan(4)).ShouldBe((ushort)3);
        byte[] text = Encoding.Latin1.GetBytes("Hello mock");
        var cut = new byte[8 + text.Length]; cut[0] = 6;
        BinaryPrimitives.WriteUInt32BigEndian(cut.AsSpan(4), (uint)text.Length); text.CopyTo(cut, 8);
        await stream.WriteAsync(cut, token);
        var echo = await Read(stream, 8 + text.Length, token);
        echo[0].ShouldBe((byte)3); echo[8..].ShouldBe(text);
        await app.StopAsync(token);
    }

    [Fact]
    public async Task Should_validate_rtsp_sessions_transport_and_pause_resume()
    {
        await using var app = MockApplication.Build(["--urls=http://127.0.0.1:0", "--Mock:VncPort=0", "--Mock:RtspPort=0"]);
        await app.StartAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        var token = timeout.Token;
        using var client = new TcpClient();
        int port = app.Services.GetRequiredService<RtspServer>().Port;
        await client.ConnectAsync("127.0.0.1", port, token);
        var stream = client.GetStream();
        string uri = $"rtsp://127.0.0.1:{port}/webcam", session = "";
        int cseq = 0;
        async Task<string> Request(string method, string headers = "")
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes($"{method} {uri} RTSP/1.0\r\nCSeq: {++cseq}\r\n{headers}\r\n"), token);
            var bytes = new List<byte>();
            while (true)
            {
                byte next = (await Read(stream, 1, token))[0];
                if (bytes.Count == 0 && next == '$')
                {
                    var header = await Read(stream, 3, token);
                    header[0].ShouldBeOneOf((byte)4, (byte)5);
                    await Read(stream, BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(1)), token); continue;
                }
                bytes.Add(next);
                if (bytes.Count >= 4 && bytes.TakeLast(4).SequenceEqual(new byte[] { 13, 10, 13, 10 })) break;
            }
            return Encoding.ASCII.GetString(bytes.ToArray());
        }
        (await Request("OPTIONS")).ShouldContain("Public: OPTIONS");
        (await Request("PLAY")).ShouldStartWith("RTSP/1.0 454");
        (await Request("SETUP", "Transport: RTP/AVP;unicast;client_port=1234-1235\r\n")).ShouldStartWith("RTSP/1.0 461");
        var setup = await Request("SETUP", "Transport: RTP/AVP/TCP;unicast;interleaved=4-5\r\n");
        setup.ShouldStartWith("RTSP/1.0 200");
        session = setup.Split("\r\n").Single(s => s.StartsWith("Session:")).Split(':')[1].Split(';')[0].Trim();
        var sessionHeader = $"Session: {session}\r\n";
        (await Request("PLAY", "Session: wrong\r\n")).ShouldStartWith("RTSP/1.0 454");
        (await Request("PLAY", sessionHeader)).ShouldContain("RTP-Info:");
        await Task.Delay(250, token);
        (await Request("PAUSE", sessionHeader)).ShouldStartWith("RTSP/1.0 200");
        (await Request("PLAY", sessionHeader)).ShouldStartWith("RTSP/1.0 200");
        (await Request("GET_PARAMETER", sessionHeader)).ShouldStartWith("RTSP/1.0 200");
        (await Request("TEARDOWN", sessionHeader)).ShouldStartWith("RTSP/1.0 200");
        (await stream.ReadAsync(new byte[1], token)).ShouldBe(0);
        await app.StopAsync(token);
    }

    private static async Task<byte[]> Read(NetworkStream stream, int length, CancellationToken token)
    { var data = new byte[length]; await stream.ReadExactlyAsync(data, token); return data; }
}
