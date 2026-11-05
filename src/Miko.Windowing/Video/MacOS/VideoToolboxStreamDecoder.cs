using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Miko.Platform.Video;
using static Miko.Windowing.MacOS.ObjC;
using static Miko.Windowing.Video.MacOS.AVFoundationInterop;

namespace Miko.Windowing.Video.MacOS;

/// <summary>
/// Decodes a pushed H.264 / H.265 stream (a live camera) with VideoToolbox, the decoder AVPlayer uses
/// itself. The pictures are IOSurface-backed NV12 pixel buffers handed to the same
/// <see cref="AVFoundationFrameSource"/> file playback uses, so they reach the Metal window zero-copy.
/// </summary>
/// <remarks>
/// <para>
/// Configuration comes from the parameter sets in front of each key frame: a new format description
/// is built when they change, and the session is recreated when VideoToolbox cannot take the new
/// one (a resolution change). Slices are passed length-prefixed (AVCC / HVCC); parameter sets and
/// delimiters are left out of the sample.
/// </para>
/// <para>
/// Decoding is synchronous — the output callback runs before <see cref="Decode"/> returns — which
/// keeps latency to one frame. A frame VideoToolbox rejects as bad data is skipped (the decoder
/// conceals it from the next key frame on); only a run of failures is reported as an error.
/// </para>
/// </remarks>
[SupportedOSPlatform("macos")]
internal sealed unsafe class VideoToolboxStreamDecoder : IVideoStreamDecoder
{
    private const int MaxConsecutiveErrors = 60;

    private readonly VideoCodec _codec;
    private readonly ILogger _logger;
    private readonly AVFoundationFrameSource _frames = new();
    private readonly object _lock = new();
    private GCHandle _self;
    private IntPtr _format;
    private IntPtr _session;
    private byte[][] _parameterSets = [];
    private int _width, _height;
    private int _errors;
    private bool _disposed;

