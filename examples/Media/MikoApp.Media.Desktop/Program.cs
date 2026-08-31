using Miko.Windowing;
using Miko.Windowing.Video;
using MikoApp.Media;

namespace MikoApp.Media.Desktop;

/// <summary>
/// 桌面宿主：注册系统原生视频后端并启动窗口。
/// <para>
/// 本地视频（<c>Assets/miko-local.mp4</c>）无需任何服务即可播放；网络视频需先运行
/// <c>MikoApp.Media.Api</c>（http://localhost:5050）以提供缩略图与样例视频。
/// </para>
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = App.CreateBuilder();

        // 系统原生硬解：Windows→Media Foundation、Linux→GStreamer、macOS→AVFoundation。
        // 不携带第三方原生库；需要系统解码器不覆盖的格式时改用 Miko.Video.FFmpeg。
        builder.UseSystemVideo();

        var context = builder.Build();
        App.InitializeHotReload(context);
        context.RunDesktop();
    }
}
