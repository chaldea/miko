using System.Diagnostics;
using Miko.Components.Player;
using Miko.Components.Player.Danmaku;
using Miko.Core;
using Miko.Core.DomElements;
using Miko.Hosting;
using Miko.Platform.Video;
using Miko.Styling;
using Miko.Windowing.Video;
using SkiaSharp;

namespace MikoApp.Player;

internal static class PlayerVerification
{
    public static int Run(string? source)
    {
        var builder = MikoAppBuilder.CreateDefault();
        builder.Services.AddMikoPlayer();
        builder.UseSystemVideo();
        var context = builder.Build();
        var engine = context.Engine;
        using var player = new MikoPlayer
        {
            Engine = engine, Source = source ?? "Assets/miko-local.mp4", AutoPlay = true, Muted = true,
            Danmaku = [new(TimeSpan.Zero, "MikoPlayer native video"), new(TimeSpan.FromSeconds(.1), "Danmaku", DanmakuMode.Top)]
        };
        var root = new DivElement { Style = new Style { Display = Miko.Common.Display.Block, Width = Miko.Common.Length.Percent(100), Height = Miko.Common.Length.Percent(100), BackgroundColor = Miko.Common.Color.Black } };
        root.Children.Add(player.Build());
        ((VideoElement)root.FindByTagName("video").Single()).PlaybackEvent += value =>
        {
            if (value is VideoSessionEvent.Error error) Console.WriteLine(error.Cause?.ToString() ?? error.Message);
        };
        using var bitmap = new SKBitmap(960, 680);
        using var canvas = new SKCanvas(bitmap);
        engine.Initialize(root, new List<StyleSheet>(), canvas, 960, 680);
        var timeout = Stopwatch.StartNew();
        bool hasFrame = false;
        while (timeout.Elapsed < TimeSpan.FromSeconds(25))
        {
            engine.Render(canvas);
            var video = (VideoElement)root.FindByTagName("video").Single();
            if (video.Session?.State == VideoSessionState.Error) break;
            if (video.Session?.FrameSource.AcquireCurrentFrame(null) != null)
            {
                video.Session.FrameSource.ReleaseCurrentFrame();
                hasFrame = true;
                if (player.Controller.Position > TimeSpan.FromSeconds(1)) break;
            }
            Thread.Sleep(16);
        }
        Console.WriteLine($"state={player.Controller.State}, position={player.Controller.Position}, duration={player.Controller.Duration}, frame={hasFrame}, error={player.Controller.Error}");
        string output = Path.Combine(Path.GetTempPath(), "miko-player-verification");
        Directory.CreateDirectory(output);
        Save(bitmap, Path.Combine(output, "desktop.png"));
        player.ToggleSettings();
        engine.SetViewportSize(360, 740);
        using var phone = new SKBitmap(360, 740);
        using var phoneCanvas = new SKCanvas(phone);
        for (int i = 0; i < 4; i++) engine.Render(phoneCanvas);
        Save(phone, Path.Combine(output, "mobile-settings.png"));
        player.ToggleSettings();
        player.ToggleFullscreen();
        for (int i = 0; i < 4; i++) engine.Render(phoneCanvas);
        Save(phone, Path.Combine(output, "mobile-fullscreen.png"));
        player.Pause();
        for (int i = 0; i < 30; i++) { engine.Render(phoneCanvas); Thread.Sleep(16); }
        Console.WriteLine($"paused={player.Controller.State}, pending={engine.HasPendingVisualWork}, screenshots={output}");
        engine.DisposeVideoSessions();
        return hasFrame && player.Controller.Error == null ? 0 : 1;
    }

    private static void Save(SKBitmap bitmap, string path)
    {
        using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }
}
