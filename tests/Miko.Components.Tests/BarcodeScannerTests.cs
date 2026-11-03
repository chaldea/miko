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
        var (raw, rawWidth, rawHeight) = ToRawFrame(upright, rotation, mirrored);

        var topLeft = new BarcodeAnalyzer { ScanRegion = new RectF(0, 0, .5f, .5f) };
        topLeft.Analyze(new CameraAnalysisFrame(raw, rawWidth, rawHeight, rotation, mirrored, TimeSpan.Zero));
        var bottomRight = new BarcodeAnalyzer { ScanRegion = new RectF(.5f, .5f, .5f, .5f) };
        bottomRight.Analyze(new CameraAnalysisFrame(raw, rawWidth, rawHeight, rotation, mirrored, TimeSpan.Zero));

        topLeft.Found.ShouldNotBeNull().Text.ShouldBe("MIKO");
        topLeft.Found.Corners.ShouldNotBeEmpty();
        topLeft.Found.Corners.ShouldAllBe(p => p.X > 0 && p.X < .5f && p.Y > 0 && p.Y < .5f);
        bottomRight.Found.ShouldBeNull();
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

    /// <summary>An upright frame with the code <paramref name="text"/> inside the scanner's scan region.</summary>
    private static byte[] Frame(string text)
    {
        var plane = Plane(UprightWidth, UprightHeight);
        var region = MikoBarcodeScanner.ScanRegion;
        int size = 180;
        int left = (int)(region.X * UprightWidth + (region.Width * UprightWidth - size) / 2);
        int top = (int)(region.Y * UprightHeight + (region.Height * UprightHeight - size) / 2);
        DrawMatrix(plane, UprightWidth, QrMatrix(text, size), left, top);
        return plane;
    }

    private static BitMatrix QrMatrix(string text, int size) => new QRCodeWriter().encode(text, BarcodeFormat.QR_CODE, size, size);

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
    private static (byte[] Plane, int Width, int Height) ToRawFrame(byte[] upright, int rotation, bool mirrored)
    {
        bool sideways = rotation % 180 != 0;
        int rawWidth = sideways ? UprightHeight : UprightWidth, rawHeight = sideways ? UprightWidth : UprightHeight;
        double radians = rotation * Math.PI / 180;
        int cos = (int)Math.Round(Math.Cos(radians)), sin = (int)Math.Round(Math.Sin(radians));
        var raw = new byte[rawWidth * rawHeight];
        for (int ry = 0; ry < rawHeight; ry++)
        for (int rx = 0; rx < rawWidth; rx++)
        {
            double px = rx + .5 - rawWidth / 2.0, py = ry + .5 - rawHeight / 2.0;
            double ux = px * cos - py * sin, uy = px * sin + py * cos;
            if (mirrored) ux = -ux;
            int x = (int)Math.Floor(ux + UprightWidth / 2.0), y = (int)Math.Floor(uy + UprightHeight / 2.0);
            raw[ry * rawWidth + rx] = upright[y * UprightWidth + x];
        }
        return (raw, rawWidth, rawHeight);
    }
}
