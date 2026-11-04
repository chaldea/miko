using Miko.Common;
using Miko.Components.Scanner;
using Miko.Core.DomElements;
using Miko.Graphics;
using Miko.Hosting;
using Miko.Platform.Camera;
using Miko.Styling;
using Miko.Testing.Camera;
using Shouldly;
using ZXing;
using ZXing.Common;
using ZXing.OneD;
using ZXing.QrCode;

namespace Miko.Components.Tests;

public class BarcodeScannerTests
{
    // Upright (as the preview shows it) portrait picture with a QR code in its top-left quadrant.
    private const int UprightWidth = 480, UprightHeight = 640;

    [Theory]
    [InlineData(0, false)]
    [InlineData(90, false)]
    [InlineData(180, false)]
    [InlineData(270, false)]
    [InlineData(0, true)]
    [InlineData(90, true)]
    [InlineData(180, true)]
    [InlineData(270, true)]
    public void Should_search_the_upright_scan_region_whatever_the_camera_orientation(int rotation, bool mirrored)
    {
        var upright = Plane(UprightWidth, UprightHeight);
        DrawMatrix(upright, UprightWidth, QrMatrix("MIKO", 200), 20, 60);
        var (raw, rawWidth, rawHeight) = ToRawFrame(upright, UprightWidth, UprightHeight, rotation, mirrored);

        var topLeft = new BarcodeAnalyzer { ScanRegion = new RectF(0, 0, .5f, .5f) };
        topLeft.Analyze(new CameraAnalysisFrame(raw, rawWidth, rawHeight, rotation, mirrored, TimeSpan.Zero));
        var bottomRight = new BarcodeAnalyzer { ScanRegion = new RectF(.5f, .5f, .5f, .5f) };
        bottomRight.Analyze(new CameraAnalysisFrame(raw, rawWidth, rawHeight, rotation, mirrored, TimeSpan.Zero));

        topLeft.Found.ShouldNotBeNull().Text.ShouldBe("MIKO");
        topLeft.Found.Corners.ShouldNotBeEmpty();
        topLeft.Found.Corners.ShouldAllBe(p => p.X > 0 && p.X < .5f && p.Y > 0 && p.Y < .5f);
        bottomRight.Found.ShouldBeNull();
    }

