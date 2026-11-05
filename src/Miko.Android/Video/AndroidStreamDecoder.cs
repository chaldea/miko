using Android.Media;
using Java.Nio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;

namespace Miko.Android.Video;

/// <summary>
/// Decodes a pushed H.264 / H.265 stream (a live camera) with <c>MediaCodec</c>, straight into the
/// <c>SurfaceTexture</c> of an <see cref="AndroidVideoFrameSource"/> — the zero-copy path file
/// playback uses.
/// </summary>
/// <remarks>
/// <para>
/// The codec is configured at the first key frame: its parameter sets become the codec-specific data
/// (<c>csd-0</c> = VPS + SPS + PPS for HEVC; <c>csd-0</c> = SPS, <c>csd-1</c> = PPS for AVC). The
/// picture size is a placeholder until the decoder reports the real one
/// (<c>INFO_OUTPUT_FORMAT_CHANGED</c>), which is what raises <see cref="VideoSessionEvent.Loaded"/>.
/// New parameter sets (a resolution change) recreate the codec.
/// </para>
/// <para>
/// Input is queued from the caller's thread; output is drained on a thread of its own and released
/// to the surface as soon as it is ready (live: no pacing). The output surface is created by the
/// render thread on its first paint of the video, so the first key frame waits for it.
/// </para>
/// </remarks>
internal sealed class AndroidStreamDecoder : IVideoStreamDecoder
{
    private const long InputTimeoutUs = 50_000;
    private const long OutputTimeoutUs = 20_000;
    private const int MaxInputSize = 4 * 1024 * 1024;

    private readonly VideoCodec _codec;
    private readonly ILogger _logger;
    private readonly AndroidVideoFrameSource _frames = new();
    private readonly object _lock = new();
    private MediaCodec? _decoder;
    private Thread? _output;
    private byte[][] _parameterSets = [];
    private volatile bool _disposed;
    private volatile bool _stopOutput;
    private int _width, _height;
    private int _failures;
    private byte[] _copy = new byte[256 * 1024];

    public AndroidStreamDecoder(VideoCodec codec, ILogger? logger = null)
    {
        _codec = codec;
        _logger = logger ?? NullLogger.Instance;
    }

    public event Action<VideoSessionEvent>? Event;

    public IVideoFrameSource FrameSource => _frames;

    internal static string MimeType(VideoCodec codec) => codec == VideoCodec.H264 ? MediaFormat.MimetypeVideoAvc : MediaFormat.MimetypeVideoHevc;

    public void Decode(in EncodedVideoFrame frame)
    {
        lock (_lock)
        {
            if (_disposed) return;
            if (frame.IsKeyFrame) Configure(frame.Data.Span);
            var decoder = _decoder;
            if (decoder == null) return;

            try
            {
                int index = decoder.DequeueInputBuffer(InputTimeoutUs);
                if (index < 0)
                {
                    // The decoder is behind: drop this frame rather than the stream's latency.
                    _logger.LogDebug("MediaCodec has no free input buffer; frame dropped");
                    return;
                }
                ByteBuffer? input = decoder.GetInputBuffer(index);
                if (input == null) return;
                input.Clear();
                var data = frame.Data.Span;
                if (data.Length > input.Capacity())
                {
                    decoder.QueueInputBuffer(index, 0, 0, 0, MediaCodecBufferFlags.None);
                    return;
                }
                if (_copy.Length < data.Length) _copy = new byte[Math.Max(data.Length, _copy.Length * 2)];
                data.CopyTo(_copy);
                input.Put(_copy, 0, data.Length);
                decoder.QueueInputBuffer(index, 0, data.Length, frame.Timestamp.Ticks / 10,
                    frame.IsKeyFrame ? MediaCodecBufferFlags.KeyFrame : MediaCodecBufferFlags.None);
                _failures = 0;
            }
            catch (Exception ex) when (ex is Java.Lang.IllegalStateException or MediaCodec.CodecException)
            {
                // A codec in error state does not come back: start again at the next key frame.
                _logger.LogWarning(ex, "MediaCodec input failed; reconfiguring at the next key frame");
                ReleaseCodec();
                if (++_failures >= 3) Event?.Invoke(new VideoSessionEvent.Error("The video decoder failed.", ex));
            }
        }
    }

