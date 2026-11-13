using System.Globalization;
using Miko.Components.Webcam;
using Miko.Components.Webcam.Ezviz;
using Miko.Components.Webcam.Mock;
using Miko.Platform.Video;

namespace MikoApp.Webcam;

/// <summary>Editable, in-memory form values. Credentials are never persisted.</summary>
public sealed class WebcamConnectionSettings
{
    public string Provider { get; set; } = "miko";
    public string? MockRtsp { get; set; } = "rtsp://localhost:8554/webcam";
    public string? MockHttp { get; set; } = "http://localhost:8080";
    public string? MockUser { get; set; }
    public string? MockPassword { get; set; }
    public string? ApiBaseUrl { get; set; } = "https://open.ys7.com";
    public string Authentication { get; set; } = "token";
    public string? AccessToken { get; set; }
    public string? AppKey { get; set; }
    public string? AppSecret { get; set; }
    public string? Serial { get; set; }
    public string? Channel { get; set; } = "1";
    public string? VerifyCode { get; set; }
    public string? LocalAddress { get; set; }
    public string? LocalUser { get; set; } = "admin";
    public bool EnableCloudRelay { get; set; }

    public static WebcamConnectionSettings FromEnvironment(WebcamDevice? device = null)
    {
        static string? Get(string key) => Environment.GetEnvironmentVariable(key);
        var settings = new WebcamConnectionSettings
        {
            MockRtsp = Get("MIKO_MOCK_RTSP") ?? "rtsp://localhost:8554/webcam",
            AccessToken = Get("EZVIZ_ACCESS_TOKEN"), AppKey = Get("EZVIZ_APP_KEY"), AppSecret = Get("EZVIZ_APP_SECRET"),
            LocalAddress = Get("EZVIZ_DEVICE_IP"), EnableCloudRelay = Get("EZVIZ_CLOUD_RELAY") == "1",
        };
        // Prefill credentials separately so the URL field never displays a password.
        if (Uri.TryCreate(settings.MockRtsp, UriKind.Absolute, out var rtsp) && rtsp.Scheme == "rtsp")
        {
            var credentials = rtsp.UserInfo.Split(':', 2);
            settings.MockUser = Uri.UnescapeDataString(credentials[0]);
            settings.MockPassword = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null;
            settings.MockRtsp = new UriBuilder(rtsp) { UserName = "", Password = "" }.Uri.AbsoluteUri;
            settings.MockHttp = new UriBuilder("http", rtsp.Host, 8080).Uri.AbsoluteUri.TrimEnd('/');
        }
        settings.MockHttp = Get("MIKO_MOCK_HTTP") ?? settings.MockHttp;
        if (string.IsNullOrWhiteSpace(settings.AccessToken) && !string.IsNullOrWhiteSpace(settings.AppKey)) settings.Authentication = "app";
        bool explicitDevice = device != null;
        if (device == null && Get("EZVIZ_DEVICE") is { Length: > 0 } address)
        {
            try { device = WebcamDevice.ParseEzopen(address, out _); }
            catch (FormatException) { /* An invalid preset must not prevent the form from opening. */ }
        }
        if (device != null)
        {
            settings.Serial = device.Serial;
            settings.Channel = device.Channel.ToString(CultureInfo.InvariantCulture);
            settings.VerifyCode = device.VerifyCode;
            settings.LocalAddress = device.LocalAddress ?? settings.LocalAddress;
        }
        if (explicitDevice) settings.Provider = "ezviz";
        return settings;
    }

