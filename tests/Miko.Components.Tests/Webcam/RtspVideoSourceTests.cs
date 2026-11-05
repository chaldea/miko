using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Miko.Components.Webcam.Streaming;
using Miko.Platform.Video;
using Shouldly;

namespace Miko.Components.Tests.Webcam;

/// <summary>
/// The RTSP client against a scripted server on loopback that answers like an Ezviz / Hikvision
/// camera: Digest authentication, the SDP of a C6c, RTP interleaved on the TCP connection.
/// </summary>
public class RtspVideoSourceTests
{
    // DESCRIBE answer of an Ezviz C6c (sub stream), verbatim apart from the addresses: H.265 labelled
    // with an H.264 profile-level-id and sprop-parameter-sets that are not valid parameter sets.
    private const string CameraSdp =
        "v=0\r\n" +
        "o=- 1793886167635980 1793886167635980 IN IP4 127.0.0.1\r\n" +
        "s=Media Presentation\r\n" +
        "e=NONE\r\n" +
        "b=AS:5100\r\n" +
        "t=0 0\r\n" +
        "a=control:rtsp://127.0.0.1/h264/ch1/sub/av_stream/\r\n" +
        "m=video 0 RTP/AVP 96\r\n" +
        "b=AS:5000\r\n" +
        "a=control:rtsp://127.0.0.1/h264/ch1/sub/av_stream/trackID=1\r\n" +
        "a=rtpmap:96 H265/90000\r\n" +
        "a=fmtp:96 profile-level-id=420029; packetization-mode=1; sprop-parameter-sets=5TNwEBAQQABX5AAAV+QoQAAAAAFEAcDy8CJAAAAAASYAAAAAAAAAAAAAAAA=,\r\n" +
        "m=audio 0 RTP/AVP 104\r\n" +
        "b=AS:50\r\n" +
        "a=recvonly\r\n" +
        "a=control:rtsp://127.0.0.1/h264/ch1/sub/av_stream/trackID=2\r\n" +
        "a=rtpmap:104 mpeg4-generic/16000/1\r\n";

    [Fact]
    public void Sdp_Should_Read_The_Video_Track_And_Skip_Garbage_Parameter_Sets()
    {
        var sdp = SdpSession.Parse(CameraSdp);

        var video = sdp.Media.Single(m => m.Type == "video");
        video.Encoding.ShouldBe("H265");
        video.PayloadType.ShouldBe(96);
        video.ClockRate.ShouldBe(90000);
        video.Control.ShouldBe("rtsp://127.0.0.1/h264/ch1/sub/av_stream/trackID=1");
        // Base64 of something that is no VPS/SPS/PPS: ignored, the camera sends the real ones in band.
        video.GetParameterSets(VideoCodec.H265).ShouldBeEmpty();
        sdp.Media.Single(m => m.Type == "audio").Encoding.ShouldBe("MPEG4-GENERIC");
    }

    [Theory]
    [InlineData("rtsp://cam/stream/", "trackID=1", "rtsp://cam/stream/trackID=1")]
    [InlineData("rtsp://cam/stream", "trackID=1", "rtsp://cam/stream/trackID=1")]
    [InlineData("rtsp://cam/stream/", "rtsp://cam/stream/trackID=1", "rtsp://cam/stream/trackID=1")]
    [InlineData("rtsp://cam/stream/", "*", "rtsp://cam/stream/")]
    public void Should_Resolve_Control_Uris(string baseUri, string control, string expected) =>
        RtspVideoSource.ResolveControl(baseUri, control).ShouldBe(expected);

