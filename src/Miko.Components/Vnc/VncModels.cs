using System.Globalization;
using Miko.Components.Vnc.Protocol;

namespace Miko.Components.Vnc;

/// <summary>What <see cref="MikoVnc"/> is doing.</summary>
public enum VncState
{
    /// <summary>Not connected and not trying (no host yet, or the connect form is up).</summary>
    Idle,

    Connecting,

    /// <summary>Waiting for the user to enter the password.</summary>
    Authenticating,

    Connected,

    /// <summary>The connection ended or failed (<see cref="MikoVnc.ErrorMessage"/> says why).</summary>
    Disconnected,
}

/// <summary>Picture quality against bandwidth.</summary>
public enum VncQuality
{
    /// <summary>Exact pixels (Tight without JPEG, ZRLE): best on a LAN.</summary>
    Lossless,

    /// <summary>Photos and video as high-quality JPEG, everything else exact. The default.</summary>
    High,

    /// <summary>Stronger JPEG and compression: Wi-Fi, a remote office.</summary>
    Balanced,

    /// <summary>Strong JPEG and maximum compression: mobile networks.</summary>
    Low,
}

/// <summary>How the remote screen fits the view.</summary>
public enum VncScaling
{
    /// <summary>The whole screen, scaled to fit with bars (zoom with the toolbar from there).</summary>
    Fit,

    /// <summary>One remote pixel per logical pixel, panning to follow the pointer.</summary>
    Actual,
}

/// <summary>The remote desktop as the server described it.</summary>
public sealed record VncDesktopInfo(string Name, int Width, int Height, string Host, int Port, string ProtocolVersion, int SecurityType);

/// <summary>A VNC server address. Parses the forms viewers accept.</summary>
public static class VncAddress
{
    public const int DefaultPort = 5900;

    /// <summary>
    /// Parses <c>host</c>, <c>host:display</c> (below 100: port 5900 + display), <c>host:port</c>,
    /// <c>host::port</c> and <c>[ipv6]:port</c>.
    /// </summary>
    public static bool TryParse(string? text, out string host, out int port)
    {
        host = "";
        port = DefaultPort;
        if (string.IsNullOrWhiteSpace(text)) return false;
        text = text.Trim();
        if (text.StartsWith("vnc://", StringComparison.OrdinalIgnoreCase)) text = text[6..].TrimEnd('/');

        string? portText = null;
        bool display = false;
        if (text.StartsWith('['))
        {
            int close = text.IndexOf(']');
            if (close < 0) return false;
            host = text[1..close];
            var rest = text[(close + 1)..];
            if (rest.StartsWith("::", StringComparison.Ordinal)) portText = rest[2..];
            else if (rest.StartsWith(':')) { portText = rest[1..]; display = true; }
            else if (rest.Length > 0) return false;
        }
        else
        {
            int dbl = text.IndexOf("::", StringComparison.Ordinal);
            int colons = text.Count(c => c == ':');
            if (dbl > 0 && colons == 2)
            {
                host = text[..dbl];
                portText = text[(dbl + 2)..];
            }
            else if (colons == 1)
            {
                int c = text.IndexOf(':');
                host = text[..c];
                portText = text[(c + 1)..];
                display = true;
            }
            else if (colons == 0)
            {
                host = text;
            }
            else
            {
                host = text;   // a bare IPv6 address
            }
        }
        if (host.Length == 0) return false;
        if (portText != null)
        {
            if (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out int n) || n < 0 || n > 65535) return false;
            port = display && n < 100 ? DefaultPort + n : n;
        }
        return port > 0;
    }

    /// <summary>The address as written back into the connect form.</summary>
    public static string Format(string host, int port)
    {
        string h = host.Contains(':') ? $"[{host}]" : host;
        return port == DefaultPort ? h : $"{h}::{port.ToString(CultureInfo.InvariantCulture)}";
    }
}

internal static class VncQualityExtensions
{
    /// <summary>(encodings, JPEG quality or null, compression level) for a preset.</summary>
    public static (IReadOnlyList<int> Encodings, int? Jpeg, int Compress) Settings(this VncQuality quality) => quality switch
    {
        VncQuality.Lossless => ([VncEncoding.Zrle, VncEncoding.Tight, VncEncoding.Hextile, VncEncoding.Zlib, VncEncoding.Rre], null, 1),
        VncQuality.Balanced => (VncConnectOptions.DefaultEncodings, 6, 6),
        VncQuality.Low => (VncConnectOptions.DefaultEncodings, 3, 9),
        _ => (VncConnectOptions.DefaultEncodings, 8, 2),
    };
}
