using System.Net;
using System.Text;
using Miko.Components.Webcam;
using Miko.Components.Webcam.Ezviz;
using Miko.Platform.Video;
using MikoApp.Webcam;
using Shouldly;

namespace Miko.Webcam.Demo.Tests;

public sealed class ConnectionSettingsTests
{
    [Fact]
    public async Task Should_default_to_mock_and_choose_the_backend_transport()
    {
        using var http = new HttpClient();
        var settings = new WebcamConnectionSettings();
        settings.Provider.ShouldBe("miko");
        var connection = settings.CreateConnection(new NullVideoBackend(), http);
        var stream = await connection.Provider.GetLiveStreamAsync(connection.Device, connection.Quality);
        stream.Source.ShouldBe("http://localhost:8080/media/demo.mp4");
        stream.Loop.ShouldBeTrue();
        settings.MockHttp = "";
        settings.MockRtsp = "rtsp://camera.test:8555/webcam";
        settings.MockUser = "user@demo";
        settings.MockPassword = "a:@/%#";
        connection = settings.CreateConnection(new StreamBackend(), http);
        stream = await connection.Provider.GetLiveStreamAsync(connection.Device, connection.Quality);
        var uri = new Uri(stream.Source);
        uri.Host.ShouldBe("camera.test"); uri.Port.ShouldBe(8555);
        var credentials = uri.UserInfo.Split(':', 2);
        Uri.UnescapeDataString(credentials[0]).ShouldBe("user@demo");
        Uri.UnescapeDataString(credentials[1]).ShouldBe("a:@/%#");
        stream.Loop.ShouldBeFalse();
    }

    [Theory]
    [InlineData("https://camera.test/webcam")]
    [InlineData("rtsp://camera.test:99999/webcam")]
    [InlineData("rtsp://user:supersecret@camera.test/webcam")]
    [InlineData("")]
    public void Should_reject_invalid_mock_parameters_without_echoing_credentials(string address)
    {
        using var http = new HttpClient();
        var settings = new WebcamConnectionSettings { MockRtsp = address };
        var error = Should.Throw<ArgumentException>(() => settings.CreateConnection(new NullVideoBackend(), http));
        error.Message.ShouldNotContain("supersecret");
    }

    [Theory]
    [InlineData("token")]
    [InlineData("app")]
    public async Task Should_snapshot_ezviz_credentials_and_request_cloud_playback(string authentication)
    {
        var handler = new EzvizHandler();
        using var http = new HttpClient(handler);
        var settings = new WebcamConnectionSettings
        {
            Provider = "ezviz", Serial = "ab1234", Channel = "2", Authentication = authentication,
            AccessToken = "token-original", AppKey = "key-original", AppSecret = "secret-original",
            EnableCloudRelay = true, ApiBaseUrl = "https://ezviz.test",
        };
        var connection = settings.CreateConnection(new NullVideoBackend(), http);
        connection.Provider.ShouldBeOfType<EzvizWebcamProvider>();
        connection.Device.Serial.ShouldBe("AB1234"); connection.Device.Channel.ShouldBe(2);
        settings.AccessToken = "changed"; settings.AppKey = "changed"; settings.AppSecret = "changed";
        var stream = await connection.Provider.GetLiveStreamAsync(connection.Device, connection.Quality);
        stream.Transport.ShouldBe(WebcamTransport.CloudRelay);
        handler.Bodies.ShouldNotContain(x => x.Contains("changed"));
        handler.Bodies.ShouldContain(x => x.Contains("channelNo=2") && x.Contains("deviceSerial=AB1234"));
        handler.Bodies.ShouldContain(x => x.Contains(authentication == "token" ? "accessToken=token-original" : "appKey=key-original"));
    }

    [Fact]
    public void Should_require_valid_ezviz_channel_and_complete_credentials()
    {
        using var http = new HttpClient();
        var settings = new WebcamConnectionSettings { Provider = "ezviz", Serial = "AB1234", Channel = "0", EnableCloudRelay = true };
        Should.Throw<ArgumentException>(() => settings.CreateConnection(new NullVideoBackend(), http)).Message.ShouldContain("通道号");
        settings.Channel = "1";
        Should.Throw<ArgumentException>(() => settings.CreateConnection(new NullVideoBackend(), http)).Message.ShouldContain("Token");
        settings.Authentication = "app"; settings.AppKey = "key";
        Should.Throw<ArgumentException>(() => settings.CreateConnection(new NullVideoBackend(), http)).Message.ShouldContain("App Secret");
        settings.Authentication = "local"; settings.EnableCloudRelay = false;
        Should.Throw<ArgumentException>(() => settings.CreateConnection(new StreamBackend(), http)).Message.ShouldContain("局域网");
        settings.LocalAddress = "192.168.1.20:554"; settings.VerifyCode = "CODE";
        settings.CreateConnection(new StreamBackend(), http).Provider.ShouldBeOfType<EzvizWebcamProvider>();
        settings.LocalAddress = "camera.test";
        settings.CreateConnection(new StreamBackend(), http).Device.LocalAddress.ShouldBe("camera.test");
        Should.Throw<ArgumentException>(() => settings.CreateConnection(new NullVideoBackend(), http)).Message.ShouldContain("不支持");
    }

    private sealed class StreamBackend : IVideoBackend, IVideoStreamDecoderFactory
    {
        public VideoBackendCapabilities Capabilities => new(false, false, []);
        public bool CanDecode(VideoCodec codec) => codec == VideoCodec.H264;
        public IVideoStreamDecoder CreateDecoder(VideoCodec codec) => throw new NotSupportedException();
        public IVideoSession CreateSession(VideoSourceDescriptor source, VideoSessionOptions options) => throw new NotSupportedException();
    }

    private sealed class EzvizHandler : HttpMessageHandler
    {
        public List<string> Bodies { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.Method == HttpMethod.Get)
                return new(HttpStatusCode.OK) { Content = new StringContent("#EXTM3U\n#EXTINF:2,\nsegment.ts\n") };
            Bodies.Add(await request.Content!.ReadAsStringAsync(token));
            var body = request.RequestUri!.AbsolutePath.EndsWith("token/get")
                ? """{"code":"200","data":{"accessToken":"issued","expireTime":4102444800000}}"""
                : """{"code":"200","data":{"url":"https://video.test/live.m3u8"}}""";
            return new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
