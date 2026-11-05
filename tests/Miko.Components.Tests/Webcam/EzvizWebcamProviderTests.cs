using System.Net;
using System.Text;
using Miko.Components.Webcam;
using Miko.Components.Webcam.Ezviz;
using Shouldly;

namespace Miko.Components.Tests.Webcam;

/// <summary>
/// The Ezviz provider against a scripted Open Platform. Response bodies are the ones the live API
/// returned for the issue's two test devices (one online in the cloud, one only on the LAN).
/// </summary>
public class EzvizWebcamProviderTests
{
    private const string CloudInfo = """{"msg":"Operation succeeded","code":"200","data":{"deviceSerial":"CD7654321","deviceName":"C6C(CD7654321)","model":"CS-C6C-TEST","status":1,"isEncrypt":0,"netAddress":"203.0.113.7","localAddress":"192.168.2.44"}}""";
    private const string LanInfo = """{"msg":"Operation succeeded","code":"200","data":{"deviceSerial":"AB1234567","deviceName":"C6c(AB1234567)","model":"CS-C6c-TEST","status":0,"isEncrypt":1,"localAddress":"192.168.1.64"}}""";
    private const string Capacity = """{"msg":"操作成功!","code":"200","data":{"support_ptz":"1","ptz_left_right":"1","ptz_top_bottom":"1","support_privacy":"1","support_capture":"1","ptz_center_mirror":"1","support_talk":"1"}}""";
    private const string PrivacyOff = """{"msg":"操作成功!","code":"200","data":{"deviceSerial":"CD7654321","channelNo":1,"enable":0}}""";
    private const string Ok = """{"msg":"操作成功!","code":"200"}""";

    private static (EzvizWebcamProvider Provider, FakeApi Api, List<(string Host, int Port)> Probes) Create(
        EzvizOptions? options = null, Func<string, int, bool>? reachable = null)
    {
        var api = new FakeApi();
        var probes = new List<(string, int)>();
        options ??= new EzvizOptions { AppKey = "key", AppSecret = "secret" };
        var provider = new EzvizWebcamProvider(options, new HttpClient(api), null, (host, port, _, _) =>
        {
            lock (probes) probes.Add((host, port));
            return Task.FromResult(reachable?.Invoke(host, port) ?? false);
        });
        api.Reply("/api/lapp/token/get", """{"code":"200","msg":"ok","data":{"accessToken":"at.issued","expireTime":4102444800000}}""");
        return (provider, api, probes);
    }

    [Fact]
    public async Task Should_Stream_From_The_Lan_With_The_Verification_Code_As_Password()
    {
        var (provider, api, probes) = Create(reachable: (host, _) => host == "192.168.1.64");
        api.Reply("/api/lapp/device/info", LanInfo);

        var stream = await provider.GetLiveStreamAsync(new WebcamDevice("AB1234567") { VerifyCode = "QWERTY" }, WebcamQuality.High);

        stream.Transport.ShouldBe(WebcamTransport.LocalNetwork);
        stream.Source.ShouldBe("rtsp://admin:QWERTY@192.168.1.64:554/h264/ch1/main/av_stream");
        probes.ShouldBe([("192.168.1.64", 554)]);
        // The token was issued from the key and secret and sent with the call.
        api.Calls.First(c => c.Path == "/api/lapp/device/info").Form["accessToken"].ShouldBe("at.issued");
    }

    [Fact]
    public async Task Should_Use_The_Given_Local_Address_Without_Asking_The_Cloud()
    {
        var (provider, api, probes) = Create(new EzvizOptions(), reachable: (_, _) => true);

        var stream = await provider.GetLiveStreamAsync(
            new WebcamDevice("AB1234567", 2) { VerifyCode = "AB/CD+", LocalAddress = "10.0.0.7:8554" }, WebcamQuality.Smooth);

        // The code is escaped into the user info; the sub stream of channel 2.
        stream.Source.ShouldBe("rtsp://admin:AB%2FCD%2B@10.0.0.7:8554/h264/ch2/sub/av_stream");
        api.Calls.ShouldBeEmpty();
        probes.ShouldBe([("10.0.0.7", 8554)]);
    }

