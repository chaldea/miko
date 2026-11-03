namespace Miko.Platform.Camera;

/// <summary>
/// Receives camera frames for analysis (barcode scanning, …). Called on a thread-pool thread, one
/// frame at a time: frames that arrive while <see cref="Analyze"/> runs are dropped, so a slow
/// analyzer lowers its own frame rate and never the preview's.
/// </summary>
public interface ICameraFrameAnalyzer
{
    /// <summary>Analyzes one frame. The frame's buffer is reused afterwards: copy what must outlive the call.</summary>
    void Analyze(in CameraAnalysisFrame frame);
}

/// <summary>
/// One luminance (grey) frame for analysis, in the camera's raw orientation, rows packed
/// (<see cref="Width"/> bytes per row). <see cref="Rotation"/> and <see cref="IsMirrored"/> say how
/// the preview shows it.
/// </summary>
public readonly ref struct CameraAnalysisFrame
{
    public CameraAnalysisFrame(ReadOnlySpan<byte> luminance, int width, int height, int rotation, bool isMirrored, TimeSpan timestamp)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (luminance.Length < width * height) throw new ArgumentException("The luminance plane is smaller than the frame.", nameof(luminance));
        Luminance = luminance;
        Width = width;
        Height = height;
        Rotation = rotation;
        IsMirrored = isMirrored;
        Timestamp = timestamp;
    }

    /// <summary><see cref="Width"/> × <see cref="Height"/> bytes, one per pixel, 0 = black.</summary>
    public ReadOnlySpan<byte> Luminance { get; }

    public int Width { get; }

    public int Height { get; }

    /// <summary>Clockwise rotation that turns the frame upright on the display (see <see cref="ICameraSession.Rotation"/>).</summary>
    public int Rotation { get; }

    public bool IsMirrored { get; }

    /// <summary>Capture time on the backend's clock.</summary>
    public TimeSpan Timestamp { get; }
}

/// <summary>
/// Hands camera frames from a backend to an <see cref="ICameraFrameAnalyzer"/>, keeping only the
/// latest: <see cref="TryPost(ReadOnlySpan{byte}, int, int, int, int, bool, TimeSpan)"/> copies the
/// luminance plane and runs the analyzer on the thread pool, unless it is still busy with the
/// previous frame, in which case the frame is dropped. Backends call it from their capture
/// callback, which then never waits for the analyzer.
/// </summary>
public sealed class CameraAnalysisPump
{
    private readonly object _lock = new();
    private ICameraFrameAnalyzer? _analyzer;
    private byte[] _buffer = [];
    private int _width, _height, _rotation;
    private bool _mirrored;
    private TimeSpan _timestamp;
    private int _busy;

    /// <summary>The installed analyzer; null when nobody listens.</summary>
    public ICameraFrameAnalyzer? Analyzer
    {
        get => Volatile.Read(ref _analyzer);
        set => Volatile.Write(ref _analyzer, value);
    }

    /// <summary>
    /// Whether a frame posted now would be analyzed: an analyzer is installed and idle. Backends test it
    /// before doing per-frame work (locking a buffer, converting colours).
    /// </summary>
    public bool IsReady => Analyzer != null && Volatile.Read(ref _busy) == 0;

    /// <summary>Raised with the analyzer's exception; the pump keeps going.</summary>
    public event Action<Exception>? AnalyzerFailed;

    /// <summary>Posts an 8-bit luminance plane (rows <paramref name="rowStride"/> bytes apart).</summary>
    /// <returns>Whether the frame was accepted (false when no analyzer is installed or it is busy).</returns>
    public bool TryPost(ReadOnlySpan<byte> luminance, int width, int height, int rowStride, int rotation, bool mirrored, TimeSpan timestamp)
    {
        if (!TryBegin(width, height)) return false;
        try
        {
            if (rowStride < width || luminance.Length < (long)rowStride * (height - 1) + width)
                throw new ArgumentException("The luminance plane is smaller than the frame.", nameof(luminance));
            var buffer = _buffer;
            if (rowStride == width)
            {
                luminance[..(width * height)].CopyTo(buffer);
            }
            else
            {
                for (int y = 0; y < height; y++)
                    luminance.Slice(y * rowStride, width).CopyTo(buffer.AsSpan(y * width, width));
            }
        }
        catch
        {
            Volatile.Write(ref _busy, 0);
            throw;
        }
        Dispatch(width, height, rotation, mirrored, timestamp);
        return true;
    }