    [Fact]
    public async Task Should_Authenticate_And_Read_Access_Units_Over_Tcp()
    {
        await using var server = new FakeCamera("admin", "QWERTY");
        await using var source = new RtspVideoSource(new Uri($"rtsp://admin:QWERTY@127.0.0.1:{server.Port}/h264/ch1/sub/av_stream"));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var info = await source.OpenAsync(timeout.Token);
        info.Codec.ShouldBe(VideoCodec.H265);

        var first = (await source.ReadFrameAsync(timeout.Token)).ShouldNotBeNull();
        first.IsKeyFrame.ShouldBeTrue();
        NalUnits.GetParameterSets(VideoCodec.H265, first.Data.Span).Count.ShouldBe(3);
        var second = (await source.ReadFrameAsync(timeout.Token)).ShouldNotBeNull();
        second.IsKeyFrame.ShouldBeFalse();
        second.Timestamp.ShouldBe(TimeSpan.FromSeconds(.1));
        source.BytesReceived.ShouldBeGreaterThan(0);

        server.Requests.Select(r => r.Method).ShouldBe(["DESCRIBE", "DESCRIBE", "SETUP", "PLAY"]);
        // The credentials go into the Digest response only, never into the request line.
        server.Requests.ShouldAllBe(r => !r.Uri.Contains("QWERTY"));
        server.Requests[1].Authorization.ShouldStartWith("Digest username=\"admin\"");
        server.Requests[2].Uri.ShouldBe($"rtsp://127.0.0.1:{server.Port}/h264/ch1/sub/av_stream/trackID=1");
        server.Requests[3].Session.ShouldBe("635537807");
    }

    [Fact]
    public async Task Should_Report_A_Wrong_Password_As_A_Permanent_Failure()
    {
        await using var server = new FakeCamera("admin", "QWERTY");
        await using var source = new RtspVideoSource(new Uri($"rtsp://admin:WRONG1@127.0.0.1:{server.Port}/h264/ch1/sub/av_stream"));

        var error = await Should.ThrowAsync<RtspException>(() => source.OpenAsync(CancellationToken.None));

        error.StatusCode.ShouldBe(401);
        error.IsPermanent.ShouldBeTrue();
        // One challenge answered, not a retry loop.
        server.Requests.Count(r => r.Method == "DESCRIBE").ShouldBe(2);
    }

    [Fact]
    public async Task Should_Time_Out_When_The_Camera_Stops_Sending()
    {
        await using var server = new FakeCamera("admin", "QWERTY") { FramesToSend = 1 };
        await using var source = new RtspVideoSource(new Uri($"rtsp://admin:QWERTY@127.0.0.1:{server.Port}/live"), TimeSpan.FromMilliseconds(1500));
        await source.OpenAsync(CancellationToken.None);
        // The first access unit completes when the second one starts, which never comes; the
        // connection stays open but silent.
        await Should.ThrowAsync<TimeoutException>(async () =>
        {
            while (true) await source.ReadFrameAsync(CancellationToken.None);
        });
    }

    [Fact]
    public async Task Should_End_A_Pending_Read_When_Cancelled()
    {
        await using var server = new FakeCamera("admin", "QWERTY") { FramesToSend = 0 };
        await using var source = new RtspVideoSource(new Uri($"rtsp://admin:QWERTY@127.0.0.1:{server.Port}/live"));
        await source.OpenAsync(CancellationToken.None);
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await Should.ThrowAsync<OperationCanceledException>(async () => await source.ReadFrameAsync(cancel.Token));
    }

    /// <summary>A one-connection RTSP server that plays back a key frame and delta frames.</summary>
    private sealed class FakeCamera : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly string _user, _password;
        private readonly Task _serve;
        private readonly CancellationTokenSource _stop = new();
        private const string Realm = "a1b2c3d4e5f6";
        private const string Nonce = "0123456789abcdef0123456789abcdef";

