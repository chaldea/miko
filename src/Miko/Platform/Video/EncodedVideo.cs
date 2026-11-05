namespace Miko.Platform.Video;

/// <summary>Compressed video codecs carried by an <see cref="IEncodedVideoSource"/>.</summary>
public enum VideoCodec
{
    /// <summary>H.264 / AVC.</summary>
    H264,

    /// <summary>H.265 / HEVC.</summary>
    H265,
}

/// <summary>
/// One access unit — every NAL unit of one picture — in Annex-B form: each NAL unit is preceded by a
/// <c>00 00 00 01</c> (or <c>00 00 01</c>) start code. Key frames carry their parameter sets
/// (SPS/PPS, plus the VPS for H.265) in band ahead of the first slice, the way IP cameras send them;
/// a decoder takes its configuration from there.
/// </summary>
public readonly struct EncodedVideoFrame
{
    public EncodedVideoFrame(ReadOnlyMemory<byte> data, TimeSpan timestamp, bool isKeyFrame)
    {
        Data = data;
        Timestamp = timestamp;
        IsKeyFrame = isKeyFrame;
    }

    /// <summary>
    /// The Annex-B bytes. Owned by the source and valid only until its next
    /// <see cref="IEncodedVideoSource.ReadFrameAsync"/>: a decoder copies what it keeps.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>Presentation time on the source's clock (starts near zero; only differences matter).</summary>
    public TimeSpan Timestamp { get; }

    /// <summary>Whether the frame is a random access point (IDR / IRAP) a decoder can start from.</summary>
    public bool IsKeyFrame { get; }
}

/// <summary>
/// A stream failure reported by an <see cref="IEncodedVideoSource"/>. <see cref="IsPermanent"/> failures
/// (wrong credentials, no such stream) end the session at once; others are retried.
/// </summary>
public class VideoStreamException : IOException
{
    public VideoStreamException(string message, bool isPermanent = false, Exception? inner = null) : base(message, inner)
    {
        IsPermanent = isPermanent;
    }

    /// <summary>Whether reconnecting cannot help.</summary>
    public bool IsPermanent { get; }
}

/// <summary>What an opened <see cref="IEncodedVideoSource"/> carries.</summary>
public sealed record EncodedVideoStreamInfo(VideoCodec Codec);

/// <summary>
/// A live compressed video stream (a camera over RTSP, a vendor's private protocol, …) pulled frame
/// by frame. The engine pairs it with the platform's hardware decoder
/// (<see cref="IVideoStreamDecoderFactory"/>) in an <see cref="EncodedVideoSession"/>, so a protocol
/// needs only managed code: no player, no bundled decoder.
/// </summary>
/// <remarks>
/// A source is opened once and read on one thread; <see cref="DisposeAsync"/> may run on another
/// and must make a pending read end. Reconnecting creates a new source from the factory. A failure
/// that reconnecting cannot fix throws a permanent <see cref="VideoStreamException"/>.
/// </remarks>
public interface IEncodedVideoSource : IAsyncDisposable
{
    /// <summary>Connects and negotiates the stream.</summary>
    Task<EncodedVideoStreamInfo> OpenAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The next access unit, or <c>null</c> when the stream ended. Its <see cref="EncodedVideoFrame.Data"/>
    /// stays valid until the next call.
    /// </summary>
    ValueTask<EncodedVideoFrame?> ReadFrameAsync(CancellationToken cancellationToken);

    /// <summary>Bytes received from the network so far, for bitrate displays.</summary>
    long BytesReceived { get; }
}

/// <summary>
/// Opens <see cref="IEncodedVideoSource"/>s for the <c>&lt;video&gt;</c> sources it recognises
/// (by scheme, typically). Registered in DI; the engine asks every registered factory before it
/// hands a source to the <see cref="IVideoBackend"/>.
/// </summary>
public interface IEncodedVideoSourceFactory
{
    /// <summary>Whether this factory handles <paramref name="source"/>.</summary>
    bool CanOpen(VideoSourceDescriptor source);

    /// <summary>Creates an unopened source for <paramref name="source"/>.</summary>
    IEncodedVideoSource Open(VideoSourceDescriptor source);
}

/// <summary>
/// Implemented by a video backend that can decode an elementary stream the app pushes to it, with
/// the platform's own decoder (VideoToolbox, MediaCodec, …). Backends without it cannot play
/// <see cref="IEncodedVideoSource"/>s.
/// </summary>
public interface IVideoStreamDecoderFactory
{
    /// <summary>Whether a decoder for <paramref name="codec"/> is available on this device.</summary>
    bool CanDecode(VideoCodec codec);

    /// <summary>Creates a decoder; it configures itself from the first key frame's parameter sets.</summary>
    IVideoStreamDecoder CreateDecoder(VideoCodec codec);
}

/// <summary>
/// A platform decoder fed with access units. Frames come out through <see cref="FrameSource"/>
/// (zero-copy where the platform allows it, like file playback).
/// </summary>
/// <remarks>
/// <see cref="Decode"/> runs on one thread at a time and copies the frame before it returns.
/// <see cref="Event"/> may fire on any thread: <see cref="VideoSessionEvent.Loaded"/> when the
/// picture size becomes known or changes (duration zero), <see cref="VideoSessionEvent.FrameAvailable"/>
/// per decoded frame, <see cref="VideoSessionEvent.Error"/> when the decoder fails.
/// </remarks>
public interface IVideoStreamDecoder : IDisposable
{
    /// <summary>Decodes one access unit. Frames before the first key frame may be dropped.</summary>
    void Decode(in EncodedVideoFrame frame);

    /// <summary>The decoded pictures, read by the render thread.</summary>
    IVideoFrameSource FrameSource { get; }

    event Action<VideoSessionEvent>? Event;
}
