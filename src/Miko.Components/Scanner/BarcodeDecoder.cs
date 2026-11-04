using Miko.Common;
using Miko.Graphics;
using ZXing;
using ZXing.Common;

namespace Miko.Components.Scanner;

/// <summary>Barcode symbologies the scanner can read.</summary>
[Flags]
public enum BarcodeFormats
{
    None = 0,
    QrCode = 1 << 0,
    DataMatrix = 1 << 1,
    Aztec = 1 << 2,
    Pdf417 = 1 << 3,
    Ean13 = 1 << 4,
    Ean8 = 1 << 5,
    UpcA = 1 << 6,
    UpcE = 1 << 7,
    Code128 = 1 << 8,
    Code39 = 1 << 9,
    Code93 = 1 << 10,
    Codabar = 1 << 11,
    Itf = 1 << 12,

    /// <summary>The 2-D symbologies.</summary>
    TwoDimensional = QrCode | DataMatrix | Aztec | Pdf417,

    /// <summary>Retail and logistics barcodes.</summary>
    OneDimensional = Ean13 | Ean8 | UpcA | UpcE | Code128 | Code39 | Code93 | Codabar | Itf,

    All = TwoDimensional | OneDimensional,
}

/// <summary>A decoded barcode.</summary>
/// <param name="Text">The decoded text.</param>
/// <param name="Format">Its symbology (a single flag).</param>
/// <param name="RawBytes">The raw payload when the symbology has one (QR byte mode, …); null otherwise.</param>
/// <param name="Corners">
/// Where the code was found. From <see cref="BarcodeAnalyzer"/>: normalized like its
/// <see cref="BarcodeAnalyzer.ScanRegion"/> (0–1 on each axis, origin top left, to the
/// <see cref="BarcodeAnalyzer.Viewport"/> when set, else to the upright frame); from
/// <see cref="MikoBarcodeScanner"/>: normalized to the scanner's view. From
/// <see cref="BarcodeDecoder.Decode(byte[], int, int, RectF?)"/>: pixels of the plane that was decoded.
/// Empty from <see cref="BarcodeDecoder.Decode(Image)"/>.
/// </param>
public sealed record BarcodeResult(string Text, BarcodeFormats Format, byte[]? RawBytes, IReadOnlyList<BarcodePoint> Corners);

/// <summary>A point of a <see cref="BarcodeResult"/>; see <see cref="BarcodeResult.Corners"/> for its units.</summary>
public readonly record struct BarcodePoint(float X, float Y);

/// <summary>
/// Decodes barcodes from luminance frames and images with ZXing (a managed port, Apache-2.0). One
/// instance per thread: the readers keep state between calls.
/// </summary>
/// <remarks>
/// 2-D codes read in any orientation. 1-D codes are read along pixel rows, so a barcode whose bars
/// run across the rows is invisible to them; when the 1-D formats find nothing, the plane is tried
/// again turned a quarter, so a barcode reads upright or sideways. (ZXing only does that itself
/// with <c>tryHarder</c>, which also scans every row and is far slower.)
/// </remarks>
public sealed class BarcodeDecoder
{
    private readonly MultiFormatReader _reader = new();
    // The 1-D formats only, for the quarter-turned pass; null without 1-D formats.
    private readonly MultiFormatReader? _sidewaysReader;
    private byte[] _turned = [];

    public BarcodeDecoder(BarcodeFormats formats = BarcodeFormats.QrCode, bool tryHarder = false)
    {
        Formats = formats;
        _reader.Hints = CreateHints(formats, tryHarder);
        // tryHarder makes ZXing turn the image itself.
        var oneDimensional = formats & BarcodeFormats.OneDimensional;
        if (oneDimensional != 0 && !tryHarder)
            _sidewaysReader = new MultiFormatReader { Hints = CreateHints(oneDimensional, tryHarder: false) };
    }

    public BarcodeFormats Formats { get; }

    /// <summary>
    /// Decodes a luminance plane (<paramref name="width"/> bytes per row), searching only
    /// <paramref name="region"/> (normalized to the plane; null for the whole plane).
    /// </summary>
    /// <returns>The code, or null when none was found. <see cref="BarcodeResult.Corners"/> are in pixels of the plane.</returns>
    public BarcodeResult? Decode(byte[] luminance, int width, int height, RectF? region = null)
    {
        ArgumentNullException.ThrowIfNull(luminance);
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (luminance.Length < width * height) throw new ArgumentException("The luminance plane is smaller than the frame.", nameof(luminance));
        int left = 0, top = 0, cropWidth = width, cropHeight = height;
        if (region is { } r)
        {
            left = Math.Clamp((int)(r.X * width), 0, width - 1);
            top = Math.Clamp((int)(r.Y * height), 0, height - 1);
            cropWidth = Math.Clamp((int)Math.Ceiling(r.Width * width), 1, width - left);
            cropHeight = Math.Clamp((int)Math.Ceiling(r.Height * height), 1, height - top);
        }
        var result = Decode(_reader, new PlanarYUVLuminanceSource(luminance, width, height, left, top, cropWidth, cropHeight, false));
        if (result != null) return ToResult(result, (x, y) => new BarcodePoint(x + left, y + top));
        if (_sidewaysReader == null) return null;

        // The crop turned a quarter clockwise: cropHeight wide, cropWidth high.
        int size = cropWidth * cropHeight;
        if (_turned.Length < size) _turned = new byte[size];
        TurnClockwise(luminance, width, left, top, cropWidth, cropHeight, _turned);
        result = Decode(_sidewaysReader, new PlanarYUVLuminanceSource(_turned, cropHeight, cropWidth, 0, 0, cropHeight, cropWidth, false));
        // A point (x, y) of the turned crop is (y, cropHeight − x) of the crop.
        return result == null ? null : ToResult(result, (x, y) => new BarcodePoint(left + y, top + cropHeight - x));
    }

