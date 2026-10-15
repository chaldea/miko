namespace Miko.Platform;

/// <summary>
/// 冲击触感的强度（ISSUE-149）。三档与 iOS <c>UIImpactFeedbackGenerator</c> 的
/// Light / Medium / Heavy 对应。
///
/// <para>
/// 刻意不叫 <c>ImpactStyle</c>：<c>Miko.Native.Haptics.ImpactStyle</c> 是面向应用的同名枚举，
/// 应用同时引用两个命名空间时会产生歧义。
/// </para>
/// </summary>
public enum HapticImpactStyle
{
    /// <summary>轻：小而轻的元素碰撞，例如按下拖动手柄。</summary>
    Light,

    /// <summary>中：常规元素碰撞。</summary>
    Medium,

    /// <summary>重：大而重的元素碰撞。</summary>
    Heavy,
}

/// <summary>
/// 组件面向的触觉反馈（ISSUE-149）。成员与 Ionic 的触觉工具函数一一对应
/// （<c>hapticSelectionStart</c> / <c>hapticSelectionChanged</c> / <c>hapticSelectionEnd</c> /
/// <c>hapticImpact</c>），组件按 Ionic 的时机调用即可。
///
/// <para>
/// 与 <see cref="IPlatformService"/> 的定位一致：<b>同步、渲染期可安全调用、不支持时静默降级</b>。
/// 桌面没有触觉硬件，默认实现为空操作——组件不该因为跑在桌面上就崩，也不必判空。
/// </para>
///
/// <para>
/// <b>线程：</b>组件在渲染/输入线程调用，也可能从计时器线程调用（例如滚轮停止后的
/// <see cref="SelectionEnd"/>）。实现必须线程安全，并自行切到平台要求的线程
/// （iOS 主线程、Android UI 线程）。
/// </para>
///
/// <para>
/// <b>选择会话：</b><see cref="SelectionStart"/> 与 <see cref="SelectionEnd"/> 圈出一次连续的选择
/// 手势，<see cref="SelectionChanged"/> 在其中每跳一格时调用。嵌套组件会随父组件重渲染而重建
/// （ISSUE-138 约束 1），一次手势可能被拆成多段 start/end，因此实现必须容忍重复的 start，
/// 也必须容忍 start 之外的 changed。
/// </para>
/// </summary>
public interface IHapticFeedback
{
    /// <summary>一次连续选择手势开始（例如手指按上滚轮、开始拖动排序项）。用于预热触觉引擎。</summary>
    void SelectionStart();

    /// <summary>选择跳到了下一项（滚轮转过一格、拖动越过一行）。这是"咔"的那一下。</summary>
    void SelectionChanged();

    /// <summary>选择手势结束。</summary>
    void SelectionEnd();

    /// <summary>一次冲击触感，表示元素之间的物理碰撞或按下。</summary>
    void Impact(HapticImpactStyle style = HapticImpactStyle.Medium);
}

/// <summary>
/// <see cref="IHapticFeedback"/> 的空实现：所有调用都不做任何事。
/// 桌面以及未接入平台的宿主使用它，因此组件无需判空。
/// </summary>
public sealed class NullHapticFeedback : IHapticFeedback
{
    public void SelectionStart() { }
    public void SelectionChanged() { }
    public void SelectionEnd() { }
    public void Impact(HapticImpactStyle style = HapticImpactStyle.Medium) { }
}
