using Android.Content;
using Miko.Hosting;

namespace Miko.Android;

/// <summary>
/// Android 启动便捷入口。让 Activity 仅需一行即可承载共享的 Miko 应用：
/// <code>SetContentView(MikoAndroidApp.CreateView(this, App.CreateContext));</code>
/// </summary>
public static class MikoAndroidApp
{
    /// <summary>使用给定的应用上下文工厂创建一个 <see cref="MikoSurfaceView"/>。</summary>
    public static MikoSurfaceView CreateView(Context context, Func<MikoAppContext> contextFactory)
    {
        return new MikoSurfaceView(context, contextFactory());
    }

    /// <summary>使用已构建的应用上下文创建一个 <see cref="MikoSurfaceView"/>。</summary>
    public static MikoSurfaceView CreateView(Context context, MikoAppContext appContext)
    {
        return new MikoSurfaceView(context, appContext);
    }
}