    // A phone held upright: the back camera delivers landscape frames (1280 × 720) that the preview
    // turns a quarter (rotation 90, or 270 on some devices). A barcode lying across the screen then
    // has its bars along the raw rows, which ZXing's 1-D readers scan: it used to never read in
    // portrait, wherever the scan region was, while the same code read once the phone was turned.
    [Theory]
    [InlineData(90, false)]
    [InlineData(270, false)]
    [InlineData(0, false)]
    [InlineData(180, false)]
    [InlineData(90, true)]
    [InlineData(270, true)]
    public void Should_read_a_barcode_lying_across_a_portrait_preview(int rotation, bool mirrored)
    {
        const int width = 720, height = 1280;
        var upright = Plane(width, height);
        var barcode = new EAN13Writer().encode("6901234567892", BarcodeFormat.EAN_13, 420, 160);
        int left = (width - barcode.Width) / 2, top = (height - barcode.Height) / 2;
        DrawMatrix(upright, width, barcode, left, top);
        var (raw, rawWidth, rawHeight) = ToRawFrame(upright, width, height, rotation, mirrored);

        var analyzer = new BarcodeAnalyzer(BarcodeFormats.All);
        var frame = MikoBarcodeScanner.GetScanFrame(width, height);
        analyzer.ScanRegion = new RectF(frame.X / width, frame.Y / height, frame.Width / width, frame.Height / height);
        analyzer.Analyze(new CameraAnalysisFrame(raw, rawWidth, rawHeight, rotation, mirrored, TimeSpan.Zero));

        var code = analyzer.Found.ShouldNotBeNull();
        code.Text.ShouldBe("6901234567892");
        code.Format.ShouldBe(BarcodeFormats.Ean13);
        // The corners are on the barcode as the preview shows it (1-D readers report points on the scan row).
        code.Corners.ShouldAllBe(p => p.X > (float)left / width - .01f && p.X < (float)(left + barcode.Width) / width + .01f
            && p.Y > (float)top / height - .01f && p.Y < (float)(top + barcode.Height) / height + .01f);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    public void Should_read_a_barcode_standing_along_the_frame(int rotation)
    {
        // Bars horizontal on the screen (the phone turned against the code): read in the quarter-turned pass.
        const int width = 720, height = 1280;
        var upright = Plane(width, height);
        var barcode = Turn(new Code128Writer().encode("MIKO-128", BarcodeFormat.CODE_128, 360, 120));
        int left = (width - barcode.Width) / 2, top = (height - barcode.Height) / 2;
        DrawMatrix(upright, width, barcode, left, top);
        var (raw, rawWidth, rawHeight) = ToRawFrame(upright, width, height, rotation, false);

        var analyzer = new BarcodeAnalyzer(BarcodeFormats.All);
        analyzer.Analyze(new CameraAnalysisFrame(raw, rawWidth, rawHeight, rotation, false, TimeSpan.Zero));

        var code = analyzer.Found.ShouldNotBeNull();
        code.Text.ShouldBe("MIKO-128");
        code.Corners.ShouldAllBe(p => p.X > (float)left / width - .01f && p.X < (float)(left + barcode.Width) / width + .01f
            && p.Y > (float)top / height - .01f && p.Y < (float)(top + barcode.Height) / height + .01f);
    }

    [Fact]
    public void Should_read_a_sideways_barcode_with_the_decoder()
    {
        const int width = 200, height = 440;
        var plane = Plane(width, height);
        DrawMatrix(plane, width, Turn(new Code128Writer().encode("SIDEWAYS", BarcodeFormat.CODE_128, 400, 120)), 40, 20);

        new BarcodeDecoder(BarcodeFormats.QrCode).Decode(plane, width, height).ShouldBeNull();
        var code = new BarcodeDecoder(BarcodeFormats.Code128).Decode(plane, width, height).ShouldNotBeNull();

        code.Text.ShouldBe("SIDEWAYS");
        code.Corners.ShouldAllBe(p => p.X >= 40 && p.X <= 160 && p.Y >= 20 && p.Y <= 420);
    }

    [Fact]
    public void Should_map_the_scan_region_through_the_preview_fit()
    {
        // A 300 × 890 view covered by a 720 × 1280 frame (scale 890 / 1280): the frame's 144 leftmost and
        // rightmost columns are cut off. A code there is not on screen, so a scan region spanning the
        // whole view must not find it.
        const int width = 720, height = 1280, viewWidth = 300, viewHeight = 890;
        var upright = Plane(width, height);
        DrawMatrix(upright, width, QrMatrix("HIDDEN", 140), 0, 600);
        var viewport = new CameraViewport(viewWidth, viewHeight, CameraFit.Cover);

        var whole = new BarcodeAnalyzer();
        whole.SetScanRegion(new RectF(0, 0, 1, 1), viewport);
        whole.Analyze(new CameraAnalysisFrame(upright, width, height, 0, false, TimeSpan.Zero));
        whole.Found.ShouldBeNull();

        // Without the viewport the region is the frame itself.
        var frameRegion = new BarcodeAnalyzer { ScanRegion = new RectF(0, 0, 1, 1) };
        frameRegion.Analyze(new CameraAnalysisFrame(upright, width, height, 0, false, TimeSpan.Zero));
        frameRegion.Found.ShouldNotBeNull().Text.ShouldBe("HIDDEN");

        // A code in the region drawn on the view is found; its corners come back in view coordinates.
        var visible = Plane(width, height);
        DrawMatrix(visible, width, QrMatrix("SHOWN", 200), 260, 540);
        var region = new RectF(.2f, .35f, .6f, .3f);
        var analyzer = new BarcodeAnalyzer();
        analyzer.SetScanRegion(region, viewport);
        analyzer.Analyze(new CameraAnalysisFrame(visible, width, height, 0, false, TimeSpan.Zero));
        var code = analyzer.Found.ShouldNotBeNull();
        code.Text.ShouldBe("SHOWN");
        // Frame x 260–460 → view (x × scale + offset) / viewWidth; y is not cropped (540–740 of 1280).
        float scale = (float)viewHeight / height, offset = (viewWidth - width * scale) / 2;
        code.Corners.ShouldAllBe(p => p.X > (260 * scale + offset) / viewWidth && p.X < (460 * scale + offset) / viewWidth
            && p.Y > 540f / height && p.Y < 740f / height);
    }

    [Fact]
    public void Should_raise_detected_once_and_scan_again_after_reset()
    {
        var analyzer = new BarcodeAnalyzer();
        var detected = new List<string>();
        analyzer.Detected += r => detected.Add(r.Text);
        var first = Frame("FIRST");

        analyzer.Analyze(new CameraAnalysisFrame(first, UprightWidth, UprightHeight, 0, false, TimeSpan.Zero));
        analyzer.Analyze(new CameraAnalysisFrame(first, UprightWidth, UprightHeight, 0, false, TimeSpan.Zero));
        detected.ShouldBe(["FIRST"]);

        analyzer.Reset();
        analyzer.Found.ShouldBeNull();
        analyzer.Analyze(new CameraAnalysisFrame(Frame("SECOND"), UprightWidth, UprightHeight, 0, false, TimeSpan.Zero));
        detected.ShouldBe(["FIRST", "SECOND"]);
    }

    [Fact]
    public void Should_read_only_the_requested_formats()
    {
        const int width = 400, height = 120;
        var plane = Plane(width, height);
        DrawMatrix(plane, width, new Code128Writer().encode("MIKO-128", BarcodeFormat.CODE_128, 360, 80), 20, 20);

        new BarcodeDecoder(BarcodeFormats.QrCode).Decode(plane, width, height).ShouldBeNull();
        var code = new BarcodeDecoder(BarcodeFormats.QrCode | BarcodeFormats.Code128).Decode(plane, width, height);

        code.ShouldNotBeNull().Text.ShouldBe("MIKO-128");
        code.Format.ShouldBe(BarcodeFormats.Code128);
    }

    [Fact]
    public void Should_decode_a_code_in_an_image()
    {
        var matrix = QrMatrix("FROM IMAGE", 160);
        var pixels = new byte[160 * 160 * 4];
        for (int y = 0; y < 160; y++)
        for (int x = 0; x < 160; x++)
        {
            byte v = matrix[x, y] ? (byte)0 : (byte)255;
            int i = (y * 160 + x) * 4;
            (pixels[i], pixels[i + 1], pixels[i + 2], pixels[i + 3]) = (v, v, v, 255);
        }
        using var image = new MikoEngineBuilder().Build().Graphics.CreateImage(160, 160, ImagePixelFormat.Rgba8888, pixels)!;

        new BarcodeDecoder().Decode(image).ShouldNotBeNull().Text.ShouldBe("FROM IMAGE");
    }

    [Fact]
    public void Should_report_each_code_once_on_the_ui_thread()
    {
        var backend = new FakeCameraBackend();
        var engine = new MikoEngineBuilder().Build();
        engine.CameraBackend = backend;
        using var surface = engine.Graphics.CreateSurface(UprightWidth, UprightHeight);
        var scanned = new List<string>();
        var scanner = new MikoBarcodeScanner
        {
            Engine = engine,
            CodeScanned = EventCallback.Factory.Create<BarcodeResult>(new object(), r => scanned.Add(r.Text)),
        };
        var root = new DivElement { Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100) } };
        root.Children.Add(scanner.Build());
        engine.Initialize(root, [], surface.Context, UprightWidth, UprightHeight);
        var session = backend.Sessions.Single();
        session.Start(UprightWidth, UprightHeight);
        engine.Render(surface.Context);

