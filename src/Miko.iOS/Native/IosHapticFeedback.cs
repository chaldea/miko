using Foundation;
using Miko.Native;
using Miko.Platform;

namespace Miko.iOS.Native;

/// <summary>
/// iOS 的组件触觉反馈（ISSUE-149），基于 <c>UISelectionFeedbackGenerator</c> 与
/// <c>UIImpactFeedbackGenerator</c>。
///
/// <para>
/// <b>generator 常驻、不随会话释放：</b>Apple 要求选择反馈由同一个 generator 贯穿整串选择，并在
/// 触发前 <c>Prepare()</c> 预热 Taptic Engine，否则第一下会明显延迟。组件这边一次手势却可能被拆成
/// 很多段 start/end——嵌套组件会随父组件重渲染而重建（ISSUE-138 约束 1），滚轮每跳一格父页面就
/// 重渲染一次。若在 end 时释放、start 时新建，每一格都会落在一个刚建好、还没预热完的 generator 上。
/// 因此 generator 只创建一次：start 只是 <c>Prepare()</c>，每次 changed 之后再 <c>Prepare()</c>
/// 一次让引擎保持就绪，end 不做任何事——不再被 prepare 的引擎几秒后会自行回到空闲。
/// </para>
///
/// <para>
/// iOS 17.5 起 generator 应绑定到发出反馈的视图（<c>GetFeedbackGenerator(view)</c>），旧的无参
/// 构造已标记过时。这里绑定宿主 <see cref="MikoViewController"/> 的根视图，宿主在
/// <c>ViewDidLoad</c> 时才 <c>Attach</c>，因此在调用时读取；宿主缺失时静默跳过。
/// </para>
///
/// <para>
/// generator 只能在主线程使用。iOS 宿主本就在主线程构建组件，此时直接执行；从计时器等其它线程
/// 调用时异步投递到主线程，不阻塞调用方。
/// </para>
/// </summary>
internal sealed class IosHapticFeedback : IHapticFeedback
{
    private readonly INativeHostContext _hostContext;

    // 只在主线程读写。
    private UISelectionFeedbackGenerator? _selection;
    private readonly UIImpactFeedbackGenerator?[] _impacts = new UIImpactFeedbackGenerator?[3];

    public IosHapticFeedback(INativeHostContext hostContext) => _hostContext = hostContext;

    public void SelectionStart() => RunOnMainThread(() => GetSelection()?.Prepare());

    public void SelectionChanged() => RunOnMainThread(() =>
    {
        if (GetSelection() is not { } generator) return;
        generator.SelectionChanged();
        generator.Prepare();
    });

    public void SelectionEnd() { }

    public void Impact(HapticImpactStyle style = HapticImpactStyle.Medium) => RunOnMainThread(() =>
    {
        if (GetImpact(style) is not { } generator) return;
        generator.ImpactOccurred();
        generator.Prepare();
    });

    private UIView? HostView => (_hostContext.Host as IosNativeHost)?.ViewController.View;

    private UISelectionFeedbackGenerator? GetSelection()
    {
        if (_selection is not null) return _selection;
        if (OperatingSystem.IsIOSVersionAtLeast(17, 5))
            return HostView is { } view ? _selection = UISelectionFeedbackGenerator.GetFeedbackGenerator(view) : null;
        return _selection = new UISelectionFeedbackGenerator();
    }

    private UIImpactFeedbackGenerator? GetImpact(HapticImpactStyle style)
    {
        if (_impacts[(int)style] is { } cached) return cached;

        var uiStyle = style switch
        {
            HapticImpactStyle.Light => UIImpactFeedbackStyle.Light,
            HapticImpactStyle.Heavy => UIImpactFeedbackStyle.Heavy,
            _ => UIImpactFeedbackStyle.Medium,
        };
        if (OperatingSystem.IsIOSVersionAtLeast(17, 5))
            return HostView is { } view ? _impacts[(int)style] = UIImpactFeedbackGenerator.GetFeedbackGenerator(uiStyle, view) : null;
        return _impacts[(int)style] = new UIImpactFeedbackGenerator(uiStyle);
    }

    private static void RunOnMainThread(Action action)
    {
        if (NSThread.IsMain) action();
        else UIApplication.SharedApplication.BeginInvokeOnMainThread(action);
    }
}