    public VideoToolboxStreamDecoder(VideoCodec codec, ILogger? logger = null)
    {
        _codec = codec;
        _logger = logger ?? NullLogger.Instance;
        _self = GCHandle.Alloc(this, GCHandleType.Weak);
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
            if (_session == IntPtr.Zero) return;

            byte[] sample = ArrayPool<byte>.Shared.Rent(data.Length + 4);
            try
            {
                int length = NalUnits.WriteLengthPrefixed(_codec, data, sample);
                if (length == 0) return;
                DecodeSample(sample, length, frame.Timestamp);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sample);
            }
        }
    }

    // Builds the format description from the key frame's parameter sets; (re)creates the session.
    private void Configure(ReadOnlySpan<byte> accessUnit)
    {
        var sets = NalUnits.GetParameterSets(_codec, accessUnit);
        if (sets.Count == 0)
        {
            if (_session == IntPtr.Zero)
                _logger.LogDebug("Key frame without parameter sets; waiting for one that has them");
            return;
        }
        if (_format != IntPtr.Zero && sets.Count == _parameterSets.Length &&
            sets.Zip(_parameterSets).All(p => p.First.AsSpan().SequenceEqual(p.Second)))
            return;

        IntPtr format = CreateFormatDescription(sets);
        if (format == IntPtr.Zero) return;
        _parameterSets = [.. sets];

        if (_session != IntPtr.Zero && VTDecompressionSessionCanAcceptFormatDescription(_session, format) == 0)
            DestroySession();
        if (_format != IntPtr.Zero) CFRelease(_format);
        _format = format;
        if (_session == IntPtr.Zero) CreateSession();
    }

    private IntPtr CreateFormatDescription(List<byte[]> sets)
    {
        var handles = new GCHandle[sets.Count];
        var pointers = stackalloc IntPtr[sets.Count];
        var sizes = stackalloc nuint[sets.Count];
        try
        {
            for (int i = 0; i < sets.Count; i++)
            {
                handles[i] = GCHandle.Alloc(sets[i], GCHandleType.Pinned);
                pointers[i] = handles[i].AddrOfPinnedObject();
                sizes[i] = (nuint)sets[i].Length;
            }
            IntPtr format;
            int status = _codec == VideoCodec.H264
                ? CMVideoFormatDescriptionCreateFromH264ParameterSets(IntPtr.Zero, (nuint)sets.Count, pointers, sizes, 4, out format)
                : CMVideoFormatDescriptionCreateFromHEVCParameterSets(IntPtr.Zero, (nuint)sets.Count, pointers, sizes, 4, IntPtr.Zero, out format);
            if (status != 0)
            {
                _logger.LogWarning("VideoToolbox rejected the {Codec} parameter sets (OSStatus {Status})", _codec, status);
                return IntPtr.Zero;
            }
            return format;
        }
        finally
        {
            foreach (var handle in handles)
                if (handle.IsAllocated) handle.Free();
        }
    }

    private void CreateSession()
    {
        IntPtr attributes = CreateDestinationAttributes();
        try
        {
            var callback = new VTDecompressionOutputCallbackRecord
            {
                Callback = (IntPtr)(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int, uint, IntPtr, CMTime, CMTime, void>)&OnDecoded,
                RefCon = GCHandle.ToIntPtr(_self),
            };
            int status = VTDecompressionSessionCreate(IntPtr.Zero, _format, IntPtr.Zero, attributes, &callback, out _session);
            if (status != 0)
            {
                _session = IntPtr.Zero;
                Fail($"VideoToolbox could not create a {_codec} decoder (OSStatus {status}).");
            }
        }
        finally
        {
            if (attributes != IntPtr.Zero) CFRelease(attributes);
        }
    }

    private void DecodeSample(byte[] sample, int length, TimeSpan timestamp)
    {
        IntPtr block = IntPtr.Zero, buffer = IntPtr.Zero;
        try
        {
            if (CMBlockBufferCreateWithMemoryBlock(IntPtr.Zero, IntPtr.Zero, (nuint)length, IntPtr.Zero, IntPtr.Zero, 0, (nuint)length, 0, out block) != 0)
                return;
            fixed (byte* bytes = sample)
            {
                if (CMBlockBufferReplaceDataBytes((IntPtr)bytes, block, 0, (nuint)length) != 0) return;
            }
            var timing = new CMSampleTimingInfo
            {
                Duration = default,
                PresentationTimeStamp = new CMTime { Value = timestamp.Ticks / 100, Timescale = 100_000, Flags = CMTime.FlagValid },
                DecodeTimeStamp = default,
            };
            nuint size = (nuint)length;
            if (CMSampleBufferCreateReady(IntPtr.Zero, block, _format, 1, 1, &timing, 1, &size, out buffer) != 0) return;

            int status = VTDecompressionSessionDecodeFrame(_session, buffer, 0, IntPtr.Zero, out _);
            if (status == KVTInvalidSessionErr)
            {
                // The session died (GPU reset, app suspended): start over at the next key frame.
                _logger.LogInformation("VideoToolbox session invalidated; recreating it at the next key frame");
                DestroySession();
                if (_format != IntPtr.Zero) { CFRelease(_format); _format = IntPtr.Zero; }
                _parameterSets = [];
            }
            else if (status != 0)
            {
                OnFrameError(status);
            }
        }
        finally
        {
            if (buffer != IntPtr.Zero) CFRelease(buffer);
            if (block != IntPtr.Zero) CFRelease(block);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnDecoded(IntPtr refCon, IntPtr sourceFrameRefCon, int status, uint infoFlags, IntPtr imageBuffer, CMTime presentationTimeStamp, CMTime presentationDuration)
    {
        try
        {
            if (GCHandle.FromIntPtr(refCon).Target is not VideoToolboxStreamDecoder decoder) return;
            if (status != 0 || imageBuffer == IntPtr.Zero)
            {
                // Dropped frames (infoFlags & kVTDecodeInfo_FrameDropped) carry no picture and no error.
                if (status != 0) decoder.OnFrameError(status);
                return;
            }
            decoder.OnPicture(imageBuffer, TimeSpan.FromSeconds(presentationTimeStamp.Seconds));
        }
        catch (Exception)
        {
            // Never let an exception unwind into VideoToolbox.
        }
    }

    private void OnPicture(IntPtr imageBuffer, TimeSpan pts)
    {
        _errors = 0;
        int width = (int)CVPixelBufferGetWidth(imageBuffer);
        int height = (int)CVPixelBufferGetHeight(imageBuffer);
        // The frame source takes over a +1 reference.
        CFRetain(imageBuffer);
        _frames.PushPixelBuffer(imageBuffer);
        if (width != _width || height != _height)
        {
            _width = width;
            _height = height;
            Event?.Invoke(new VideoSessionEvent.Loaded(width, height, TimeSpan.Zero));
        }
        Event?.Invoke(new VideoSessionEvent.FrameAvailable(pts));
    }

    private void OnFrameError(int status)
    {
        if (++_errors == MaxConsecutiveErrors)
            Fail($"VideoToolbox failed to decode {MaxConsecutiveErrors} frames in a row (OSStatus {status}).");
        else
            _logger.LogDebug("VideoToolbox skipped a frame (OSStatus {Status})", status);
    }

    private void Fail(string message)
    {
        _logger.LogWarning("{Message}", message);
        Event?.Invoke(new VideoSessionEvent.Error(message, null));
    }

    private void DestroySession()
    {
        if (_session == IntPtr.Zero) return;
        VTDecompressionSessionInvalidate(_session);
        CFRelease(_session);
        _session = IntPtr.Zero;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            DestroySession();
            if (_format != IntPtr.Zero) { CFRelease(_format); _format = IntPtr.Zero; }
        }
        _frames.Dispose();
        if (_self.IsAllocated) _self.Free();
    }

    /// <summary>
    /// <c>{ PixelFormatType: 420v, MetalCompatibility: true, IOSurfaceProperties: {} }</c> — what the
    /// zero-copy path of <see cref="AVFoundationFrameSource"/> maps into Metal textures.
    /// </summary>
    private static IntPtr CreateDestinationAttributes()
    {
        IntPtr cf = NativeLibrary.Load(CoreFoundationLibrary);
        IntPtr keyCallbacks = NativeLibrary.GetExport(cf, "kCFTypeDictionaryKeyCallBacks");
        IntPtr valueCallbacks = NativeLibrary.GetExport(cf, "kCFTypeDictionaryValueCallBacks");
        IntPtr attributes = CFDictionaryCreateMutable(IntPtr.Zero, 0, keyCallbacks, valueCallbacks);

        IntPtr formatKey = GetPixelFormatTypeKey();
        if (formatKey != IntPtr.Zero)
        {
            int format = unchecked((int)PixelFormatNv12VideoRange);
            IntPtr number = CFNumberCreate(IntPtr.Zero, KCFNumberSInt32Type, &format);
            CFDictionarySetValue(attributes, formatKey, number);
            CFRelease(number);
        }
        IntPtr metalKey = GetMetalCompatibilityKey();
        IntPtr yes = ReadExportedConstant(CoreFoundationLibrary, "kCFBooleanTrue");
        if (metalKey != IntPtr.Zero && yes != IntPtr.Zero) CFDictionarySetValue(attributes, metalKey, yes);

        IntPtr surfaceKey = ReadExportedConstant(CoreVideoLibrary, "kCVPixelBufferIOSurfacePropertiesKey");
        if (surfaceKey != IntPtr.Zero)
        {
            IntPtr empty = CFDictionaryCreateMutable(IntPtr.Zero, 0, keyCallbacks, valueCallbacks);
            CFDictionarySetValue(attributes, surfaceKey, empty);
            CFRelease(empty);
        }
        return attributes;
    }

    // ---- interop ------------------------------------------------------------

    private const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CoreVideoLibrary = "/System/Library/Frameworks/CoreVideo.framework/CoreVideo";
    private const string CoreMediaLibrary = "/System/Library/Frameworks/CoreMedia.framework/CoreMedia";
    private const string VideoToolboxLibrary = "/System/Library/Frameworks/VideoToolbox.framework/VideoToolbox";
    private const int KCFNumberSInt32Type = 3;
    private const int KVTInvalidSessionErr = -12903;

    [StructLayout(LayoutKind.Sequential)]
    private struct VTDecompressionOutputCallbackRecord
    {
        public IntPtr Callback;
        public IntPtr RefCon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CMSampleTimingInfo
    {
        public CMTime Duration;
        public CMTime PresentationTimeStamp;
        public CMTime DecodeTimeStamp;
    }

    [DllImport(CoreFoundationLibrary)]
    private static extern IntPtr CFDictionaryCreateMutable(IntPtr allocator, nint capacity, IntPtr keyCallBacks, IntPtr valueCallBacks);

    [DllImport(CoreFoundationLibrary)]
    private static extern void CFDictionarySetValue(IntPtr dictionary, IntPtr key, IntPtr value);

    [DllImport(CoreFoundationLibrary)]
    private static extern IntPtr CFNumberCreate(IntPtr allocator, int type, void* value);

    [DllImport(CoreMediaLibrary)]
    private static extern int CMVideoFormatDescriptionCreateFromH264ParameterSets(
        IntPtr allocator, nuint parameterSetCount, IntPtr* parameterSetPointers, nuint* parameterSetSizes,
        int nalUnitHeaderLength, out IntPtr formatDescription);

    [DllImport(CoreMediaLibrary)]
    private static extern int CMVideoFormatDescriptionCreateFromHEVCParameterSets(
        IntPtr allocator, nuint parameterSetCount, IntPtr* parameterSetPointers, nuint* parameterSetSizes,
        int nalUnitHeaderLength, IntPtr extensions, out IntPtr formatDescription);

    [DllImport(CoreMediaLibrary)]
    private static extern int CMBlockBufferCreateWithMemoryBlock(
        IntPtr structureAllocator, IntPtr memoryBlock, nuint blockLength, IntPtr blockAllocator,
        IntPtr customBlockSource, nuint offsetToData, nuint dataLength, uint flags, out IntPtr blockBuffer);

    [DllImport(CoreMediaLibrary)]
    private static extern int CMBlockBufferReplaceDataBytes(IntPtr sourceBytes, IntPtr destinationBuffer, nuint offsetIntoDestination, nuint dataLength);

    [DllImport(CoreMediaLibrary)]
    private static extern int CMSampleBufferCreateReady(
        IntPtr allocator, IntPtr dataBuffer, IntPtr formatDescription, nint numSamples,
        nint numSampleTimingEntries, CMSampleTimingInfo* sampleTimingArray,
        nint numSampleSizeEntries, nuint* sampleSizeArray, out IntPtr sampleBuffer);

    [DllImport(VideoToolboxLibrary)]
    private static extern int VTDecompressionSessionCreate(
        IntPtr allocator, IntPtr videoFormatDescription, IntPtr videoDecoderSpecification,
        IntPtr destinationImageBufferAttributes, VTDecompressionOutputCallbackRecord* outputCallback,
        out IntPtr decompressionSession);

    [DllImport(VideoToolboxLibrary)]
    private static extern int VTDecompressionSessionDecodeFrame(IntPtr session, IntPtr sampleBuffer, uint decodeFlags, IntPtr sourceFrameRefCon, out uint infoFlags);

    [DllImport(VideoToolboxLibrary)]
    private static extern byte VTDecompressionSessionCanAcceptFormatDescription(IntPtr session, IntPtr formatDescription);

    [DllImport(VideoToolboxLibrary)]
    private static extern void VTDecompressionSessionInvalidate(IntPtr session);
}
