using System.Diagnostics;
using Miko.Components.Vnc;
using Miko.Components.Vnc.Protocol;
using Miko.Core;
using Miko.Events;
using Miko.Graphics;
using Miko.Platform;
using Miko.Windowing.Native;

namespace MikoApp.Vnc.Desktop;

/// <summary>
/// <c>--verify &lt;dir&gt;</c>: runs the real app context offscreen against <c>VNC_HOST</c> / <c>VNC_PASSWORD</c>,
/// waits for the remote screen and saves frames (desktop window, toolbar panels, phone portrait). Then
/// connects once per encoding with a bare <see cref="VncClient"/> and compares each decoded screen with the
/// Raw one. Input sent to the server is limited to pointer moves (the remote machine is someone's desktop).
/// </summary>
internal static class VncVerification
{
    private const int Width = 1280, Height = 800;

    public static int Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var (host, port) = MikoApp.Vnc.App.ServerFromEnvironment();
        if (host == null) { Console.WriteLine("set VNC_HOST (and VNC_PASSWORD)"); return 2; }

        VncDemo? page = null;
        var context = MikoApp.Vnc.App.CreateContext(builder => builder.UseDesktopNative(), page: p => page = p);
        var controller = context.Controller;
        var engine = context.Engine;
        using var surface = engine.Graphics.CreateSurface(Width, Height);
        controller.Initialize(surface.Context, Width, Height);

