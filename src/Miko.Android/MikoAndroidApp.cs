using Android.App;
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

    /// <summary>
    /// 把 Activity 的 <c>OnActivityResult</c> 转发给 Miko 的 Native 能力层。
    /// <para>
    /// 使用 <c>ICameraService</c>（拍照、录像、相册选择、图片编辑）的应用**必须**在宿主 Activity 里
    /// 调用本方法，否则这些能力发起系统 Intent 后永远等不到结果：
    /// </para>
    /// <code>
    /// protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    /// {
    ///     if (MikoAndroidApp.HandleActivityResult(_view, requestCode, resultCode, data)) return;
    ///     base.OnActivityResult(requestCode, resultCode, data);
    /// }
    /// </code>
    /// </summary>
    /// <returns>该结果是否由 Miko 发起并已被消费；为 <c>false</c> 时应交还给基类处理。</returns>
    public static bool HandleActivityResult(
        MikoSurfaceView view,
        int requestCode,
        Result resultCode,
        Intent? data)
    {
        ArgumentNullException.ThrowIfNull(view);

        return view.NativeHost.ActivityResults.Deliver(requestCode, resultCode, data);
    }
}
