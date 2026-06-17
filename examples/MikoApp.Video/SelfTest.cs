using Microsoft.Extensions.Logging;
using Miko.Platform.Video;
using Miko.Windowing.Video;

namespace MikoApp.Video;

/// <summary>
/// 无窗口端到端自检：编码测试片段 → 真实 FFmpeg 后端解码 → 校验帧源产出有效图像。
/// 验证 ISSUE-058 桌面后端的解码合成链路，不依赖 GPU 窗口。
/// </summary>
public static class SelfTest
{
    public static int Run()
    {
        Console.WriteLine("[selftest] encoding sample clip...");
        string clip = TestClip.EnsureSampleClip();
        Console.WriteLine($"[selftest] clip: {clip} ({new FileInfo(clip).Length} bytes)");

        using var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
        var backend = new FFmpegVideoBackend(loggerFactory.CreateLogger<FFmpegVideoBackend>());

        int width = 0, height = 0;
        bool loaded = false, ended = false, errored = false;
        string? errorMessage = null;

        var session = backend.CreateSession(
            new VideoSourceDescriptor(clip),
            new VideoSessionOptions(AutoPlay: true, Muted: true, Loop: false));

        session.Event += evt =>
        {
            switch (evt)
            {
                case VideoSessionEvent.Loaded l: loaded = true; width = l.Width; height = l.Height; break;
                case VideoSessionEvent.Ended: ended = true; break;
                case VideoSessionEvent.Error e: errored = true; errorMessage = e.Message; break;
            }
        };

        // 轮询帧源，统计成功取到的帧数。
        int framesSeen = 0;
        int lastW = 0, lastH = 0;
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline && !ended && !errored)
        {
            var img = session.FrameSource.AcquireCurrentFrame(null);
            if (img != null)
            {
                framesSeen++;
                lastW = img.Width;
                lastH = img.Height;
                session.FrameSource.ReleaseCurrentFrame();
            }
            Thread.Sleep(20);
        }

        session.Dispose();

        Console.WriteLine($"[selftest] loaded={loaded} size={width}x{height} framesSeen={framesSeen} lastFrame={lastW}x{lastH} ended={ended} errored={errored}");
        if (errored) Console.WriteLine($"[selftest] error: {errorMessage}");

        bool ok = loaded && width == 320 && height == 240 && framesSeen > 0 && lastW == 320 && lastH == 240 && !errored;
        Console.WriteLine(ok ? "[selftest] PASS" : "[selftest] FAIL");
        return ok ? 0 : 1;
    }
}