    /// <summary>Decodes an image (a photo picked from the gallery, a screenshot). Vector images are rasterized at their size.</summary>
    public BarcodeResult? Decode(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        int width = image.Width, height = image.Height;
        if (width <= 0 || height <= 0) return null;
        var pixels = new byte[width * height * 4];
        if (!image.ReadPixels(pixels, ImagePixelFormat.Rgba8888)) return null;
        var luminance = new byte[width * height];
        for (int i = 0, p = 0; i < luminance.Length; i++, p += 4)
        {
            // Transparent pixels count as white (a code on a transparent PNG).
            int a = pixels[p + 3];
            int y = (pixels[p] * 77 + pixels[p + 1] * 150 + pixels[p + 2] * 29) >> 8;
            luminance[i] = (byte)((y * a + 255 * (255 - a)) / 255);
        }
        var result = Decode(luminance, width, height);
        return result == null ? null : result with { Corners = [] };
    }

    /// <summary>
    /// Copies the <paramref name="cropWidth"/> × <paramref name="cropHeight"/> crop at
    /// (<paramref name="left"/>, <paramref name="top"/>) of a plane into <paramref name="target"/>,
    /// turned a quarter clockwise (<paramref name="cropHeight"/> bytes per row).
    /// </summary>
    private static void TurnClockwise(byte[] source, int stride, int left, int top, int cropWidth, int cropHeight, byte[] target)
    {
        // Target row y is source column left + y, read bottom to top.
        for (int y = 0; y < cropWidth; y++)
        {
            int s = (top + cropHeight - 1) * stride + left + y, t = y * cropHeight;
            for (int x = 0; x < cropHeight; x++, s -= stride)
                target[t + x] = source[s];
        }
    }

    private static BarcodeResult ToResult(Result result, Func<float, float, BarcodePoint> map)
    {
        var corners = result.ResultPoints is { } points
            ? points.Where(p => p != null).Select(p => map(p.X, p.Y)).ToArray()
            : [];
        return new BarcodeResult(result.Text, FromZxing(result.BarcodeFormat), result.RawBytes, corners);
    }

    private static Result? Decode(MultiFormatReader reader, LuminanceSource source)
    {
        try
        {
            return reader.decodeWithState(new BinaryBitmap(new HybridBinarizer(source)));
        }
        catch (ReaderException)
        {
            return null;
        }
        finally
        {
            reader.reset();
        }
    }

    private static Dictionary<DecodeHintType, object> CreateHints(BarcodeFormats formats, bool tryHarder)
    {
        var hints = new Dictionary<DecodeHintType, object>
        {
            [DecodeHintType.POSSIBLE_FORMATS] = ToZxing(formats),
            [DecodeHintType.CHARACTER_SET] = "UTF-8",
        };
        if (tryHarder) hints[DecodeHintType.TRY_HARDER] = true;
        // Light-on-dark codes (dark-mode screens) as well.
        if ((formats & BarcodeFormats.TwoDimensional) != 0) hints[DecodeHintType.ALSO_INVERTED] = true;
        return hints;
    }

    private static List<BarcodeFormat> ToZxing(BarcodeFormats formats)
    {
        var list = new List<BarcodeFormat>();
        foreach (var (flag, format) in Map)
            if ((formats & flag) != 0) list.Add(format);
        if (list.Count == 0) throw new ArgumentException("At least one barcode format is needed.", nameof(formats));
        return list;
    }

    private static BarcodeFormats FromZxing(BarcodeFormat format)
    {
        foreach (var (flag, zxing) in Map)
            if (zxing == format) return flag;
        return BarcodeFormats.None;
    }

    private static readonly (BarcodeFormats Flag, BarcodeFormat Format)[] Map =
    [
        (BarcodeFormats.QrCode, BarcodeFormat.QR_CODE),
        (BarcodeFormats.DataMatrix, BarcodeFormat.DATA_MATRIX),
        (BarcodeFormats.Aztec, BarcodeFormat.AZTEC),
        (BarcodeFormats.Pdf417, BarcodeFormat.PDF_417),
        (BarcodeFormats.Ean13, BarcodeFormat.EAN_13),
        (BarcodeFormats.Ean8, BarcodeFormat.EAN_8),
        (BarcodeFormats.UpcA, BarcodeFormat.UPC_A),
        (BarcodeFormats.UpcE, BarcodeFormat.UPC_E),
        (BarcodeFormats.Code128, BarcodeFormat.CODE_128),
        (BarcodeFormats.Code39, BarcodeFormat.CODE_39),
        (BarcodeFormats.Code93, BarcodeFormat.CODE_93),
        (BarcodeFormats.Codabar, BarcodeFormat.CODABAR),
        (BarcodeFormats.Itf, BarcodeFormat.ITF),
    ];
}
