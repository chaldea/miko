using UIKit;

namespace Miko.iOS.Native;

/// <summary>
/// iOS Native 服务看到的宿主。<see cref="MikoViewController"/> 在 <c>ViewDidLoad</c> 时
/// 把它 <c>Attach</c> 到 <c>INativeHostContext</c>。
/// <para>
/// 相机、相册、Safari 视图等能力都需要一个 <see cref="UIViewController"/> 来 present；
/// 服务在**调用时**通过本对象取得它，因为服务容器早在 <c>MikoAppBuilder.Build()</c> 时
/// 就已构建、那时视图控制器还不存在。
/// </para>
/// </summary>
public sealed class IosNativeHost
{
    public IosNativeHost(UIViewController viewController) => ViewController = viewController;

    /// <summary>承载 Miko 渲染视图的控制器，用于 present 系统界面。</summary>
    public UIViewController ViewController { get; }

    /// <summary>
    /// 用于 present 的控制器：优先取当前已 present 的最上层控制器，
    /// 否则在已有模态界面之上再 present 会被 UIKit 拒绝。
    /// </summary>
    public UIViewController TopViewController
    {
        get
        {
            var top = ViewController;
            while (top.PresentedViewController is { } presented) top = presented;
            return top;
        }
    }
}