    private void Configure(ReadOnlySpan<byte> accessUnit)
    {
        var sets = NalUnits.GetParameterSets(_codec, accessUnit);
        if (sets.Count == 0) return;
        if (_decoder != null && sets.Count == _parameterSets.Length &&
            sets.Zip(_parameterSets).All(p => p.First.AsSpan().SequenceEqual(p.Second)))
            return;
        if (_decoder != null)
        {
            _logger.LogInformation("Stream parameters changed; recreating the {Codec} decoder", _codec);
            ReleaseCodec();
        }

        var surface = _frames.WaitForSurface(TimeSpan.FromSeconds(5));
        if (surface == null || _disposed)
        {
            _logger.LogWarning("The video surface was not created; waiting for the next key frame");
            return;
        }

        string mime = MimeType(_codec);
        // The size is a hint: the decoder takes the real one from the SPS and reports it.
        var format = MediaFormat.CreateVideoFormat(mime, 1920, 1080);
        format.SetInteger(MediaFormat.KeyMaxInputSize, MaxInputSize);
        if (OperatingSystem.IsAndroidVersionAtLeast(30)) format.SetInteger(MediaFormat.KeyLowLatency, 1);
        if (_codec == VideoCodec.H265)
        {
            format.SetByteBuffer("csd-0", ByteBuffer.Wrap(AnnexB(sets)));
        }
        else
        {
            format.SetByteBuffer("csd-0", ByteBuffer.Wrap(AnnexB(sets.Take(1))));
            format.SetByteBuffer("csd-1", ByteBuffer.Wrap(AnnexB(sets.Skip(1))));
        }

        try
        {
            var decoder = MediaCodec.CreateDecoderByType(mime);
            decoder.Configure(format, surface, null, MediaCodecConfigFlags.None);
            decoder.Start();
            _decoder = decoder;
            _parameterSets = [.. sets];
            _stopOutput = false;
            _output = new Thread(() => DrainOutput(decoder)) { IsBackground = true, Name = "miko-stream-mediacodec" };
            _output.Start();
            _logger.LogInformation("MediaCodec {Name} decoding {Codec}", decoder.Name, _codec);
        }
        catch (Exception ex) when (ex is Java.IO.IOException or Java.Lang.IllegalArgumentException or MediaCodec.CodecException or Java.Lang.IllegalStateException)
        {
            Event?.Invoke(new VideoSessionEvent.Error($"This device cannot decode {_codec} ({ex.Message}).", ex));
        }
    }

    private void DrainOutput(MediaCodec decoder)
    {
        var info = new MediaCodec.BufferInfo();
        while (!_stopOutput)
        {
            int index;
            try
            {
                index = decoder.DequeueOutputBuffer(info, OutputTimeoutUs);
            }
            catch (Java.Lang.IllegalStateException)
            {
                return; // stopped under us
            }
            if (index >= 0)
            {
                try { decoder.ReleaseOutputBuffer(index, render: true); }
                catch (Java.Lang.IllegalStateException) { return; }
                var pts = TimeSpan.FromTicks(info.PresentationTimeUs * 10);
                _frames.NotifyFrameAvailable(pts, _width, _height);
                Event?.Invoke(new VideoSessionEvent.FrameAvailable(pts));
            }
            else if (index == (int)MediaCodecInfoState.OutputFormatChanged)
            {
                ReadOutputSize(decoder.OutputFormat);
            }
        }
    }

    private void ReadOutputSize(MediaFormat format)
    {
        int width = format.GetInteger(MediaFormat.KeyWidth), height = format.GetInteger(MediaFormat.KeyHeight);
        // The crop rectangle is the picture; width / height may include alignment padding (1088 for 1080).
        if (format.ContainsKey("crop-right") && format.ContainsKey("crop-left"))
            width = format.GetInteger("crop-right") - format.GetInteger("crop-left") + 1;
        if (format.ContainsKey("crop-bottom") && format.ContainsKey("crop-top"))
            height = format.GetInteger("crop-bottom") - format.GetInteger("crop-top") + 1;
        if (width <= 0 || height <= 0 || (width == _width && height == _height)) return;
        _width = width;
        _height = height;
        Event?.Invoke(new VideoSessionEvent.Loaded(width, height, TimeSpan.Zero));
    }

    private static byte[] AnnexB(IEnumerable<byte[]> units)
    {
        var list = units.ToList();
        var buffer = new byte[list.Sum(u => u.Length + 4)];
        int at = 0;
        foreach (var unit in list)
        {
            NalUnits.StartCode.CopyTo(buffer.AsSpan(at));
            unit.CopyTo(buffer, at + 4);
            at += 4 + unit.Length;
        }
        return buffer;
    }

    private void ReleaseCodec()
    {
        var decoder = _decoder;
        _decoder = null;
        _parameterSets = [];
        _stopOutput = true;
        _output?.Join(1000);
        _output = null;
        if (decoder == null) return;
        try { decoder.Stop(); } catch (Java.Lang.IllegalStateException) { }
        decoder.Release();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // First: a Decode waiting for the surface returns at once.
        _frames.Dispose();
        lock (_lock) ReleaseCodec();
    }
}
