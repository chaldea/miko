using Miko.Windowing;
using Miko.Windowing.Video;
using MikoApp.Media;

namespace MikoApp.Media.Desktop;

/// <summary>
/// 桌面宿主：注册 FFmpeg 视频后端并启动窗口。先运行 <c>MikoApp.Media.Api</c>（http://localhost:5050），
/// 客户端会通过网络加载产品缩略图与样例视频并渲染。
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = App.CreateBuilder();
        builder.UseFFmpegVideo();               // 注册 FFmpeg 视频后端（位于 Miko.Windowing）

        var context = builder.Build();
        App.InitializeHotReload(context);
        context.RunDesktop();
    }
}
