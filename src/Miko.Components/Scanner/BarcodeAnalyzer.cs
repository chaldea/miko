using Miko.Common;
using Miko.Platform.Camera;

namespace Miko.Components.Scanner;

/// <summary>
/// The view a camera preview is shown in, for <see cref="BarcodeAnalyzer.Viewport"/>: its size (any
/// unit, only the aspect ratio matters) and how the upright frame is fitted into it, as
/// <c>RenderEngine</c> draws a <c>&lt;camera&gt;</c> (centered, cover crops, contain letterboxes).
/// </summary>
public readonly record struct CameraViewport(float Width, float Height, CameraFit Fit = CameraFit.Cover);

/// <summary>
/// Feeds camera frames to a <see cref="BarcodeDecoder"/> until a code is found, then stops decoding
/// until <see cref="Reset"/>. An <see cref="ICameraFrameAnalyzer"/> for <see cref="Camera.MikoCameraView"/>;
/// <see cref="MikoBarcodeScanner"/> uses one.
/// </summary>
/// <remarks>
/// <para>
/// Reads one code per frame, the first the decoder finds. Frames arrive one at a time (the camera
/// drops frames while this analyzer is busy), so a slow decode lowers the scan rate, never the
/// preview's frame rate.
/// </para>
/// <para>
/// The scan region is cut out of the raw frame and turned upright before decoding, so the decoder
/// sees what the user sees. That matters for 1-D barcodes, which are read along pixel rows: a phone
/// camera delivers landscape frames, so in portrait a barcode that lies across the screen has its
/// bars along the raw rows and could never be read from the raw frame.
/// </para>
/// </remarks>
public sealed class BarcodeAnalyzer : ICameraFrameAnalyzer
{
    private static readonly RectF WholeFrame = new(0, 0, 1, 1);

    private readonly object _lock = new();
    private readonly object _decodeLock = new();
    private readonly BarcodeDecoder _decoder;
    private byte[] _buffer = [];
    private BarcodeResult? _found;
    private RectF? _scanRegion;
    private CameraViewport? _viewport;

    public BarcodeAnalyzer(BarcodeFormats formats = BarcodeFormats.QrCode, bool tryHarder = false)
    {
        Formats = formats;
        TryHarder = tryHarder;
        _decoder = new BarcodeDecoder(formats, tryHarder);
    }

    public BarcodeFormats Formats { get; }

    public bool TryHarder { get; }

    /// <summary>
    /// The part of the picture to search, normalized to the preview as the user sees it (0–1 on each
    /// axis, origin top left): to <see cref="Viewport"/> when it is set, otherwise to the upright frame.
    /// Null searches the whole frame. Mapped to the camera's raw frame orientation here.
    /// </summary>
    public RectF? ScanRegion
    {
        get { lock (_lock) return _scanRegion; }
        set { lock (_lock) _scanRegion = value; }
    }

    /// <summary>
    /// The view the preview is shown in. Set it when <see cref="ScanRegion"/> is drawn over a view
    /// whose aspect ratio differs from the frame's (a full-screen preview crops the frame): the region
    /// then covers exactly what is drawn. Null: the region is normalized to the upright frame.
    /// </summary>
    public CameraViewport? Viewport
    {
        get { lock (_lock) return _viewport; }
        set { lock (_lock) _viewport = value; }
    }

    /// <summary>Sets <see cref="ScanRegion"/> and <see cref="Viewport"/> together (no frame sees one without the other).</summary>
    public void SetScanRegion(RectF? region, CameraViewport? viewport)
    {
        lock (_lock)
        {
            _scanRegion = region;
            _viewport = viewport;
        }
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
        CameraViewport? viewport;
        lock (_lock)
        {
            if (_found != null) return;
            region = _scanRegion;
            viewport = _viewport;
        }

        int rotation = CameraOrientation.Normalize(frame.Rotation);
        bool mirrored = frame.IsMirrored;
        bool sideways = rotation % 180 != 0;
        int uprightWidth = sideways ? frame.Height : frame.Width;
        int uprightHeight = sideways ? frame.Width : frame.Height;
        var fit = PreviewFit.Create(uprightWidth, uprightHeight, viewport);

        // The region in the upright frame (normalized), as the preview shows it, then unmirrored:
        // the decoder reads the real picture, a mirrored code would not read.
        var area = fit.ToFrame(region ?? WholeFrame);
        if (area.Width <= 0 || area.Height <= 0) return;
        if (mirrored) area = new RectF(1 - area.Right, area.Y, area.Width, area.Height);
        int left = Math.Clamp((int)(area.X * uprightWidth), 0, uprightWidth - 1);
        int top = Math.Clamp((int)(area.Y * uprightHeight), 0, uprightHeight - 1);
        int cropWidth = Math.Clamp((int)Math.Ceiling(area.Right * uprightWidth) - left, 1, uprightWidth - left);
        int cropHeight = Math.Clamp((int)Math.Ceiling(area.Bottom * uprightHeight) - top, 1, uprightHeight - top);

        BarcodeResult? raw;
        // ZXing reads a byte[]; the frame's span is only valid during this call.
        lock (_decodeLock)
        {
            int size = cropWidth * cropHeight;
            if (_buffer.Length < size) _buffer = new byte[size];
            CopyUpright(frame.Luminance, frame.Width, frame.Height, rotation, left, top, cropWidth, cropHeight, _buffer);
            raw = _decoder.Decode(_buffer, cropWidth, cropHeight);
        }
        if (raw == null) return;

        var corners = raw.Corners
            .Select(p =>
            {
                float x = (p.X + left) / uprightWidth, y = (p.Y + top) / uprightHeight;
                return fit.ToView(mirrored ? 1 - x : x, y);
            })
            .ToArray();
        var result = raw with { Corners = corners };
        lock (_lock)
        {
            if (_found != null) return;
            _found = result;
        }
        Detected?.Invoke(result);
    }

