using System.Globalization;

namespace Miko.Components.Webcam;

/// <summary>
/// One camera channel as a provider addresses it: the device serial and channel, plus what direct
/// LAN streaming needs — the device's verification code (its local password) and, optionally, its
/// address on the local network.
/// </summary>
/// <param name="Serial">Device serial number (e.g. <c>AB1234567</c>).</param>
/// <param name="Channel">Channel number, 1-based (an NVR has one per camera).</param>
public sealed record WebcamDevice(string Serial, int Channel = 1)
{
    /// <summary>
    /// The verification code on the device label (Ezviz: six capital letters). It is the device's local
    /// password: without it the camera cannot be streamed directly.
    /// </summary>
    public string? VerifyCode { get; init; }

    /// <summary>
    /// Host or IP (optionally <c>host:port</c> for RTSP) of the device on the local network. When
    /// absent the provider uses the address the cloud reports for the device.
    /// </summary>
    public string? LocalAddress { get; init; }

    /// <summary>Display name; providers fall back to the name the cloud knows, then the serial.</summary>
    public string? Name { get; init; }

    /// <summary>
    /// Parses an EZOpen address — <c>ezopen://[verifyCode@]open.ys7.com/{serial}/{channel}[.hd].live</c>.
    /// The <c>.hd</c> suffix selects <see cref="WebcamQuality.High"/>, reported through <paramref name="quality"/>.
    /// </summary>
    public static WebcamDevice ParseEzopen(string address, out WebcamQuality quality)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != "ezopen")
            throw new FormatException($"'{address}' is not an ezopen:// address.");
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length < 2 || segments[0].Length == 0)
            throw new FormatException($"'{address}' does not name a device and channel (ezopen://open.ys7.com/SERIAL/1.live).");
        var channelPart = segments[1].Split('.');
        if (!int.TryParse(channelPart[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int channel) || channel < 1)
            throw new FormatException($"'{address}' has no valid channel number.");
        quality = channelPart.Contains("hd", StringComparer.OrdinalIgnoreCase) ? WebcamQuality.High : WebcamQuality.Smooth;
        return new WebcamDevice(segments[0].ToUpperInvariant(), channel)
        {
            VerifyCode = string.IsNullOrEmpty(uri.UserInfo) ? null : Uri.UnescapeDataString(uri.UserInfo),
        };
    }

    /// <summary>The EZOpen address of this channel (without the verification code).</summary>
    public string ToEzopen(WebcamQuality quality = WebcamQuality.Smooth) =>
        $"ezopen://open.ys7.com/{Serial}/{Channel.ToString(CultureInfo.InvariantCulture)}{(quality == WebcamQuality.High ? ".hd" : "")}.live";

    // The verification code is a password: keep it out of logs and debugger displays.
    public override string ToString() => $"{Serial}/{Channel.ToString(CultureInfo.InvariantCulture)}";
}

/// <summary>Live stream quality.</summary>
public enum WebcamQuality
{
    /// <summary>The camera's sub stream (Ezviz "流畅" / "标清"): low bitrate, quick to start.</summary>
    Smooth,

    /// <summary>The camera's main stream (Ezviz "高清"): full resolution.</summary>
    High,
}

/// <summary>What a camera can do beyond live video, as far as its provider can drive it.</summary>
[Flags]
public enum WebcamFeatures
{
    None = 0,

    /// <summary>Pan left / right.</summary>
    Pan = 1,

    /// <summary>Tilt up / down.</summary>
    Tilt = 2,

    /// <summary>Optical zoom in / out.</summary>
    Zoom = 4,

    /// <summary>Privacy mode: the lens is turned away or covered.</summary>
    Privacy = 8,

    /// <summary>Snapshots taken by the device.</summary>
    Capture = 16,

    /// <summary>Flipping the picture (for cameras mounted upside down).</summary>
    Mirror = 32,
}

/// <summary>A camera as reported by its provider.</summary>
/// <param name="Name">Display name.</param>
/// <param name="IsOnline">Whether the provider can reach the device (cloud or LAN).</param>
/// <param name="Features">Controls the provider can drive for this device now.</param>
public sealed record WebcamInfo(string Name, bool IsOnline, WebcamFeatures Features)
{
    /// <summary>Device model, when known.</summary>
    public string? Model { get; init; }

    /// <summary>Whether privacy mode is on; null when unknown.</summary>
    public bool? PrivacyEnabled { get; init; }

    /// <summary>Qualities the live stream offers.</summary>
    public IReadOnlyList<WebcamQuality> Qualities { get; init; } = [WebcamQuality.Smooth, WebcamQuality.High];