        PostAndWait(engine, session, Frame("FIRST"), () => scanner.Code != null);
        engine.Render(surface.Context);
        scanned.ShouldBe(["FIRST"]);

        // Found: further frames are not decoded, nothing more is reported.
        PostAndWait(engine, session, Frame("IGNORED"), () => true);
        engine.Render(surface.Context);
        engine.Render(surface.Context);
        scanned.ShouldBe(["FIRST"]);
        scanner.Code!.Text.ShouldBe("FIRST");

        scanner.Reset();
        PostAndWait(engine, session, Frame("SECOND"), () => scanner.Code != null);
        engine.Render(surface.Context);
        scanned.ShouldBe(["FIRST", "SECOND"]);

        scanner.Dispose();
        engine.DisposeMediaSessions();
    }

    /// <summary>Posts a frame through the session's analysis pump and waits until it was analyzed and <paramref name="done"/> holds.</summary>
    private static void PostAndWait(Miko.Core.MikoEngine engine, FakeCameraSession session, byte[] frame, Func<bool> done)
    {
        SpinWait.SpinUntil(() => session.Pump.IsReady, TimeSpan.FromSeconds(5)).ShouldBeTrue();
        session.Pump.TryPost(frame, UprightWidth, UprightHeight, UprightWidth, 0, false, TimeSpan.Zero).ShouldBeTrue();
        SpinWait.SpinUntil(() => session.Pump.IsReady && done(), TimeSpan.FromSeconds(5)).ShouldBeTrue();
    }

    [Fact]
    public void Should_search_only_the_painted_scan_frame()
    {
        var (engine, session, surface, scanner) = ShowScanner();

        // Outside the frame (and its margin): ignored. The view and the frame have the same size here.
        var frame = MikoBarcodeScanner.GetScanFrame(UprightWidth, UprightHeight);
        var outside = Plane(UprightWidth, UprightHeight);
        DrawMatrix(outside, UprightWidth, QrMatrix("OUTSIDE", 120), 4, 4);
        PostAndWait(engine, session, outside, () => true);
        scanner.Code.ShouldBeNull();

        PostAndWait(engine, session, Frame("INSIDE"), () => scanner.Code != null);
        scanner.Code!.Text.ShouldBe("INSIDE");
        scanner.Code.Corners.ShouldAllBe(p => p.X * UprightWidth > frame.X && p.X * UprightWidth < frame.Right
            && p.Y * UprightHeight > frame.Y && p.Y * UprightHeight < frame.Bottom);

        scanner.Dispose();
        engine.DisposeMediaSessions();
        surface.Dispose();
    }

    [Fact]
    public void Should_sweep_the_scan_line_down_the_frame_while_scanning()
    {
        var clock = new ManualClock();
        var (engine, session, surface, scanner) = ShowScanner(clock);
        var frame = MikoBarcodeScanner.GetScanFrame(UprightWidth, UprightHeight);

        clock.Advance(TimeSpan.FromSeconds(.5));
        engine.Render(surface.Context);
        int first = ScanLineRow(surface, frame).ShouldNotBeNull();
        // The overlay asks for the next frame itself (on-demand hosts draw only when work is pending).
        engine.HasPendingRenderWork.ShouldBeTrue();

        clock.Advance(TimeSpan.FromSeconds(.6));
        engine.Render(surface.Context);
        int second = ScanLineRow(surface, frame).ShouldNotBeNull();
        second.ShouldBeGreaterThan(first + 40, $"first {first}, second {second}");

        // Found: the line stops and the scanner stops asking for frames.
        PostAndWait(engine, session, Frame("FOUND"), () => scanner.Code != null);
        engine.Render(surface.Context);
        engine.Render(surface.Context);
        ScanLineRow(surface, frame).ShouldBeNull();
        engine.HasPendingRenderWork.ShouldBeFalse();

        scanner.Dispose();
        engine.DisposeMediaSessions();
        surface.Dispose();
    }

    /// <summary>The brightest row on the frame's vertical center line, when one is lit by the scan line.</summary>
    private static int? ScanLineRow(RenderSurface surface, RectF frame)
    {
        int x = (int)(frame.X + frame.Width / 2);
        int? best = null;
        int bestValue = 200;
        for (int y = (int)frame.Y + 3; y < (int)frame.Bottom - 3; y++)
        {
            int value = surface.GetPixel(x, y).R;
            if (value > bestValue) (best, bestValue) = (y, value);
        }
        return best;
    }

    /// <summary>A scanner filling a <see cref="UprightWidth"/> × <see cref="UprightHeight"/> engine, its camera running and painted once.</summary>
    private static (Miko.Core.MikoEngine Engine, FakeCameraSession Session, RenderSurface Surface, MikoBarcodeScanner Scanner) ShowScanner(TimeProvider? clock = null)
    {
        var backend = new FakeCameraBackend();
        var engine = new MikoEngineBuilder().Build();
        engine.CameraBackend = backend;
        var surface = engine.Graphics.CreateSurface(UprightWidth, UprightHeight);
        var scanner = new MikoBarcodeScanner { Engine = engine, Clock = clock };
        var root = new DivElement { Style = new Style { Display = Display.Block, Width = Length.Percent(100), Height = Length.Percent(100) } };
        root.Children.Add(scanner.Build());
        engine.Initialize(root, [], surface.Context, UprightWidth, UprightHeight);
        var session = backend.Sessions.Single();
        // Opaque mid grey, so the white overlay stands out in every channel.
        var grey = new byte[UprightWidth * UprightHeight * 4];
        for (int i = 0; i < grey.Length; i += 4) (grey[i], grey[i + 1], grey[i + 2], grey[i + 3]) = (100, 100, 100, 255);
        session.Start(UprightWidth, UprightHeight, grey);
        engine.Render(surface.Context);
        engine.Render(surface.Context);
        return (engine, session, surface, scanner);
    }

    private sealed class ManualClock : TimeProvider
    {
        private long _ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _ticks;

        public void Advance(TimeSpan by) => _ticks += by.Ticks;
    }

    /// <summary>An upright frame with the code <paramref name="text"/> in the middle of the scanner's scan frame.</summary>
    private static byte[] Frame(string text)
    {
        var plane = Plane(UprightWidth, UprightHeight);
        var frame = MikoBarcodeScanner.GetScanFrame(UprightWidth, UprightHeight);
        int size = 180;
        int left = (int)(frame.X + (frame.Width - size) / 2);
        int top = (int)(frame.Y + (frame.Height - size) / 2);
        DrawMatrix(plane, UprightWidth, QrMatrix(text, size), left, top);
        return plane;
    }

    private static BitMatrix QrMatrix(string text, int size) => new QRCodeWriter().encode(text, BarcodeFormat.QR_CODE, size, size);

    /// <summary><paramref name="matrix"/> turned a quarter clockwise.</summary>
    private static BitMatrix Turn(BitMatrix matrix)
    {
        var turned = new BitMatrix(matrix.Height, matrix.Width);
        for (int y = 0; y < matrix.Height; y++)
        for (int x = 0; x < matrix.Width; x++)
            if (matrix[x, y]) turned[matrix.Height - 1 - y, x] = true;
        return turned;
    }

    private static byte[] Plane(int width, int height)
    {
        var plane = new byte[width * height];
        Array.Fill(plane, (byte)255);
        return plane;
    }

    private static void DrawMatrix(byte[] plane, int width, BitMatrix matrix, int left, int top)
    {
        for (int y = 0; y < matrix.Height; y++)
        for (int x = 0; x < matrix.Width; x++)
            plane[(top + y) * width + left + x] = matrix[x, y] ? (byte)0 : (byte)255;
    }

    /// <summary>
    /// The raw frame a camera would deliver for <paramref name="upright"/>: the preview rotates a raw
    /// frame clockwise by <paramref name="rotation"/>, then mirrors it (RenderEngine.RenderCameraFrame),
    /// so each raw pixel samples the upright pixel that transform puts it on. Rotation matrix with
    /// y pointing down, written independently of the analyzer's lookup tables.
    /// </summary>
    private static (byte[] Plane, int Width, int Height) ToRawFrame(byte[] upright, int uprightWidth, int uprightHeight, int rotation, bool mirrored)
    {
        bool sideways = rotation % 180 != 0;
        int rawWidth = sideways ? uprightHeight : uprightWidth, rawHeight = sideways ? uprightWidth : uprightHeight;
        double radians = rotation * Math.PI / 180;
        int cos = (int)Math.Round(Math.Cos(radians)), sin = (int)Math.Round(Math.Sin(radians));
        var raw = new byte[rawWidth * rawHeight];
        for (int ry = 0; ry < rawHeight; ry++)
        for (int rx = 0; rx < rawWidth; rx++)
        {
            double px = rx + .5 - rawWidth / 2.0, py = ry + .5 - rawHeight / 2.0;
            double ux = px * cos - py * sin, uy = px * sin + py * cos;
            if (mirrored) ux = -ux;
            int x = (int)Math.Floor(ux + uprightWidth / 2.0), y = (int)Math.Floor(uy + uprightHeight / 2.0);
            raw[ry * rawWidth + rx] = upright[y * uprightWidth + x];
        }
        return (raw, rawWidth, rawHeight);
    }
}
