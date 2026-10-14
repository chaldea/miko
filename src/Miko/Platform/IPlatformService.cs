namespace Miko.Platform;

/// <summary>
/// 组件面向的平台服务（ISSUE-138）。聚合组件库在交互增强时需要的那部分平台能力：平台识别
/// （继承自 <see cref="IPlatformInfo"/>）、系统返回键、屏幕方向/息屏、触觉反馈。
///
/// <para><b>与 <c>Miko.Native</c> 的区别：</b><c>Miko.Native</c> 是面向**应用开发者**的能力层，
/// 覆盖相机、文件系统等完整原生 API，方法为 <c>Task</c> 且平台不支持时抛
/// <see cref="PlatformNotSupportedException"/>。组件库需要的是另一组东西——它在渲染期间同步
/// 调用、不能因为跑在桌面上就崩，也不需要那些重型能力。因此本接口的所有成员都是**同步、
/// 无分配、渲染期可安全调用**的，不支持的能力是静默降级的空实现。</para>
///
/// <para>
/// 实现由平台宿主提供（桌面 / Android / iOS / Simulator）。在 <c>MikoAppBuilder.CreateDefault</c>
/// 中注册为单例，与 <see cref="IPlatformInfo"/> 指向同一实例，因此模拟器切换设备时两侧一起变化。
/// </para>
/// </summary>
public interface IPlatformService : IPlatformInfo
{
    /// <summary>系统返回键的分发链。平台宿主把硬件返回、返回手势或 Escape 交给它。</summary>
    BackButtonRegistry Back { get; }

    /// <summary>屏幕方向与息屏控制。桌面等不支持的平台返回空实现，调用永远安全。</summary>
    IOrientationService Orientation { get; }

    /// <summary>触觉反馈（ISSUE-149）。桌面等没有触觉硬件的平台返回空实现，调用永远安全。</summary>
    IHapticFeedback Haptics { get; }
}