    /// <summary>Validate and snapshot the current form. This never contacts a server.</summary>
    public WebcamConnection CreateConnection(IVideoBackend backend, HttpClient http)
    {
        bool canStream = backend is IVideoStreamDecoderFactory decoder && decoder.CanDecode(VideoCodec.H264);
        if (Provider == "miko")
        {
            var rtsp = ParseAddress(MockRtsp, "RTSP 地址", "rtsp");
            if (!string.IsNullOrEmpty(MockPassword) && string.IsNullOrWhiteSpace(MockUser))
                throw new ArgumentException("填写 RTSP 密码时，请同时填写用户名。");
            var source = new UriBuilder(rtsp)
            {
                UserName = Uri.EscapeDataString(MockUser?.Trim() ?? ""),
                Password = Uri.EscapeDataString(MockPassword ?? ""),
            }.Uri.AbsoluteUri;
            var httpBase = string.IsNullOrWhiteSpace(MockHttp)
                ? new UriBuilder("http", rtsp.Host, 8080).Uri
                : ParseAddress(MockHttp, "HTTP 服务地址", "http", "https");
            var video = new Uri(httpBase.AbsoluteUri.TrimEnd('/') + "/media/demo.mp4").AbsoluteUri;
            return new(new WebcamDevice("mock") { Name = "Miko 全景摄像头" },
                new MockWebcamProvider(source, fallbackSource: canStream ? null : video), WebcamQuality.Smooth);
        }
        if (Provider != "ezviz") throw new ArgumentException("请选择 Miko Mock 或萤石云。");
        if (string.IsNullOrWhiteSpace(Serial) || Serial.Trim().Any(c => !char.IsAsciiLetterOrDigit(c)))
            throw new ArgumentException("请填写有效的设备序列号（英文字母和数字）。");
        if (!int.TryParse(Channel, NumberStyles.None, CultureInfo.InvariantCulture, out int channel) || channel is < 1 or > 256)
            throw new ArgumentException("通道号必须是 1–256 的整数。");
        var api = ParseAddress(ApiBaseUrl, "萤石云 API 地址", "http", "https");
        string? local = string.IsNullOrWhiteSpace(LocalAddress) ? null : LocalAddress.Trim();
        if (local != null && (!Uri.TryCreate("rtsp://" + local, UriKind.Absolute, out var endpoint) ||
            endpoint.Host.Length == 0 || endpoint.Port is 0 or > 65535 || endpoint.UserInfo.Length > 0 ||
            endpoint.AbsolutePath != "/" || endpoint.Query.Length > 0 || endpoint.Fragment.Length > 0))
            throw new ArgumentException("局域网地址应为主机名或 IP，可包含端口，例如 192.168.1.20:554。");
        if (local != null && (string.IsNullOrWhiteSpace(VerifyCode) || string.IsNullOrWhiteSpace(LocalUser)))
            throw new ArgumentException("局域网连接需要用户名和设备验证码 / 密码。");
        if (Authentication == "token" && string.IsNullOrWhiteSpace(AccessToken))
            throw new ArgumentException("请填写 Access Token，或切换其他认证方式。");
        if (Authentication == "app" && (string.IsNullOrWhiteSpace(AppKey) || string.IsNullOrWhiteSpace(AppSecret)))
            throw new ArgumentException("请同时填写 App Key 和 App Secret。");
        if (Authentication == "local" && (local == null || EnableCloudRelay))
            throw new ArgumentException("仅局域网模式需要设备地址和验证码，且不能启用云端中转。");
        if (Authentication is not ("token" or "app" or "local")) throw new ArgumentException("请选择认证方式。");
        if (!canStream && !EnableCloudRelay)
            throw new ArgumentException("当前视频后端不支持摄像头 RTSP，请启用萤石云中转，或使用支持 RTSP 的平台。");

        var options = new EzvizOptions
        {
            ApiBaseUrl = api.AbsoluteUri, AccessToken = Authentication == "token" ? AccessToken!.Trim() : null,
            AppKey = Authentication == "app" ? AppKey!.Trim() : null, AppSecret = Authentication == "app" ? AppSecret!.Trim() : null,
            LocalUser = string.IsNullOrWhiteSpace(LocalUser) ? "admin" : LocalUser.Trim(),
            EnableCloudRelay = EnableCloudRelay, PreferLocalNetwork = canStream,
        };
        return new(new WebcamDevice(Serial.Trim().ToUpperInvariant(), channel)
        { VerifyCode = VerifyCode, LocalAddress = local }, new EzvizWebcamProvider(options, http), WebcamQuality.Smooth);
    }

    private static Uri ParseAddress(string? value, string label, params string[] schemes)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri) || !schemes.Contains(uri.Scheme) ||
            uri.Host.Length == 0 || uri.Port is 0 or > 65535 || uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException($"{label}格式不正确，请填写 {string.Join(" / ", schemes.Select(s => s + "://"))} 地址，用户名和密码请填入单独的输入框。");
        return uri;
    }
}

/// <summary>A connection keeps its own provider snapshot, including while old async calls finish.</summary>
public sealed record WebcamConnection(WebcamDevice Device, IWebcamProvider Provider, WebcamQuality Quality);

/// <summary>App-local state; a newly mounted MikoWebcam receives the active provider once through DI.</summary>
public sealed class WebcamDemoSession(IVideoBackend backend, HttpClient http)
{
    public WebcamConnection? Connection { get; private set; }
    public void Connect(WebcamConnectionSettings settings) => Connection = settings.CreateConnection(backend, http);
    public void Disconnect() => Connection = null;
    public IWebcamProvider RequireProvider() => Connection?.Provider ?? throw new InvalidOperationException("Connect before mounting the webcam.");
}