    [Fact]
    public async Task Should_Ask_For_The_Verification_Code_Of_A_Lan_Camera()
    {
        var (provider, api, _) = Create(reachable: (_, _) => true);
        api.Reply("/api/lapp/device/info", LanInfo);

        var error = await Should.ThrowAsync<WebcamException>(() => provider.GetLiveStreamAsync(new WebcamDevice("AB1234567"), WebcamQuality.Smooth));

        error.Code.ShouldBe("verifycode");
    }

    [Fact]
    public async Task Should_Not_Use_The_Paid_Relay_Unless_Enabled()
    {
        var (provider, api, _) = Create();
        api.Reply("/api/lapp/device/info", CloudInfo);

        var error = await Should.ThrowAsync<WebcamException>(() => provider.GetLiveStreamAsync(new WebcamDevice("CD7654321"), WebcamQuality.Smooth));

        error.Code.ShouldBe("unreachable");
        api.Calls.ShouldNotContain(c => c.Path == "/api/lapp/v2/live/address/get");
    }

    [Fact]
    public async Task Should_Return_The_Relay_Hls_Address_When_Enabled()
    {
        var (provider, api, _) = Create(new EzvizOptions { AppKey = "key", AppSecret = "secret", EnableCloudRelay = true });
        api.Reply("/api/lapp/device/info", CloudInfo);
        api.Reply("/api/lapp/v2/live/address/get", """{"msg":"Operation succeeded","code":"200","data":{"id":"1","url":"https://open.ys7.com/v3/openlive/CD7654321_1_2.m3u8?expire=1","expireTime":"2026-11-05 14:43:54"}}""");
        api.ReplyRaw("/v3/openlive/CD7654321_1_2.m3u8", "#EXTM3U\n#EXT-X-TARGETDURATION:2\n#EXTINF:2.0,\nseg0.ts\n");

        var stream = await provider.GetLiveStreamAsync(new WebcamDevice("CD7654321"), WebcamQuality.Smooth);

        stream.Transport.ShouldBe(WebcamTransport.CloudRelay);
        stream.MimeType.ShouldBe("application/vnd.apple.mpegurl");
        var call = api.Calls.Single(c => c.Path == "/api/lapp/v2/live/address/get");
        call.Form["protocol"].ShouldBe("2");
        call.Form["quality"].ShouldBe("2");
    }

    [Fact]
    public async Task Should_Report_A_Relay_That_Answers_With_An_Error_Clip()
    {
        // What the live account answered: a playlist of one clip named after the error (9048 = viewer limit).
        var (provider, api, _) = Create(new EzvizOptions { AppKey = "key", AppSecret = "secret", EnableCloudRelay = true });
        api.Reply("/api/lapp/device/info", CloudInfo);
        api.Reply("/api/lapp/v2/live/address/get", """{"code":"200","msg":"ok","data":{"url":"https://open.ys7.com/v3/openlive/x.m3u8"}}""");
        api.ReplyRaw("/v3/openlive/x.m3u8", "#EXTM3U\n#EXT-X-VERSION:3\n#EXTINF:4.000,\nhttps://wxv9.ys7.com:8101/ErrCode/9048_0.ts\n#EXT-X-ENDLIST\n");

        var error = await Should.ThrowAsync<WebcamException>(() => provider.GetLiveStreamAsync(new WebcamDevice("CD7654321"), WebcamQuality.Smooth));

        error.Code.ShouldBe("9048");
        WebcamTexts.Chinese.Describe(error).ShouldBe("同时观看人数已达账号上限");
    }