    /// <summary>Posts an 8-bit luminance plane in unmanaged memory (a locked platform buffer).</summary>
    public bool TryPost(IntPtr luminance, int width, int height, int rowStride, int rotation, bool mirrored, TimeSpan timestamp)
    {
        if (luminance == IntPtr.Zero || rowStride < width) return false;
        if (!TryBegin(width, height)) return false;
        try
        {
            var buffer = _buffer;
            for (int y = 0; y < height; y++)
                System.Runtime.InteropServices.Marshal.Copy(luminance + y * rowStride, buffer, y * width, width);
        }
        catch
        {
            Volatile.Write(ref _busy, 0);
            throw;
        }
        Dispatch(width, height, rotation, mirrored, timestamp);
        return true;
    }

    /// <summary>
    /// Posts a 32-bit RGB frame (RGBA/RGBX or, with <paramref name="bgr"/>, BGRA/BGRX), converting it
    /// to luminance (BT.601). For backends whose decoder only produces RGB.
    /// </summary>
    public bool TryPostRgb(ReadOnlySpan<byte> pixels, int width, int height, int rowStride, bool bgr, int rotation, bool mirrored, TimeSpan timestamp)
    {
        if (!TryBegin(width, height)) return false;
        try
        {
            if (rowStride < width * 4 || pixels.Length < (long)rowStride * (height - 1) + width * 4)
                throw new ArgumentException("The pixel buffer is smaller than the frame.", nameof(pixels));
            int r = bgr ? 2 : 0, b = bgr ? 0 : 2;
            var buffer = _buffer;
            for (int y = 0; y < height; y++)
            {
                var row = pixels.Slice(y * rowStride, width * 4);
                int o = y * width;
                for (int x = 0, i = 0; x < width; x++, i += 4)
                    buffer[o + x] = (byte)((row[i + r] * 77 + row[i + 1] * 150 + row[i + b] * 29) >> 8);
            }
        }
        catch
        {
            Volatile.Write(ref _busy, 0);
            throw;
        }
        Dispatch(width, height, rotation, mirrored, timestamp);
        return true;
    }

    private bool TryBegin(int width, int height)
    {
        if (width <= 0 || height <= 0 || Analyzer == null) return false;
        if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return false;
        int size = width * height;
        if (_buffer.Length < size) _buffer = GC.AllocateUninitializedArray<byte>(size);
        return true;
    }

    private void Dispatch(int width, int height, int rotation, bool mirrored, TimeSpan timestamp)
    {
        lock (_lock)
        {
            _width = width;
            _height = height;
            _rotation = rotation;
            _mirrored = mirrored;
            _timestamp = timestamp;
        }
        ThreadPool.UnsafeQueueUserWorkItem(static pump => pump.Run(), this, preferLocal: false);
    }

    private void Run()
    {
        try
        {
            var analyzer = Analyzer;
            if (analyzer == null) return;
            int width, height, rotation;
            bool mirrored;
            TimeSpan timestamp;
            lock (_lock)
            {
                (width, height, rotation, mirrored, timestamp) = (_width, _height, _rotation, _mirrored, _timestamp);
            }
            analyzer.Analyze(new CameraAnalysisFrame(_buffer.AsSpan(0, width * height), width, height, rotation, mirrored, timestamp));
        }
        catch (Exception ex)
        {
            AnalyzerFailed?.Invoke(ex);
        }
        finally
        {
            Volatile.Write(ref _busy, 0);
        }
    }
}

/// <summary>Orientation arithmetic shared by the mobile backends.</summary>
public static class CameraOrientation
{
    /// <summary>
    /// The clockwise rotation that shows a raw frame upright, from the sensor orientation (clockwise
    /// angle that turns the frame upright on the device in its natural orientation, as Android's
    /// <c>SENSOR_ORIENTATION</c>) and the display rotation (counter-clockwise rotation of the screen
    /// content from the natural orientation, as Android's <c>Display.getRotation()</c> in degrees).
    /// Front cameras are then mirrored, so their rotation is counted the other way.
    /// </summary>
    public static int PreviewRotation(int sensorOrientation, int displayRotation, CameraFacing facing)
        => facing == CameraFacing.Front
            ? Normalize(sensorOrientation + displayRotation)
            : Normalize(sensorOrientation - displayRotation);

    /// <summary>Reduces an angle to 0, 90, 180 or 270 (rounding to the nearest quarter turn).</summary>
    public static int Normalize(int degrees)
    {
        int quarter = (int)Math.Round(degrees / 90.0, MidpointRounding.AwayFromZero);
        return ((quarter % 4) + 4) % 4 * 90;
    }
}