        MikoVnc? vnc = null;
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromSeconds(20))
        {
            Frame(controller, surface, Width, Height);
            vnc ??= page?.Vnc;
            if (vnc is { State: VncState.Connected or VncState.Disconnected or VncState.Authenticating }) break;
            Thread.Sleep(16);
        }
        if (vnc == null) { Console.WriteLine("no MikoVnc on the page"); return 2; }
        Console.WriteLine($"state={vnc.State} after {clock.ElapsedMilliseconds}ms; desktop={vnc.Desktop}; error={vnc.ErrorMessage}");
        if (vnc.State != VncState.Connected)
        {
            File.WriteAllBytes(Path.Combine(directory, "failed.png"), surface.EncodePng());
            return 1;
        }

        // A locked Windows desktop powers its display off and the server captures black: a pointer move wakes it.
        controller.OnPointerMove(Width / 2f + 3, Height / 2f + 3);
        controller.OnPointerMove(Width / 2f, Height / 2f);
        // The first full frame (a 1080p desktop over the LAN).
        Settle(controller, surface, vnc, TimeSpan.FromSeconds(8));
        var client = vnc.Client!;
        Console.WriteLine($"encodings: {string.Join(", ", client.EncodingCounts.Select(kv => $"{VncEncoding.Name(kv.Key)}×{kv.Value}"))}; " +
            $"{client.BytesReceived / 1024} KB in {client.UpdateCount} updates; rate={vnc.BytesPerSecond}");
        Console.WriteLine($"framebuffer lit pixels: {LitShare(client.Framebuffer):0.0}%");
        File.WriteAllBytes(Path.Combine(directory, "desktop.png"), surface.EncodePng());

        // Move the remote pointer to the middle of the screen (the drawn pointer follows).
        var screen = Find(engine, "miko-vnc-screen")!;
        float cx = Width / 2f, cy = Height / 2f;
        controller.OnPointerMove(cx - 40, cy - 40);
        controller.OnPointerMove(cx, cy);
        Settle(controller, surface, vnc, TimeSpan.FromSeconds(2));
        Console.WriteLine($"pointer={vnc.Client?.Framebuffer.Width}x{vnc.Client?.Framebuffer.Height} cursorShape drawn; zoom={vnc.Zoom}");
        File.WriteAllBytes(Path.Combine(directory, "pointer.png"), surface.EncodePng());

        // Toolbar: the keys bar and the settings panel, through real clicks.
        Click(controller, surface, engine, "miko-vnc-keys-button");
        Click(controller, surface, engine, "miko-vnc-settings-button");
        Console.WriteLine($"keys bar={vnc.IsKeysBarVisible} settings={vnc.IsSettingsOpen}");
        File.WriteAllBytes(Path.Combine(directory, "panels.png"), surface.EncodePng());
        Click(controller, surface, engine, "miko-vnc-settings-button");
        Click(controller, surface, engine, "miko-vnc-keys-button");

        // Zoom in twice around the pointer, then back.
        Click(controller, surface, engine, "miko-vnc-zoom-in");
        Click(controller, surface, engine, "miko-vnc-zoom-in");
        Settle(controller, surface, vnc, TimeSpan.FromSeconds(1));
        Console.WriteLine($"zoom={vnc.Zoom}");
        File.WriteAllBytes(Path.Combine(directory, "zoomed.png"), surface.EncodePng());
        vnc.FitToScreen();

        // Phone portrait (the same session, a narrower surface).
        using var phone = engine.Graphics.CreateSurface(390, 844);
        for (int i = 0; i < 20; i++) { Frame(controller, phone, 390, 844); Thread.Sleep(16); }
        File.WriteAllBytes(Path.Combine(directory, "phone.png"), phone.EncodePng());
        for (int i = 0; i < 5; i++) Frame(controller, surface, Width, Height);

        Console.WriteLine($"session: {client.BytesReceived / 1024} KB, {client.UpdateCount} updates, " +
            $"{string.Join(", ", client.EncodingCounts.Select(kv => $"{VncEncoding.Name(kv.Key)}×{kv.Value}"))}");
        vnc.Disconnect();
        Frame(controller, surface, Width, Height);
        File.WriteAllBytes(Path.Combine(directory, "disconnected.png"), surface.EncodePng());

        int failures = CompareEncodings(host, port, Environment.GetEnvironmentVariable("VNC_PASSWORD"), engine.Graphics, directory);
        vnc.Dispose();
        return failures == 0 ? 0 : 1;
    }

    /// <summary>One connection per encoding; each decoded screen against the Raw one.</summary>
    private static int CompareEncodings(string host, int port, string? password, IGraphicsFactory graphics, string directory)
    {
        var cases = new (string Name, int[] Encodings, int? Jpeg)[]
        {
            ("raw", [VncEncoding.Raw], null),
            ("zrle", [VncEncoding.Zrle], null),
            ("tight", [VncEncoding.Tight], null),
            ("tight-jpeg", [VncEncoding.Tight], 6),
            ("hextile", [VncEncoding.Hextile], null),
            ("zlib", [VncEncoding.Zlib], null),
            ("rre", [VncEncoding.Rre], null),
        };
        byte[]? reference = null;
        int failures = 0;
        foreach (var (name, encodings, jpeg) in cases)
        {
            var options = new VncConnectOptions(host, port)
            {
                Password = password, Encodings = encodings, JpegQuality = jpeg, CompressLevel = 6, Shared = true,
                JpegDecoder = jpeg != null ? new JpegBridge(graphics) : null,
            };
            var sw = Stopwatch.StartNew();
            using var client = VncClient.ConnectAsync(options).GetAwaiter().GetResult();
            long lastUpdate = 0;
            Exception? failure = null;
            client.FramebufferUpdated += () => Interlocked.Exchange(ref lastUpdate, Environment.TickCount64);
            client.Disconnected += ex => failure = ex ?? new EndOfStreamException();
            client.Start();
            // Settled: pixels arrived and nothing more for half a second.
            while (sw.Elapsed < TimeSpan.FromSeconds(30) && failure == null
                && (client.EncodingCounts.Count == 0 || Environment.TickCount64 - Interlocked.Read(ref lastUpdate) < 500))
                Thread.Sleep(20);
            var fb = client.Framebuffer;
            var pixels = (byte[])fb.Pixels.Clone();
            string counts = string.Join(", ", client.EncodingCounts.Select(kv => $"{VncEncoding.Name(kv.Key)}×{kv.Value}"));
            if (failure != null) { Console.WriteLine($"{name}: FAILED {failure.Message}"); failures++; continue; }
            using (var image = graphics.CreateImage(fb.Width, fb.Height, ImagePixelFormat.Bgrx8888, pixels))
            using (var s = graphics.CreateSurface(fb.Width / 2, fb.Height / 2))
            {
                s.Context.DrawImage(image!, new Miko.Common.RectF(0, 0, fb.Width / 2, fb.Height / 2));
                File.WriteAllBytes(Path.Combine(directory, $"encoding-{name}.png"), s.EncodePng());
            }
            if (reference == null)
            {
                reference = pixels;
                Console.WriteLine($"{name}: {fb.Width}×{fb.Height} in {sw.ElapsedMilliseconds}ms, {client.BytesReceived / 1024} KB ({counts})");
                continue;
            }
            long differing = 0, totalError = 0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                int e = Math.Abs(pixels[i] - reference[i]) + Math.Abs(pixels[i + 1] - reference[i + 1]) + Math.Abs(pixels[i + 2] - reference[i + 2]);
                totalError += e;
                if (e > 6) differing++;
            }
            double share = differing * 100.0 / (pixels.Length / 4);
            double meanError = totalError / 3.0 / (pixels.Length / 4);
            bool lossy = jpeg != null;
            // A live desktop changes a little between connections (a clock): lossless must still be near-identical.
            // JPEG on a photo: a few levels of error on most pixels is the expected loss.
            bool ok = lossy ? meanError < 8 : share < 0.5;
            if (!ok) failures++;
            Console.WriteLine($"{name}: {sw.ElapsedMilliseconds}ms, {client.BytesReceived / 1024} KB ({counts}); " +
                $"vs raw: {share:0.000}% pixels differ, mean error {meanError:0.00} {(ok ? "OK" : "MISMATCH")}");
        }
        return failures;
    }

    private static double LitShare(VncFramebuffer fb)
    {
        long lit = 0;
        var p = fb.Pixels;
        for (int i = 0; i < p.Length; i += 4) if (p[i] + p[i + 1] + p[i + 2] > 30) lit++;
        return lit * 100.0 / (p.Length / 4);
    }

    private sealed class JpegBridge(IGraphicsFactory graphics) : IVncJpegDecoder
    {
        public bool Decode(ReadOnlySpan<byte> jpeg, int width, int height, Span<byte> destination, int stride)
        {
            using var image = graphics.DecodeImage(jpeg);
            return image != null && image.Width == width && image.Height == height && image.ReadPixels(destination, ImagePixelFormat.Bgra8888, stride);
        }
    }

    private static void Settle(MikoInteractionController controller, RenderSurface surface, MikoVnc vnc, TimeSpan max)
    {
        var sw = Stopwatch.StartNew();
        long count = -1, stableSince = 0;
        while (sw.Elapsed < max)
        {
            Frame(controller, surface, Width, Height);
            long now = vnc.Client?.UpdateCount ?? 0;
            if (now != count) { count = now; stableSince = sw.ElapsedMilliseconds; }
            else if (now > 1 && sw.ElapsedMilliseconds - stableSince > 600) break;
            Thread.Sleep(16);
        }
        for (int i = 0; i < 3; i++) Frame(controller, surface, Width, Height);
    }

    private static void Click(MikoInteractionController controller, RenderSurface surface, MikoEngine engine, string className)
    {
        var element = Find(engine, className);
        if (element == null) { Console.WriteLine($"no {className}"); return; }
        var box = Bounds(engine, element);
        float x = box.X + box.Width / 2, y = box.Y + box.Height / 2;
        controller.OnPointerDown(x, y, MouseButton.Left);
        controller.OnPointerUp(x, y, MouseButton.Left);
        for (int i = 0; i < 4; i++) Frame(controller, surface, Width, Height);
    }

    private static Element? Find(MikoEngine engine, string className)
    {
        static Element? Walk(Element e, string c)
        {
            if (e.HasClass(c)) return e;
            foreach (var child in e.Children) if (Walk(child, c) is { } f) return f;
            return null;
        }
        return engine.GetRoot() is { } root ? Walk(root, className) : null;
    }

    private static Miko.Common.RectF Bounds(MikoEngine engine, Element element)
    {
        static Miko.Layout.LayoutBox? Box(Miko.Layout.LayoutBox b, Element e)
            => b.Element == e ? b : b.Children.Select(c => Box(c, e)).FirstOrDefault(x => x != null);
        return Box(engine.GetCurrentLayout()!, element)!.BoxModel.BorderBox;
    }

    private static void Frame(MikoInteractionController controller, RenderSurface surface, int width, int height) =>
        controller.RenderFrame(surface.Context, width, height, 1 / 60f, c => controller.Engine.Render(c));
}