    /// <summary>Whether the device is reachable without the cloud, on the local network.</summary>
    public bool IsOnLocalNetwork { get; init; }
}

/// <summary>How a live stream reaches the app.</summary>
public enum WebcamTransport
{
    /// <summary>Straight from the camera on the local network (no server, no traffic cost).</summary>
    LocalNetwork,

    /// <summary>Relayed by the vendor's cloud.</summary>
    CloudRelay,
}

/// <summary>A playable live stream: what to put into <c>&lt;video src&gt;</c>.</summary>
/// <param name="Source">The source URI (<c>rtsp://…</c>, an HLS playlist, a custom scheme with a registered source factory).</param>
/// <param name="MimeType">MIME type for the backend, if the URI does not say it.</param>
/// <param name="Transport">Local or relayed.</param>
/// <param name="Quality">The quality actually delivered.</param>
public sealed record WebcamStream(string Source, string? MimeType, WebcamTransport Transport, WebcamQuality Quality)
{
    // The source of a direct stream carries the camera password.
    public override string ToString() => $"{Transport} {Quality}";
}

/// <summary>Pan / tilt / zoom directions.</summary>
public enum PtzDirection
{
    Up,
    Down,
    Left,
    Right,
    UpLeft,
    DownLeft,
    UpRight,
    DownRight,
    ZoomIn,
    ZoomOut,
}

/// <summary>Pan / tilt / zoom speed.</summary>
public enum PtzSpeed
{
    Slow,
    Normal,
    Fast,
}

/// <summary>A snapshot taken by the device.</summary>
/// <param name="Data">The encoded image (JPEG, usually).</param>
/// <param name="Url">Where the provider stored it, when it did (Ezviz keeps captures for a few hours).</param>
public sealed record WebcamSnapshot(byte[] Data, string? Url)
{
    public DateTimeOffset TakenAt { get; init; } = DateTimeOffset.Now;
}

/// <summary>A provider-side failure, with the provider's error code when it has one.</summary>
public class WebcamException : Exception
{
    public WebcamException(string message, string? code = null, Exception? inner = null) : base(message, inner) => Code = code;

    /// <summary>The provider's error code (Ezviz: <c>20007</c> = device offline, <c>60002</c>–<c>60005</c> = PTZ limit, …).</summary>
    public string? Code { get; }
}

/// <summary>
/// The camera backend behind <see cref="MikoWebcam"/>: device information, the live stream, pan / tilt /
/// zoom, snapshots and privacy mode. Ezviz (<see cref="Ezviz.EzvizWebcamProvider"/>) is the default;
/// replace the registration to support another vendor or a private protocol.
/// </summary>
/// <remarks>
/// <para>
/// A provider returns a stream as a <c>&lt;video&gt;</c> source. A standard protocol needs nothing
/// else: <c>rtsp://</c> plays through the platform decoder (registered by <c>AddMikoWebcam()</c>), HLS
/// through the video backend. A private protocol registers an
/// <see cref="Miko.Platform.Video.IEncodedVideoSourceFactory"/> for its own scheme and returns that
/// scheme here: the factory delivers H.264 / H.265 access units, the platform decodes them.
/// </para>
/// <para>
/// Calls come from the UI thread and may run concurrently for different devices. A feature the device
/// lacks throws <see cref="NotSupportedException"/>; a vendor error throws <see cref="WebcamException"/>.
/// </para>
/// </remarks>
public interface IWebcamProvider
{
    Task<WebcamInfo> GetInfoAsync(WebcamDevice device, CancellationToken cancellationToken = default);

    /// <summary>Resolves the live stream, preferring the local network.</summary>
    Task<WebcamStream> GetLiveStreamAsync(WebcamDevice device, WebcamQuality quality, CancellationToken cancellationToken = default);

    /// <summary>Starts moving; the camera keeps moving until <see cref="StopPtzAsync"/> (or its own limit).</summary>
    Task StartPtzAsync(WebcamDevice device, PtzDirection direction, PtzSpeed speed, CancellationToken cancellationToken = default);

    Task StopPtzAsync(WebcamDevice device, PtzDirection direction, CancellationToken cancellationToken = default);

    Task<WebcamSnapshot> CaptureAsync(WebcamDevice device, CancellationToken cancellationToken = default);

    Task SetPrivacyAsync(WebcamDevice device, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>Flips the picture (rotates it by 180°).</summary>
    Task MirrorAsync(WebcamDevice device, CancellationToken cancellationToken = default);
}