    /// <summary>
    /// Copies the crop (<paramref name="left"/>, <paramref name="top"/>, <paramref name="cropWidth"/> ×
    /// <paramref name="cropHeight"/>) of the upright, unmirrored picture out of a raw frame
    /// (<paramref name="width"/> × <paramref name="height"/>) that the preview turns clockwise by
    /// <paramref name="rotation"/> (RenderEngine.RenderCameraFrame).
    /// </summary>
    private static void CopyUpright(ReadOnlySpan<byte> raw, int width, int height, int rotation,
        int left, int top, int cropWidth, int cropHeight, byte[] target)
    {
        // Upright pixel (ux, uy) is raw pixel  0°: (ux, uy)   90°: (uy, H−1−ux)
        //                                    180°: (W−1−ux, H−1−uy)   270°: (W−1−uy, ux)
        for (int y = 0; y < cropHeight; y++)
        {
            int uy = top + y, t = y * cropWidth;
            switch (rotation)
            {
                case 90:
                    for (int x = 0, s = (height - 1 - left) * width + uy; x < cropWidth; x++, s -= width)
                        target[t + x] = raw[s];
                    break;
                case 180:
                    for (int x = 0, s = (height - 1 - uy) * width + width - 1 - left; x < cropWidth; x++, s--)
                        target[t + x] = raw[s];
                    break;
                case 270:
                    for (int x = 0, s = left * width + width - 1 - uy; x < cropWidth; x++, s += width)
                        target[t + x] = raw[s];
                    break;
                default:
                    raw.Slice(uy * width + left, cropWidth).CopyTo(target.AsSpan(t, cropWidth));
                    break;
            }
        }
    }

    /// <summary>
    /// Maps normalized points between a view and the upright frame fitted into it (centered, scaled to
    /// cover or contain). Without a viewport both spaces are the same.
    /// </summary>
    private readonly struct PreviewFit
    {
        // View-normalized = frame-normalized × Scale + Offset, per axis.
        private readonly float _scaleX, _scaleY, _offsetX, _offsetY;

        private PreviewFit(float scaleX, float scaleY, float offsetX, float offsetY)
            => (_scaleX, _scaleY, _offsetX, _offsetY) = (scaleX, scaleY, offsetX, offsetY);

        public static PreviewFit Create(int frameWidth, int frameHeight, CameraViewport? viewport)
        {
            if (viewport is not { Width: > 0, Height: > 0 } v || !float.IsFinite(v.Width) || !float.IsFinite(v.Height))
                return new PreviewFit(1, 1, 0, 0);
            float scale = v.Fit == CameraFit.Contain
                ? Math.Min(v.Width / frameWidth, v.Height / frameHeight)
                : Math.Max(v.Width / frameWidth, v.Height / frameHeight);
            float scaleX = frameWidth * scale / v.Width, scaleY = frameHeight * scale / v.Height;
            return new PreviewFit(scaleX, scaleY, (1 - scaleX) / 2, (1 - scaleY) / 2);
        }

        /// <summary>A view region → the frame region it shows, clipped to the frame (empty when it shows none).</summary>
        public RectF ToFrame(RectF view)
        {
            float x0 = Math.Clamp((view.X - _offsetX) / _scaleX, 0, 1), x1 = Math.Clamp((view.Right - _offsetX) / _scaleX, 0, 1);
            float y0 = Math.Clamp((view.Y - _offsetY) / _scaleY, 0, 1), y1 = Math.Clamp((view.Bottom - _offsetY) / _scaleY, 0, 1);
            return new RectF(x0, y0, Math.Max(0, x1 - x0), Math.Max(0, y1 - y0));
        }

        public BarcodePoint ToView(float x, float y) => new(x * _scaleX + _offsetX, y * _scaleY + _offsetY);
    }
}
