using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miko.DevTools;
using Miko.Hosting;

namespace MikoApp.Media;

public static class App
{
    /// <summary>
    /// 配置与平台无关的部分（HTTP、图片加载、样式、路由、日志）。
    /// 视频后端（FFmpeg）由平台宿主在调用 <see cref="MikoAppBuilder.Build"/> 前注册，
    /// 因为它位于 Miko.Windowing，仅桌面宿主引用。
    /// </summary>
    public static MikoAppBuilder CreateBuilder()
    {
        var builder = MikoAppBuilder.CreateDefault();

        builder.UseTitle("Miko Media Demo");
        builder.UseSize(1100, 760);

        // 指向本地 API；产品列表与缩略图/视频都从这里通过 http 加载。
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5050")
        });

        // 注册图片加载，并指定本程序集用于解析 res://（占位图为嵌入资源）。
        builder.AddResourceAssembly(typeof(App).Assembly);

        builder.AddDevTools();
        builder.AddStyleSheet(GlobalStyles.Create());

        builder.UseGeneratedRoutes();
        builder.UseDefaultLayout<MainLayout>();

        builder.EnableHotReload();
        builder.UseLogging(logging =>
        {
            logging.AddConsole().SetMinimumLevel(LogLevel.Information);
        });

        return builder;
    }

    public static void InitializeHotReload(MikoAppContext context)
    {
        MikoHotReloadHandler.Initialize(context.GetHotReloadService());
    }
}