        public FakeCamera(string user, string password)
        {
            _user = user;
            _password = password;
            _listener.Start();
            _serve = Task.Run(ServeAsync);
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public List<(string Method, string Uri, string Authorization, string Session)> Requests { get; } = new();
        public int FramesToSend { get; init; } = 3;

        private async Task ServeAsync()
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                var stream = client.GetStream();
                var reader = new StreamReader(stream, Encoding.ASCII);
                while (!_stop.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(_stop.Token);
                    if (line == null) return;
                    if (line.Length == 0) continue;
                    var parts = line.Split(' ');
                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string? h;
                    while (!string.IsNullOrEmpty(h = await reader.ReadLineAsync(_stop.Token)))
                    {
                        int colon = h.IndexOf(':');
                        headers[h[..colon].Trim()] = h[(colon + 1)..].Trim();
                    }
                    string method = parts[0], uri = parts[1], cseq = headers["CSeq"];
                    lock (Requests) Requests.Add((method, uri, headers.GetValueOrDefault("Authorization") ?? "", headers.GetValueOrDefault("Session") ?? ""));

                    if (!Authorized(method, uri, headers.GetValueOrDefault("Authorization")))
                    {
                        await Write(stream, $"RTSP/1.0 401 Unauthorized\r\nCSeq: {cseq}\r\nWWW-Authenticate: Digest realm=\"{Realm}\", nonce=\"{Nonce}\", stale=\"FALSE\"\r\nWWW-Authenticate: Basic realm=\"{Realm}\"\r\n\r\n");
                        continue;
                    }
                    switch (method)
                    {
                        case "DESCRIBE":
                            var sdp = CameraSdp.Replace("127.0.0.1", $"127.0.0.1:{Port}");
                            await Write(stream, $"RTSP/1.0 200 OK\r\nCSeq: {cseq}\r\nContent-Type: application/sdp\r\nContent-Base: rtsp://127.0.0.1:{Port}/h264/ch1/sub/av_stream/\r\nContent-Length: {Encoding.ASCII.GetByteCount(sdp)}\r\n\r\n{sdp}");
                            break;
                        case "SETUP":
                            await Write(stream, $"RTSP/1.0 200 OK\r\nCSeq: {cseq}\r\nSession:        635537807;timeout=20\r\nTransport: RTP/AVP/TCP;unicast;interleaved=0-1;ssrc= 9e186ab;mode=\"play\"\r\n\r\n");
                            break;
                        case "PLAY":
                            await Write(stream, $"RTSP/1.0 200 OK\r\nCSeq: {cseq}\r\nSession:        635537807\r\n\r\n");
                            await SendFramesAsync(stream);
                            break;
                        default:
                            await Write(stream, $"RTSP/1.0 200 OK\r\nCSeq: {cseq}\r\n\r\n");
                            break;
                    }
                }
            }
            catch (Exception) when (_stop.IsCancellationRequested)
            {
            }
            catch (IOException)
            {
            }
        }

        private bool Authorized(string method, string uri, string? authorization)
        {
            if (authorization == null || !authorization.StartsWith("Digest ", StringComparison.Ordinal)) return false;
            var response = authorization.Split("response=\"")[1].Split('"')[0];
            string Md5(string s) => Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(s)));
            return response == Md5($"{Md5($"{_user}:{Realm}:{_password}")}:{Nonce}:{Md5($"{method}:{uri}")}");
        }

        private async Task SendFramesAsync(NetworkStream stream)
        {
            if (FramesToSend == 0) return;
            uint ts = 1323465580;
            ushort seq = 34081;
            async Task Rtp(bool marker, params byte[] payload)
            {
                var packet = new byte[4 + 12 + payload.Length];
                packet[0] = (byte)'$';
                packet[1] = 0;
                packet[2] = (byte)((12 + payload.Length) >> 8);
                packet[3] = (byte)(12 + payload.Length);
                packet[4] = 0x80;
                packet[5] = (byte)((marker ? 0x80 : 0) | 96);
                packet[6] = (byte)(seq >> 8); packet[7] = (byte)seq; seq++;
                packet[8] = (byte)(ts >> 24); packet[9] = (byte)(ts >> 16); packet[10] = (byte)(ts >> 8); packet[11] = (byte)ts;
                payload.CopyTo(packet, 16);
                await stream.WriteAsync(packet);
            }
            // Key frame: VPS, SPS, PPS (marker), IDR in two fragments (continuation typed 38).
            await Rtp(false, 0x40, 0x01, 0x0c, 0x01);
            await Rtp(false, 0x42, 0x01, 0x01, 0x01, 0x60);
            await Rtp(true, 0x44, 0x01, 0xc0, 0xf2);
            await Rtp(false, 0x62, 0x01, 0x93, 0xaf, 0x01);
            await Rtp(true, 0x62, 0x01, 0x66, 0x2d);
            for (int i = 1; i < FramesToSend; i++)
            {
                ts += 9000;
                await Rtp(true, 0x02, 0x01, (byte)i);
            }
            // An audio packet on channel 2 in between: skipped by the client.
            await stream.WriteAsync(new byte[] { (byte)'$', 2, 0, 1, 0xff });
        }

        private static Task Write(NetworkStream stream, string text) => stream.WriteAsync(Encoding.ASCII.GetBytes(text)).AsTask();

        public async ValueTask DisposeAsync()
        {
            _stop.Cancel();
            _listener.Stop();
            try { await _serve; } catch { }
            _stop.Dispose();
        }
    }
}
