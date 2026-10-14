namespace Miko.Platform;

/// <summary>
/// 屏幕方向。与 <see cref="HostPlatform"/> 不同，这是**运行时可变**的物理屏幕方向。
///
/// <para>
/// 刻意不叫 <c>Orientation</c>：<c>Miko.Common.Orientation</c> 已经表示模拟器的**设备外框朝向**
/// （一个预览概念），而这里是操作系统层面的屏幕方向 API。名字相同会让同时引用两个命名空间的
/// 文件产生歧义，也容易让人误以为二者可以互换。
/// </para>
/// </summary>
public enum ScreenOrientation
{
    /// <summary>竖屏，不指定上下（跟随设备传感器）。</summary>
    Portrait,

    /// <summary>横屏，不指定左右（跟随设备传感器）。</summary>
    Landscape,

    /// <summary>竖屏，Home 键在下。</summary>
    PortraitPrimary,

    /// <summary>竖屏，Home 键在上。</summary>
    PortraitSecondary,

    /// <summary>横屏，Home 键在左。</summary>
    LandscapePrimary,

    /// <summary>横屏，Home 键在右。</summary>
    LandscapeSecondary,
}

/// <summary>
/// 屏幕方向与息屏控制（ISSUE-138）。供组件在特定交互期间请求平台行为——例如视频全屏时
/// 锁定横屏并保持屏幕常亮。
///
/// <para>
/// 与 <see cref="IPlatformService"/> 的定位一致：**同步调用、不支持时静默降级**。桌面与 Web
/// 没有可切换的屏幕方向，其实现是空操作而非抛异常——组件不该因为跑在桌面上就崩。
/// </para>
/// </summary>
public interface IOrientationService
{
    /// <summary>
    /// 锁定屏幕方向。<paramref name="orientation"/> 为 <c>null</c> 时仅禁止自动旋转，不指定方向。
    /// 与 <see cref="Unlock"/> 成对使用，引用计数——嵌套锁定不会互相干扰。
    /// </summary>
    void Lock(ScreenOrientation? orientation = null);

    /// <summary>解除一次方向锁定。已无锁定时为空操作。</summary>
    void Unlock();

    /// <summary>请求切换到一个具体方向而不加锁（仅当当前允许自动旋转时生效）。</summary>
    Task SetAsync(ScreenOrientation orientation);

    /// <summary>请求保持屏幕常亮；引用计数，与 <see cref="AllowSleep"/> 成对使用。</summary>
    void KeepAwake();

    /// <summary>解除一次屏幕常亮保持。已无保持时为空操作。</summary>
    void AllowSleep();
}

/// <summary>
/// <see cref="IOrientationService"/> 的空实现：所有调用都不做任何事。
/// 桌面、Web 以及未接入平台的宿主使用它，因此组件无需判空。
/// </summary>
public sealed class NullOrientationService : IOrientationService
{
    public void Lock(ScreenOrientation? orientation = null) { }
    public void Unlock() { }
    public Task SetAsync(ScreenOrientation orientation) => Task.CompletedTask;
    public void KeepAwake() { }
    public void AllowSleep() { }
}
