using System.Diagnostics;

namespace Miko.MockServer;

public static class MediaGenerator
{
    /// <summary>Author original assets; FFmpeg is needed only for this explicit regeneration step.</summary>
    public static async Task GenerateAsync(string ffmpeg)
    {
        string directory = Path.Combine(Directory.GetCurrentDirectory(), "Media");
        Directory.CreateDirectory(directory);
        var desktop = new Raster(640, 360);
        desktop.Fill(0, 0, 640, 360, 0x101b2e);
        desktop.Fill(0, 0, 640, 66, 0x1d304e);
        desktop.Text(28, 24, "MIKO REMOTE DESKTOP", 0x67e8f9, 3);
        desktop.Text(28, 94, "YOUR OWN INTERACTIVE SESSION", 0xb7c8df);
        desktop.Text(28, 124, "CLICK BUTTONS OR TYPE ON YOUR KEYBOARD", 0xb7c8df);
        desktop.Fill(28, 174, 190, 62, 0x147d92);
        desktop.Text(52, 197, "CLICK ME", 0xffffff, 3);
        desktop.Fill(246, 174, 170, 62, 0x334a6d);
        desktop.Text(274, 197, "RESET", 0xffffff, 3);
        desktop.Text(28, 316, "RFB 3.8 / MIKO MOCK SERVER", 0x8198b6);
        desktop.Save(Path.Combine(directory, "desktop.ppm"));

        var panorama = new Raster(1280, 360);
        for (int y = 0; y < 360; y++) panorama.Fill(0, y, 1280, 1, y < 210 ? (uint)(0x193b60 + (y / 10) * 0x010101) : 0x234c43);
        for (int sector = 0; sector < 8; sector++)
        {
            int x = sector * 160;
            panorama.Text(x + 12, 24, $"ZONE {sector + 1}", 0xe0f2fe, 2);
            int height = 60 + sector % 3 * 30;
            panorama.Fill(x + 28, 220 - height, 100, height, (uint)(0x345a76 + sector * 0x030503));
            for (int row = 0; row < 3; row++) for (int col = 0; col < 4; col++)
                panorama.Fill(x + 38 + col * 22, 230 - height + row * 16, 10, 8, 0xfcd58d);
            panorama.Fill(x + 5, 225, 4, 40, 0x284637);
            panorama.Fill(x - 5, 218, 24, 24, 0x438263);
        }
        panorama.Fill(0, 285, 1280, 52, 0x192d3e);
        for (int x = 0; x < 1280; x += 64) panorama.Fill(x, 309, 28, 3, 0xcbd5e1);
        panorama.Text(500, 344, "MIKO PANORAMA - 5 FPS", 0x9ec8bd, 1);
        string temporary = Path.Combine(directory, "panorama.ppm");
        panorama.Save(temporary);
        try
        {
            await Run(ffmpeg, "-y", "-hide_banner", "-loglevel", "error", "-loop", "1", "-framerate", "5", "-i", temporary,
                "-vf", "drawgrid=width=160:height=90:thickness=1:color=white@0.08,drawtext=text='LIVE %{pts\\:hms}':x=20:y=60:fontsize=18:fontcolor=white",
                "-t", "12", "-an", "-c:v", "libx264", "-profile:v", "baseline", "-pix_fmt", "yuv420p", "-preset", "slow", "-crf", "30",
                "-g", "10", "-keyint_min", "10", "-sc_threshold", "0", "-bf", "0", "-x264-params", "aud=1:repeat-headers=1",
                "-movflags", "+faststart", Path.Combine(directory, "demo.mp4"));
            await Run(ffmpeg, "-y", "-hide_banner", "-loglevel", "error", "-i", Path.Combine(directory, "demo.mp4"), "-c:v", "copy", "-bsf:v", "h264_mp4toannexb", "-f", "h264", Path.Combine(directory, "panorama.h264"));
        }
        finally { File.Delete(temporary); }
        Console.WriteLine($"Generated original demo assets in {directory}");
    }

    private static async Task Run(string executable, params string[] arguments)
    {
        var info = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
        foreach (string argument in arguments) info.ArgumentList.Add(argument);
        using var process = Process.Start(info) ?? throw new InvalidOperationException("Could not start FFmpeg.");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException($"FFmpeg exited with {process.ExitCode}.");
    }
}
