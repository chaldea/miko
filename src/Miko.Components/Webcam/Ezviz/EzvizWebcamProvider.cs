using System.Globalization;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Miko.Components.Webcam.Ezviz;

/// <summary>Configuration of the Ezviz provider.</summary>
public sealed class EzvizOptions
{
    /// <summary>Open Platform API host; <c>https://open.ys7.com</c> in China, <c>https://open.ezvizlife.com</c> elsewhere.</summary>
    public string ApiBaseUrl { get; set; } = "https://open.ys7.com";

    /// <summary>The app key of the Open Platform application (with <see cref="AppSecret"/>, tokens are issued and renewed automatically).</summary>
    public string? AppKey { get; set; }

    public string? AppSecret { get; set; }

    /// <summary>A fixed access token, for apps whose server issues tokens. Used until it expires.</summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Stream straight from the camera when it is on the local network (default). The camera serves
    /// RTSP with user <see cref="LocalUser"/> and its verification code as the password.
    /// </summary>
    public bool PreferLocalNetwork { get; set; } = true;

    /// <summary>
    /// Fall back to the cloud's HLS relay when the camera is not on the local network. Off by default:
    /// relayed streams count against the account's paid traffic and simultaneous-viewer limits.
    /// </summary>
    public bool EnableCloudRelay { get; set; }

    /// <summary>The camera's local user name.</summary>
    public string LocalUser { get; set; } = "admin";

    /// <summary>RTSP port used when <see cref="WebcamDevice.LocalAddress"/> does not name one.</summary>
    public int RtspPort { get; set; } = 554;

    /// <summary>How long to try the camera's local address before giving up on the local network.</summary>
    public TimeSpan LocalProbeTimeout { get; set; } = TimeSpan.FromSeconds(1.5);
}

/// <summary>
/// The default <see cref="IWebcamProvider"/>: Ezviz (萤石) cameras through the Open Platform API, with
/// the live picture streamed straight from the camera on the local network.
/// </summary>
/// <remarks>
/// <para>
/// <b>What the EZOpen SDK does natively and this does in C#.</b> The SDK's control calls are Open
/// Platform HTTP APIs — device info, capabilities, pan / tilt / zoom, capture, privacy, mirror — and are
/// called here directly. Its streams (LAN direct → P2P → relay, tried in that order) use Ezviz's
/// private protocol in closed native libraries. The LAN step is replaced by the standard protocol the
/// camera also speaks: RTSP, user <c>admin</c>, the verification code as password, H.264 / H.265
/// decoded by the platform. P2P has no managed equivalent; the cloud relay is reached through the
/// API's HLS address when <see cref="EzvizOptions.EnableCloudRelay"/> allows it.
/// </para>
/// <para>
/// The camera's local address comes from <see cref="WebcamDevice.LocalAddress"/> or from the cloud,
/// which knows it even for a device it reports offline. Without the cloud (no credentials, no
/// internet) a device with a local address and verification code still streams, but cannot be
/// steered: Ezviz cameras take pan / tilt only through the cloud.
/// </para>
/// </remarks>
public sealed class EzvizWebcamProvider : IWebcamProvider
{
    private readonly EzvizOpenApiClient _api;
    private readonly EzvizOptions _options;
    private readonly HttpClient _http;
    private readonly ILogger _logger;
    private readonly Func<string, int, TimeSpan, CancellationToken, Task<bool>> _probe;

