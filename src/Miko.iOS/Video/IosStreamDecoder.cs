using CoreMedia;
using CoreVideo;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;
using ObjCRuntime;
using VideoToolbox;

namespace Miko.iOS.Video;

/// <summary>
/// Decodes a pushed H.264 / H.265 stream (a live camera) with VideoToolbox, into IOSurface-backed NV12
/// pixel buffers that <see cref="IosVideoFrameSource"/> maps into Metal zero-copy — the path AVPlayer
/// playback uses. The macOS desktop backend does the same through P/Invoke
/// (<c>VideoToolboxStreamDecoder</c>); here the managed bindings exist.
/// </summary>
/// <remarks>
/// The format description comes from the parameter sets of each key frame; when they change (a new
/// resolution) the session is recreated. Slices are passed
/// length-prefixed, parameter sets and delimiters left out. Decoding is synchronous (one frame of
/// latency); a bad frame is skipped, only a run of failures is reported.
/// </remarks>
internal sealed class IosStreamDecoder : IVideoStreamDecoder
{
    private const int MaxConsecutiveErrors = 60;

    private readonly VideoCodec _codec;
    private readonly ILogger _logger;
    private readonly IosVideoFrameSource _frames = new();
    private readonly object _lock = new();
    private CMVideoFormatDescription? _format;
    private VTDecompressionSession? _session;
    private byte[][] _parameterSets = [];
    private int _width, _height, _errors;
    private bool _disposed;

    public IosStreamDecoder(VideoCodec codec, ILogger? logger = null)
    {
        _codec = codec;
        _logger = logger ?? NullLogger.Instance;
    }

    public event Action<VideoSessionEvent>? Event;

    public IVideoFrameSource FrameSource => _frames;

    public void Decode(in EncodedVideoFrame frame)
    {
        var data = frame.Data.Span;
        lock (_lock)
        {
            if (_disposed) return;
            if (frame.IsKeyFrame) Configure(data);
            if (_session == null || _format == null) return;

            var sample = new byte[data.Length + 4];
            int length = NalUnits.WriteLengthPrefixed(_codec, data, sample);
            if (length == 0) return;
            if (length != sample.Length) Array.Resize(ref sample, length);

            using var block = CMBlockBuffer.FromMemoryBlock(sample, 0, CMBlockBufferFlags.AssureMemoryNow, out var blockError);
            if (block == null || blockError != CMBlockBufferError.None) return;
            var timing = new CMSampleTimingInfo
            {
                Duration = CMTime.Invalid,
                PresentationTimeStamp = new CMTime(frame.Timestamp.Ticks / 100, 100_000),
                DecodeTimeStamp = CMTime.Invalid,
            };
            using var buffer = CMSampleBuffer.CreateReady(block, _format, 1, [timing], [(nuint)length], out var sampleError);
            if (buffer == null || sampleError != CMSampleBufferError.None) return;

            var status = _session.DecodeFrame(buffer, 0, IntPtr.Zero, out _);
            if (status == VTStatus.InvalidSession)
            {
                // The app went to the background (or the GPU reset): start over at the next key frame.
                _logger.LogInformation("VideoToolbox session invalidated; recreating it at the next key frame");
                DestroySession();
                _format?.Dispose();
                _format = null;
                _parameterSets = [];
            }
            else if (status != VTStatus.Ok)
            {
                OnFrameError(status);
            }
        }
    }

    private void Configure(ReadOnlySpan<byte> accessUnit)
    {
        var sets = NalUnits.GetParameterSets(_codec, accessUnit);
        if (sets.Count == 0) return;
        if (_format != null && sets.Count == _parameterSets.Length &&
            sets.Zip(_parameterSets).All(p => p.First.AsSpan().SequenceEqual(p.Second)))
            return;

        CMFormatDescriptionError error;
        var format = _codec == VideoCodec.H264
            ? CMVideoFormatDescription.FromH264ParameterSets(sets, 4, out error)
            : CMVideoFormatDescription.FromHevcParameterSets(sets, 4, null, out error);
        if (format == null || error != CMFormatDescriptionError.None)
        {
            _logger.LogWarning("VideoToolbox rejected the {Codec} parameter sets ({Error})", _codec, error);
            return;
        }
        _parameterSets = [.. sets];

        // New parameter sets mean a new resolution (or stream): a fresh session. The binding's
        // CanAcceptFormatDescriptor returns the native Boolean typed as VTStatus, which reads inverted.
        DestroySession();
        _format?.Dispose();
        _format = format;
        if (_session == null) CreateSession();
    }

    private void CreateSession()
    {
        var attributes = new CVPixelBufferAttributes
        {
            PixelFormatType = CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange,
            MetalCompatibility = true,
        };
        _session = VTDecompressionSession.Create(OnDecoded, _format!, (VTVideoDecoderSpecification?)null, attributes);
        if (_session == null) Fail($"VideoToolbox could not create a {_codec} decoder.");
    }

    private void OnDecoded(IntPtr sourceFrame, VTStatus status, VTDecodeInfoFlags flags, CVImageBuffer? buffer, CMTime presentationTimeStamp, CMTime presentationDuration)
    {
        try
        {
            if (status != VTStatus.Ok)
            {
                OnFrameError(status);
                return;
            }
            if (buffer == null) return; // dropped
            // A wrapper of our own (+1): the frame source keeps it until the next frame replaces it.
            var pixels = Runtime.GetINativeObject<CVPixelBuffer>(buffer.Handle, false);
            if (pixels == null) return;
            _errors = 0;
            int width = (int)pixels.Width, height = (int)pixels.Height;
            var pts = TimeSpan.FromSeconds(presentationTimeStamp.Seconds);
            _frames.PushPixelBuffer(pixels, pts);
            if (width != _width || height != _height)
            {
                _width = width;
                _height = height;
                Event?.Invoke(new VideoSessionEvent.Loaded(width, height, TimeSpan.Zero));
            }
            Event?.Invoke(new VideoSessionEvent.FrameAvailable(pts));
        }
        catch (Exception ex)
        {
            // Never let an exception unwind into VideoToolbox.
            _logger.LogDebug(ex, "Handling a decoded frame failed");
        }
    }

    private void OnFrameError(VTStatus status)
    {
        if (++_errors == MaxConsecutiveErrors)
            Fail($"VideoToolbox failed to decode {MaxConsecutiveErrors} frames in a row ({status}).");
        else
            _logger.LogDebug("VideoToolbox skipped a frame ({Status})", status);
    }

    private void Fail(string message)
    {
        _logger.LogWarning("{Message}", message);
        Event?.Invoke(new VideoSessionEvent.Error(message, null));
    }

    private void DestroySession()
    {
        if (_session == null) return;
        _session.Dispose();
        _session = null;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            DestroySession();
            _format?.Dispose();
            _format = null;
        }
        _frames.Dispose();
    }
}
