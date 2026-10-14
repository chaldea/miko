using Android.Views;
using Miko.Native;
using Miko.Platform;

namespace Miko.Android.Native;

/// <summary>
/// Android 的组件触觉反馈（ISSUE-149），基于 <c>View.PerformHapticFeedback</c>。
///
/// <para>
/// <b>为什么不用 <c>Vibrator</c>：</b>面向应用的 <c>AndroidHapticsService</c> 直接驱动振动器，
/// 需要应用声明 <c>VIBRATE</c> 权限，也不受系统"触摸反馈"开关约束。组件的滚轮"咔"一下属于
/// 界面反馈，走 <c>PerformHapticFeedback</c> 才对：不需要任何权限，用户在系统设置里关掉触摸
/// 反馈时自然静音，强度和波形也由系统按机型调校，与原生控件一致。
/// </para>
///
/// <para>
/// 常量的选取与 Flutter 的 <c>HapticFeedback</c> 在 Android 上的映射相同：选择变化用
/// <c>CLOCK_TICK</c>（系统 <c>TimePicker</c> 转动时用的就是它），冲击三档依次为
/// <c>VIRTUAL_KEY</c> / <c>KEYBOARD_TAP</c> / <c>CONTEXT_CLICK</c>。Android 没有 iOS 那样的
/// "预热"，选择开始与结束都不产生触感（与系统控件一致）。
/// </para>
///
/// <para>
/// 触感由 Activity 的 DecorView 发出（宿主 <see cref="MikoSurfaceView"/> 构造时 <c>Attach</c>，
/// 调用时才读取），并切到 UI 线程。宿主缺失时静默跳过。
/// </para>
/// </summary>
internal sealed class AndroidHapticFeedback : IHapticFeedback
{
    private readonly INativeHostContext _hostContext;

    public AndroidHapticFeedback(INativeHostContext hostContext) => _hostContext = hostContext;

    public void SelectionStart() { }

    public void SelectionChanged() => Perform(FeedbackConstants.ClockTick);

    public void SelectionEnd() { }

    public void Impact(HapticImpactStyle style = HapticImpactStyle.Medium) => Perform(style switch
    {
        HapticImpactStyle.Light => FeedbackConstants.VirtualKey,
        HapticImpactStyle.Heavy when OperatingSystem.IsAndroidVersionAtLeast(23) => FeedbackConstants.ContextClick,
        _ => FeedbackConstants.KeyboardTap,
    });

    private void Perform(FeedbackConstants feedback)
    {
        var activity = (_hostContext.Host as AndroidNativeHost)?.Activity;
        if (activity is null) return;

        if (global::Android.OS.Looper.MyLooper() == global::Android.OS.Looper.MainLooper) Run();
        else activity.RunOnUiThread(Run);

        void Run() => activity.Window?.DecorView?.PerformHapticFeedback(feedback);
    }
}