    public EzvizWebcamProvider(EzvizOptions options, HttpClient? http = null, ILogger<EzvizWebcamProvider>? logger = null)
        : this(options, http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) }, logger, null)
    {
    }

    internal EzvizWebcamProvider(EzvizOptions options, HttpClient http, ILogger? logger, Func<string, int, TimeSpan, CancellationToken, Task<bool>>? probe)
    {
        _options = options;
        _http = http;
        _api = new EzvizOpenApiClient(http, options);
        _logger = logger ?? NullLogger.Instance;
        _probe = probe ?? ProbeTcpAsync;
    }

    /// <summary>The API client, for Open Platform calls the provider does not wrap.</summary>
    public EzvizOpenApiClient Api => _api;

    public async Task<WebcamInfo> GetInfoAsync(WebcamDevice device, CancellationToken cancellationToken = default)
    {
        CloudDevice? cloud = await TryGetCloudDeviceAsync(device, cancellationToken).ConfigureAwait(false);
        var local = await ResolveLocalEndpointAsync(device, cloud, cancellationToken).ConfigureAwait(false);

        var features = WebcamFeatures.None;
        bool? privacy = null;
        if (cloud is { Online: true })
        {
            var capacity = await TryPostAsync("/api/lapp/device/capacity", [new("deviceSerial", device.Serial)], cancellationToken).ConfigureAwait(false);
            if (capacity is { } caps)
            {
                bool ptz = Flag(caps, "support_ptz");
                if (ptz && Flag(caps, "ptz_left_right")) features |= WebcamFeatures.Pan;
                if (ptz && Flag(caps, "ptz_top_bottom")) features |= WebcamFeatures.Tilt;
                if (ptz && Flag(caps, "ptz_zoom")) features |= WebcamFeatures.Zoom;
                if (Flag(caps, "support_privacy") || Flag(caps, "ptz_close_scene")) features |= WebcamFeatures.Privacy;
                if (Flag(caps, "support_capture")) features |= WebcamFeatures.Capture;
                if (Flag(caps, "ptz_center_mirror")) features |= WebcamFeatures.Mirror;
            }
            if ((features & WebcamFeatures.Privacy) != 0)
            {
                var status = await TryPostAsync("/api/lapp/device/scene/switch/status", Channel(device), cancellationToken).ConfigureAwait(false);
                if (status is { } s && s.TryGetProperty("enable", out var enable)) privacy = ReadInt(enable) == 1;
            }
        }

        return new WebcamInfo(device.Name ?? cloud?.Name ?? device.Serial, cloud is { Online: true } || local != null, features)
        {
            Model = cloud?.Model,
            PrivacyEnabled = privacy,
            IsOnLocalNetwork = local != null,
        };
    }

    public async Task<WebcamStream> GetLiveStreamAsync(WebcamDevice device, WebcamQuality quality, CancellationToken cancellationToken = default)
    {
        CloudDevice? cloud = null;
        if (_options.PreferLocalNetwork)
        {
            if (device.LocalAddress == null) cloud = await TryGetCloudDeviceAsync(device, cancellationToken).ConfigureAwait(false);
            var local = await ResolveLocalEndpointAsync(device, cloud, cancellationToken).ConfigureAwait(false);
            if (local is { } endpoint)
            {
                if (string.IsNullOrEmpty(device.VerifyCode))
                    throw new WebcamException("The camera is on the local network; its verification code is needed to stream from it.", "verifycode");
                return new WebcamStream(BuildRtspUri(endpoint.Host, endpoint.Port, device, quality), null, WebcamTransport.LocalNetwork, quality);
            }
        }

        if (!_options.EnableCloudRelay)
            throw new WebcamException(cloud is { Online: false }
                ? "The camera is offline and not on this network."
                : "The camera is not on this network (cloud relay is disabled).", cloud is { Online: false } ? "20007" : "unreachable");

        return await GetRelayStreamAsync(device, quality, cancellationToken).ConfigureAwait(false);
    }

    public Task StartPtzAsync(WebcamDevice device, PtzDirection direction, PtzSpeed speed, CancellationToken cancellationToken = default) =>
        _api.PostAsync("/api/lapp/device/ptz/start",
        [
            .. Channel(device),
            new("direction", DirectionCode(direction)),
            new("speed", speed switch { PtzSpeed.Slow => "0", PtzSpeed.Fast => "2", _ => "1" }),
        ], cancellationToken);

    public Task StopPtzAsync(WebcamDevice device, PtzDirection direction, CancellationToken cancellationToken = default) =>
        _api.PostAsync("/api/lapp/device/ptz/stop", [.. Channel(device), new("direction", DirectionCode(direction))], cancellationToken);

    public async Task<WebcamSnapshot> CaptureAsync(WebcamDevice device, CancellationToken cancellationToken = default)
    {
        var data = await _api.PostAsync("/api/lapp/device/capture", Channel(device), cancellationToken).ConfigureAwait(false);
        var url = data.TryGetProperty("picUrl", out var pic) ? pic.GetString() : null;
        if (string.IsNullOrEmpty(url)) throw new WebcamException("The device returned no picture.", "capture");
        var bytes = await _http.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
        return new WebcamSnapshot(bytes, url);
    }

    public Task SetPrivacyAsync(WebcamDevice device, bool enabled, CancellationToken cancellationToken = default) =>
        _api.PostAsync("/api/lapp/device/scene/switch/set", [.. Channel(device), new("enable", enabled ? "1" : "0")], cancellationToken);

    public Task MirrorAsync(WebcamDevice device, CancellationToken cancellationToken = default) =>
        // command 2 = centre mirror: flips both axes, i.e. rotates the picture by 180°.
        _api.PostAsync("/api/lapp/device/ptz/mirror", [.. Channel(device), new("command", "2")], cancellationToken);

    // ---- streams -----------------------------------------------------------------

    private string BuildRtspUri(string host, int port, WebcamDevice device, WebcamQuality quality)
    {
        var builder = new UriBuilder("rtsp", host, port,
            $"/h264/ch{device.Channel.ToString(CultureInfo.InvariantCulture)}/{(quality == WebcamQuality.High ? "main" : "sub")}/av_stream")
        {
            UserName = Uri.EscapeDataString(_options.LocalUser),
            Password = Uri.EscapeDataString(device.VerifyCode!),
        };
        return builder.Uri.ToString();
    }

    private async Task<WebcamStream> GetRelayStreamAsync(WebcamDevice device, WebcamQuality quality, CancellationToken cancellationToken)
    {
        var parameters = new List<KeyValuePair<string, string>>(Channel(device))
        {
            new("protocol", "2"), // HLS
            new("quality", quality == WebcamQuality.High ? "1" : "2"),
            new("expireTime", "86400"),
        };
        if (!string.IsNullOrEmpty(device.VerifyCode)) parameters.Add(new("code", device.VerifyCode));
        var data = await _api.PostAsync("/api/lapp/v2/live/address/get", parameters, cancellationToken).ConfigureAwait(false);
        var url = data.TryGetProperty("url", out var u) ? u.GetString() : null;
        if (string.IsNullOrEmpty(url)) throw new WebcamException("Ezviz returned no live address.", "live");

        // The relay answers a refused stream with a playlist of one error clip (…/ErrCode/9048_0.ts),
        // which would play as a picture of an error: report it instead.
        string playlist = await _http.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        int err = playlist.IndexOf("/ErrCode/", StringComparison.Ordinal);
        if (err >= 0)
        {
            var code = new string(playlist.AsSpan(err + "/ErrCode/".Length).ToArray().TakeWhile(char.IsDigit).ToArray());
            throw new WebcamException(EzvizOpenApiClient.Describe(code, "The cloud refused the stream."), code);
        }
        return new WebcamStream(url, "application/vnd.apple.mpegurl", WebcamTransport.CloudRelay, quality);
    }

    private async Task<(string Host, int Port)?> ResolveLocalEndpointAsync(WebcamDevice device, CloudDevice? cloud, CancellationToken cancellationToken)
    {
        string? address = device.LocalAddress ?? cloud?.LocalAddress;
        if (string.IsNullOrWhiteSpace(address)) return null;
        var (host, port) = SplitHostPort(address.Trim(), _options.RtspPort);
        bool reachable = await _probe(host, port, _options.LocalProbeTimeout, cancellationToken).ConfigureAwait(false);
        if (!reachable) _logger.LogInformation("Camera {Device} not reachable at {Host}:{Port}", device, host, port);
        return reachable ? (host, port) : null;
    }

    internal static (string Host, int Port) SplitHostPort(string address, int defaultPort)
    {
        // [v6]:port, host:port, bare host / v6
        if (address.StartsWith('['))
        {
            int close = address.IndexOf(']');
            if (close > 0)
            {
                string host6 = address[1..close];
                return close + 1 < address.Length && address[close + 1] == ':' &&
                       int.TryParse(address[(close + 2)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int p6) ? (host6, p6) : (host6, defaultPort);
            }
        }
        int colon = address.LastIndexOf(':');
        if (colon > 0 && address.IndexOf(':') == colon &&
            int.TryParse(address[(colon + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int port))
            return (address[..colon], port);
        return (address, defaultPort);
    }

    private static async Task<bool> ProbeTcpAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            using var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    // ---- cloud -------------------------------------------------------------------

    private sealed record CloudDevice(string? Name, bool Online, string? LocalAddress, string? Model);

    // Null when the cloud is not configured or does not know the device; the local network may still work.
    private async Task<CloudDevice?> TryGetCloudDeviceAsync(WebcamDevice device, CancellationToken cancellationToken)
    {
        var data = await TryPostAsync("/api/lapp/device/info", [new("deviceSerial", device.Serial)], cancellationToken).ConfigureAwait(false);
        if (data is not { ValueKind: JsonValueKind.Object } info) return null;
        return new CloudDevice(
            Str(info, "deviceName"),
            info.TryGetProperty("status", out var status) && ReadInt(status) == 1,
            Str(info, "localAddress"),
            Str(info, "model"));
    }

    private async Task<JsonElement?> TryPostAsync(string path, KeyValuePair<string, string>[] parameters, CancellationToken cancellationToken)
    {
        if (!_api.HasCredentials) return null;
        try
        {
            return await _api.PostAsync(path, parameters, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is WebcamException or HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Ezviz {Path} failed: {Message}", path, ex.Message);
            return null;
        }
    }

    private static KeyValuePair<string, string>[] Channel(WebcamDevice device) =>
    [
        new("deviceSerial", device.Serial),
        new("channelNo", device.Channel.ToString(CultureInfo.InvariantCulture)),
    ];

    private static string DirectionCode(PtzDirection direction) => direction switch
    {
        PtzDirection.Up => "0",
        PtzDirection.Down => "1",
        PtzDirection.Left => "2",
        PtzDirection.Right => "3",
        PtzDirection.UpLeft => "4",
        PtzDirection.DownLeft => "5",
        PtzDirection.UpRight => "6",
        PtzDirection.DownRight => "7",
        PtzDirection.ZoomIn => "8",
        PtzDirection.ZoomOut => "9",
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };

    private static bool Flag(JsonElement caps, string name) =>
        caps.TryGetProperty(name, out var value) && ReadInt(value) is int n && n != 0;

    private static int? ReadInt(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number when value.TryGetInt32(out int n) => n,
        JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) => n,
        _ => null,
    };

    private static string? Str(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && v.GetString() is { Length: > 0 } s ? s : null;
}
