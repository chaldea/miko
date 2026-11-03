using Miko.Common;
using Miko.Platform.Camera;

namespace Miko.Components.Scanner;

/// <summary>
/// Feeds camera frames to a <see cref="BarcodeDecoder"/> until a code is found, then stops decoding
/// until <see cref="Reset"/>. An <see cref="ICameraFrameAnalyzer"/> for <see cref="Camera.MikoCameraView"/>;
/// <see cref="MikoBarcodeScanner"/> uses one.
/// </summary>
/// <remarks>
/// Reads one code per frame, the first the decoder finds. Frames arrive one at a time (the camera
/// drops frames while this analyzer is busy), so a slow decode lowers the scan rate, never the
/// preview's frame rate.
/// </remarks>
public sealed class BarcodeAnalyzer : ICameraFrameAnalyzer
{
    private readonly object _lock = new();
    private readonly object _decodeLock = new();
    private readonly BarcodeDecoder _decoder;
    private byte[] _buffer = [];
    private BarcodeResult? _found;
    private RectF? _scanRegion;

    public BarcodeAnalyzer(BarcodeFormats formats = BarcodeFormats.QrCode, bool tryHarder = false)
    {
        Formats = formats;
        TryHarder = tryHarder;
        _decoder = new BarcodeDecoder(formats, tryHarder);
    }

    public BarcodeFormats Formats { get; }

    public bool TryHarder { get; }

    /// <summary>
    /// The part of the picture to search, normalized to the upright preview (0–1 on each axis, origin
    /// top left); null searches the whole frame. Mapped to the camera's raw frame orientation here.
    /// </summary>
    public RectF? ScanRegion
    {
        get { lock (_lock) return _scanRegion; }
        set { lock (_lock) _scanRegion = value; }
    }

    /// <summary>The code found; null while scanning. Cleared by <see cref="Reset"/>.</summary>
    public BarcodeResult? Found
    {
        get { lock (_lock) return _found; }
    }

    /// <summary>Raised once when a code is found, on the analysis thread (a thread-pool thread).</summary>
    public event Action<BarcodeResult>? Detected;

    /// <summary>Clears <see cref="Found"/> and scans for the next code.</summary>
    public void Reset()
    {
        lock (_lock) _found = null;
    }

    public void Analyze(in CameraAnalysisFrame frame)
    {
        RectF? region;
        lock (_lock)
        {
            if (_found != null) return;
            region = _scanRegion;
        }

        int rotation = CameraOrientation.Normalize(frame.Rotation);
        bool mirrored = frame.IsMirrored;
        int width = frame.Width, height = frame.Height;
        BarcodeResult? raw;
        // ZXing reads a byte[]; the frame's span is only valid during this call.
        lock (_decodeLock)
        {
            int size = width * height;
            if (_buffer.Length < size) _buffer = new byte[size];
            frame.Luminance[..size].CopyTo(_buffer);
            var rawRegion = region is { } r ? ToRawRegion(r, rotation, mirrored) : (RectF?)null;
            raw = _decoder.Decode(_buffer, width, height, rawRegion);
        }
        if (raw == null) return;

        var corners = raw.Corners
            .Select(p => ToUpright(p.X / width, p.Y / height, rotation, mirrored))
            .ToArray();
        var result = raw with { Corners = corners };
        lock (_lock)
        {
            if (_found != null) return;
            _found = result;
        }
        Detected?.Invoke(result);
    }

    // The preview draws a raw frame rotated clockwise by `rotation`, then mirrored horizontally
    // (RenderEngine.RenderCameraFrame). These map normalized points between the two.

    /// <summary>A normalized raw-frame point → the upright preview.</summary>
    private static BarcodePoint ToUpright(float rx, float ry, int rotation, bool mirrored)
    {
        var (x, y) = rotation switch
        {
            90 => (1 - ry, rx),
            180 => (1 - rx, 1 - ry),
            270 => (ry, 1 - rx),
            _ => (rx, ry),
        };
        return mirrored ? new BarcodePoint(1 - x, y) : new BarcodePoint(x, y);
    }

    /// <summary>A normalized upright point → the raw frame (inverse of <see cref="ToUpright"/>).</summary>
    private static (float X, float Y) ToRaw(float ux, float uy, int rotation, bool mirrored)
    {
        if (mirrored) ux = 1 - ux;
        return rotation switch
        {
            90 => (uy, 1 - ux),
            180 => (1 - ux, 1 - uy),
            270 => (1 - uy, ux),
            _ => (ux, uy),
        };
    }

    /// <summary>An upright region → the raw-frame region that covers it.</summary>
    private static RectF ToRawRegion(RectF region, int rotation, bool mirrored)
    {
        var (x0, y0) = ToRaw(region.X, region.Y, rotation, mirrored);
        var (x1, y1) = ToRaw(region.Right, region.Bottom, rotation, mirrored);
        float left = Math.Min(x0, x1), top = Math.Min(y0, y1);
        return new RectF(left, top, Math.Abs(x1 - x0), Math.Abs(y1 - y0));
    }
}