    [Fact]
    public async Task Should_Read_Features_And_Privacy_From_The_Capabilities()
    {
        var (provider, api, _) = Create();
        api.Reply("/api/lapp/device/info", CloudInfo);
        api.Reply("/api/lapp/device/capacity", Capacity);
        api.Reply("/api/lapp/device/scene/switch/status", PrivacyOff);

        var info = await provider.GetInfoAsync(new WebcamDevice("CD7654321"));

        info.Name.ShouldBe("C6C(CD7654321)");
        info.IsOnline.ShouldBeTrue();
        info.Features.ShouldBe(WebcamFeatures.Pan | WebcamFeatures.Tilt | WebcamFeatures.Privacy | WebcamFeatures.Capture | WebcamFeatures.Mirror);
        info.PrivacyEnabled.ShouldBe(false);
        info.IsOnLocalNetwork.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Offer_Cloud_Controls_For_A_Device_The_Cloud_Reports_Offline()
    {
        var (provider, api, _) = Create(reachable: (_, _) => true);
        api.Reply("/api/lapp/device/info", LanInfo);

        var info = await provider.GetInfoAsync(new WebcamDevice("AB1234567"));

        info.IsOnline.ShouldBeTrue();
        info.IsOnLocalNetwork.ShouldBeTrue();
        info.Features.ShouldBe(WebcamFeatures.None);
        api.Calls.ShouldNotContain(c => c.Path == "/api/lapp/device/capacity");
    }

    [Theory]
    [InlineData(PtzDirection.Up, "0")]
    [InlineData(PtzDirection.Down, "1")]
    [InlineData(PtzDirection.Left, "2")]
    [InlineData(PtzDirection.Right, "3")]
    [InlineData(PtzDirection.ZoomIn, "8")]
    public async Task Should_Send_Ptz_Commands_With_The_Api_Direction_Codes(PtzDirection direction, string code)
    {
        var (provider, api, _) = Create();
        api.Reply("/api/lapp/device/ptz/start", Ok);
        api.Reply("/api/lapp/device/ptz/stop", Ok);
        var device = new WebcamDevice("CD7654321");

        await provider.StartPtzAsync(device, direction, PtzSpeed.Fast);
        await provider.StopPtzAsync(device, direction);

        var start = api.Calls.Single(c => c.Path == "/api/lapp/device/ptz/start").Form;
        start["deviceSerial"].ShouldBe("CD7654321");
        start["channelNo"].ShouldBe("1");
        start["direction"].ShouldBe(code);
        start["speed"].ShouldBe("2");
        api.Calls.Single(c => c.Path == "/api/lapp/device/ptz/stop").Form["direction"].ShouldBe(code);
    }

    [Fact]
    public async Task Should_Report_A_Ptz_Limit_With_Its_Code()
    {
        var (provider, api, _) = Create();
        api.Reply("/api/lapp/device/ptz/start", """{"code":"60004","msg":"设备左方向云台达到限位"}""");

        var error = await Should.ThrowAsync<WebcamException>(() => provider.StartPtzAsync(new WebcamDevice("CD7654321"), PtzDirection.Left, PtzSpeed.Normal));

        error.Code.ShouldBe("60004");
        WebcamTexts.Chinese.Describe(error).ShouldBe("云台已转到尽头");
    }

    [Fact]
    public async Task Should_Renew_An_Expired_Token_And_Retry_Once()
    {
        var (provider, api, _) = Create();
        api.Reply("/api/lapp/device/ptz/stop", """{"code":"10002","msg":"accessToken过期或异常"}""", Ok);

        await provider.StopPtzAsync(new WebcamDevice("CD7654321"), PtzDirection.Up);

        api.Calls.Count(c => c.Path == "/api/lapp/token/get").ShouldBe(2);
        api.Calls.Count(c => c.Path == "/api/lapp/device/ptz/stop").ShouldBe(2);
    }

    [Fact]
    public async Task Should_Use_A_Fixed_Token_Without_Issuing_One()
    {
        var (provider, api, _) = Create(new EzvizOptions { AccessToken = "at.fixed" });
        api.Reply("/api/lapp/device/scene/switch/set", Ok);

        await provider.SetPrivacyAsync(new WebcamDevice("CD7654321"), true);

        api.Calls.ShouldNotContain(c => c.Path == "/api/lapp/token/get");
        var call = api.Calls.Single();
        call.Form["accessToken"].ShouldBe("at.fixed");
        call.Form["enable"].ShouldBe("1");
    }

    [Fact]
    public async Task Should_Download_The_Device_Capture()
    {
        var (provider, api, _) = Create();
        api.Reply("/api/lapp/device/capture", """{"code":"200","msg":"ok","data":{"picUrl":"https://opencapture.ys7.com/pic/1.jpg?Expires=1"}}""");
        api.ReplyRaw("/pic/1.jpg", "ÿØjpeg");

        var snapshot = await provider.CaptureAsync(new WebcamDevice("CD7654321"));

        snapshot.Url.ShouldStartWith("https://opencapture.ys7.com/pic/1.jpg");
        snapshot.Data.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Should_Parse_Ezopen_Addresses()
    {
        var device = WebcamDevice.ParseEzopen("ezopen://QWERTY@open.ys7.com/ab1234567/2.hd.live", out var quality);

        device.Serial.ShouldBe("AB1234567");
        device.Channel.ShouldBe(2);
        device.VerifyCode.ShouldBe("QWERTY");
        quality.ShouldBe(WebcamQuality.High);
        device.ToEzopen(WebcamQuality.High).ShouldBe("ezopen://open.ys7.com/AB1234567/2.hd.live");
        // The verification code is a password: not in ToString.
        device.ToString().ShouldNotContain("QWERTY");
        Should.Throw<FormatException>(() => WebcamDevice.ParseEzopen("ezopen://open.ys7.com/", out _));
        Should.Throw<FormatException>(() => WebcamDevice.ParseEzopen("rtsp://host/x", out _));
    }

    [Theory]
    [InlineData("192.168.1.5", "192.168.1.5", 554)]
    [InlineData("192.168.1.5:8554", "192.168.1.5", 8554)]
    [InlineData("[fe80::1]:10554", "fe80::1", 10554)]
    [InlineData("fe80::1", "fe80::1", 554)]
    public void Should_Split_Local_Addresses(string address, string host, int port) =>
        EzvizWebcamProvider.SplitHostPort(address, 554).ShouldBe((host, port));

    private sealed class FakeApi : HttpMessageHandler
    {
        private readonly Dictionary<string, Queue<string>> _replies = new();
        private readonly Dictionary<string, string> _raw = new();

        public List<(string Path, Dictionary<string, string> Form)> Calls { get; } = new();

        /// <summary>Answers <paramref name="path"/> with <paramref name="bodies"/> in turn (the last one repeats).</summary>
        public void Reply(string path, params string[] bodies) => _replies[path] = new Queue<string>(bodies);

        public void ReplyRaw(string path, string body) => _raw[path] = body;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri!.AbsolutePath;
            if (_raw.TryGetValue(path, out var raw))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.Latin1.GetBytes(raw)) };

            var form = new Dictionary<string, string>();
            if (request.Content != null)
            {
                foreach (var pair in (await request.Content.ReadAsStringAsync(cancellationToken)).Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split('=', 2);
                    form[Uri.UnescapeDataString(kv[0])] = Uri.UnescapeDataString(kv[1].Replace('+', ' '));
                }
            }
            lock (Calls) Calls.Add((path, form));
            if (!_replies.TryGetValue(path, out var bodies)) return new HttpResponseMessage(HttpStatusCode.NotFound);
            var body = bodies.Count > 1 ? bodies.Dequeue() : bodies.Peek();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
