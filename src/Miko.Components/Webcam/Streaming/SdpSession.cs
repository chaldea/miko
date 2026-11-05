using System.Globalization;

namespace Miko.Components.Webcam.Streaming;

/// <summary>The media sections of an SDP description (RFC 8866) that RTSP playback needs.</summary>
internal sealed class SdpSession
{
    public List<SdpMedia> Media { get; } = new();

    /// <summary>Session-level <c>a=control</c>, if any.</summary>
    public string? Control { get; private set; }

    public static SdpSession Parse(string text)
    {
        var session = new SdpSession();
        SdpMedia? media = null;
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length < 2 || line[1] != '=') continue;
            var value = line[2..];
            switch (line[0])
            {
                case 'm':
                    media = SdpMedia.FromMediaLine(value);
                    if (media != null) session.Media.Add(media);
                    break;
                case 'a':
                    int colon = value.IndexOf(':');
                    var name = colon < 0 ? value : value[..colon];
                    var arg = colon < 0 ? "" : value[(colon + 1)..].Trim();
                    if (media == null)
                    {
                        if (name == "control") session.Control = arg;
                    }
                    else
                    {
                        media.ApplyAttribute(name, arg);
                    }
                    break;
            }
        }
        return session;
    }
}

/// <summary>One <c>m=</c> section.</summary>
internal sealed class SdpMedia
{
    private SdpMedia(string type, int payloadType)
    {
        Type = type;
        PayloadType = payloadType;
    }

    /// <summary><c>video</c>, <c>audio</c>, …</summary>
    public string Type { get; }

    /// <summary>The first payload type of the media line.</summary>
    public int PayloadType { get; }

    /// <summary>Encoding name from <c>a=rtpmap</c> (upper case), e.g. <c>H264</c>, <c>H265</c>.</summary>
    public string? Encoding { get; private set; }

    public int ClockRate { get; private set; } = 90_000;

    public string? Control { get; private set; }

    /// <summary><c>a=fmtp</c> parameters of <see cref="PayloadType"/>, keys lower case.</summary>
    public Dictionary<string, string> FormatParameters { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal static SdpMedia? FromMediaLine(string value)
    {
        // m=<media> <port> <proto> <fmt> ...
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 4 || !int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pt)) return null;
        return new SdpMedia(parts[0], pt);
    }

    internal void ApplyAttribute(string name, string arg)
    {
        switch (name)
        {
            case "control":
                Control = arg;
                break;
            case "rtpmap":
            {
                // a=rtpmap:<pt> <encoding>/<clock>[/<channels>]
                var parts = arg.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && parts[0] == PayloadType.ToString(CultureInfo.InvariantCulture))
                {
                    var encoding = parts[1].Split('/');
                    Encoding = encoding[0].Trim().ToUpperInvariant();
                    if (encoding.Length > 1 && int.TryParse(encoding[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int clock))
                        ClockRate = clock;
                }
                break;
            }
            case "fmtp":
            {
                var parts = arg.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2 && parts[0] == PayloadType.ToString(CultureInfo.InvariantCulture))
                {
                    foreach (var pair in parts[1].Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        int eq = pair.IndexOf('=');
                        if (eq > 0) FormatParameters[pair[..eq].Trim()] = pair[(eq + 1)..].Trim();
                    }
                }
                break;
            }
        }
    }

    /// <summary>
    /// Parameter sets from <c>sprop-vps</c> / <c>sprop-sps</c> / <c>sprop-pps</c> (H.265) or
    /// <c>sprop-parameter-sets</c> (H.264). Malformed values are skipped: cameras are known to put
    /// garbage there and send the real ones in band.
    /// </summary>
    public List<byte[]> GetParameterSets(Miko.Platform.Video.VideoCodec codec)
    {
        var sets = new List<byte[]>();
        IEnumerable<string> values = codec == Miko.Platform.Video.VideoCodec.H265
            ? new[] { "sprop-vps", "sprop-sps", "sprop-pps" }.Select(k => FormatParameters.GetValueOrDefault(k) ?? "")
            : (FormatParameters.GetValueOrDefault("sprop-parameter-sets") ?? "").Split(',');
        foreach (var value in values)
        {
            foreach (var item in value.Split(','))
            {
                if (item.Length == 0) continue;
                try
                {
                    var bytes = Convert.FromBase64String(item.Trim());
                    if (bytes.Length > 0 && Miko.Platform.Video.NalUnits.IsParameterSet(codec, Miko.Platform.Video.NalUnits.GetType(codec, bytes)))
                        sets.Add(bytes);
                }
                catch (FormatException)
                {
                }
            }
        }
        return sets;
    }
}
