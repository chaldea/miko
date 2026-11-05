using System.Diagnostics;
using Miko.Components.Webcam;
using Miko.Graphics;
using Miko.Windowing.Native;
using Miko.Windowing.Video;

namespace MikoApp.Webcam.Desktop;

/// <summary>
/// <c>--verify &lt;dir&gt;</c>: runs the real app context (same DI, provider and video backend as the
/// window) offscreen, waits for the live picture and saves portrait, fullscreen-landscape and
/// pan-pressed frames as PNGs. The window's own frame dump fires before a stream can connect.
/// </summary>
internal static class WebcamVerification
{
    public static int Run(string directory)
    {
        Directory.CreateDirectory(directory);
        MikoApp.Webcam.WebcamDemo? page = null;
        var context = MikoApp.Webcam.App.CreateContext(builder =>
        {
            builder.UseSystemVideo();
            builder.UseDesktopNative();
        }, page: p => page = p);
        var controller = context.Controller;
        var engine = context.Engine;
        using var surface = engine.Graphics.CreateSurface(420, 860);
        controller.Initialize(surface.Context, 420, 860);

        MikoWebcam? webcam = null;
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < TimeSpan.FromSeconds(20))
        {
            Frame(controller, surface, 420, 860);
            webcam ??= page?.Webcam;
            if (webcam is { State: WebcamState.Playing or WebcamState.Error }) break;
            Thread.Sleep(16);
        }
        if (webcam == null) { Console.WriteLine("no MikoWebcam on the page"); return 2; }
        Console.WriteLine($"state={webcam.State} after {clock.ElapsedMilliseconds}ms; transport={webcam.Stream?.Transport}; info={webcam.Info?.Name} features={webcam.Info?.Features}; error={webcam.ErrorMessage}");
        // Let the bitrate settle.
        for (int i = 0; i < 90; i++) { Frame(controller, surface, 420, 860); Thread.Sleep(16); }
        Console.WriteLine($"rate={webcam.BytesPerSecond}");
        File.WriteAllBytes(Path.Combine(directory, "portrait.png"), surface.EncodePng());

        if (Environment.GetEnvironmentVariable("EZVIZ_VERIFY_PTZ") == "1") VerifyPtz(controller, surface, webcam, directory);

        webcam.SetQualityAsync(WebcamQuality.High).GetAwaiter().GetResult();
        clock.Restart();
        while (clock.Elapsed < TimeSpan.FromSeconds(15) && webcam.State != WebcamState.Playing) { Frame(controller, surface, 420, 860); Thread.Sleep(16); }
        for (int i = 0; i < 40; i++) { Frame(controller, surface, 420, 860); Thread.Sleep(16); }
        Console.WriteLine($"high: state={webcam.State} after {clock.ElapsedMilliseconds}ms");
        File.WriteAllBytes(Path.Combine(directory, "portrait-hd.png"), surface.EncodePng());

        webcam.ToggleFullscreen();
        using var landscape = engine.Graphics.CreateSurface(860, 420);
        for (int i = 0; i < 30; i++) { Frame(controller, landscape, 860, 420); Thread.Sleep(16); }
        File.WriteAllBytes(Path.Combine(directory, "fullscreen.png"), landscape.EncodePng());
        webcam.ToggleFullscreen();
        for (int i = 0; i < 5; i++) Frame(controller, surface, 420, 860);
        webcam.Dispose();
        return webcam.State == WebcamState.Error ? 1 : 0;
    }

    /// <summary>
    /// Holds the disk's right arrow for a second through real pointer events, then the left one to
    /// turn back, with device snapshots before, between and after (the picture itself may be on the
    /// cloud only, so the snapshots are the evidence that the camera turned).
    /// </summary>
    private static void VerifyPtz(Miko.Platform.MikoInteractionController controller, RenderSurface surface, MikoWebcam webcam, string directory)
    {
        var pad = FindByClass(controller.Engine, "miko-webcam-ptz-pad");
        if (pad == null) { Console.WriteLine("ptz: no pad"); return; }
        // The disk is centred horizontally; its centre was found by hit testing (PadCentre).
        float cx = PadCentre.X, cy = PadCentre.Y, arm = pad.OffsetWidth * .34f;
        Snapshot(webcam, Path.Combine(directory, "ptz-0.jpg"));
        foreach (var (name, x) in new[] { ("right", cx + arm), ("left", cx - arm) })
        {
            controller.OnPointerDown(x, cy, Miko.Events.MouseButton.Left);
            for (int i = 0; i < 3; i++) Frame(controller, surface, 420, 860);
            Console.WriteLine($"ptz {name}: active={webcam.ActivePtz}");
            File.WriteAllBytes(Path.Combine(directory, $"ptz-pressed-{name}.png"), surface.EncodePng());
            var hold = Stopwatch.StartNew();
            while (hold.Elapsed < TimeSpan.FromSeconds(1)) { Frame(controller, surface, 420, 860); Thread.Sleep(16); }
            controller.OnPointerUp(x, cy, Miko.Events.MouseButton.Left);
            for (int i = 0; i < 60; i++) { Frame(controller, surface, 420, 860); Thread.Sleep(16); }
            Console.WriteLine($"ptz {name}: released active={webcam.ActivePtz} hint={webcam.Hint}");
            Snapshot(webcam, Path.Combine(directory, $"ptz-after-{name}.jpg"));
        }
    }

    private static void Snapshot(MikoWebcam webcam, string path)
    {
        var snapshot = webcam.CaptureAsync().GetAwaiter().GetResult();
        if (snapshot != null) File.WriteAllBytes(path, snapshot.Data);
        Console.WriteLine($"snapshot {Path.GetFileName(path)}: {snapshot?.Data.Length ?? 0} bytes");
    }

    // Where the disk sits on the 420×860 page (the screenshot shows it there).
    private static readonly (float X, float Y) PadCentre = (210, 623);

    private static Miko.Core.Element? FindByClass(Miko.Core.MikoEngine engine, string className)
    {
        var hit = engine.HitTest(PadCentre.X, PadCentre.Y);
        for (var e = hit; e != null; e = e.Parent)
            if (e.HasClass(className)) return e;
        return null;
    }

    private static void Frame(Miko.Platform.MikoInteractionController controller, RenderSurface surface, int width, int height) =>
        controller.RenderFrame(surface.Context, width, height, 1 / 60f, c => controller.Engine.Render(c));
}
