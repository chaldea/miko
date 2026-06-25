using Miko.Common;

namespace Miko.Simulator;

/// <summary>
/// 一个模拟设备的描述：逻辑分辨率、像素密度与安全区边距。
/// <para>
/// 逻辑尺寸（<see cref="LogicalWidth"/>/<see cref="LogicalHeight"/>）是应用看到的视口大小，
/// 与 Miko 引擎的布局坐标一致；<see cref="Scale"/> 仅影响模拟器把设备画面合成到窗口时的清晰度
/// （以物理像素渲染离屏画面，再缩放绘制到屏幕）。
/// </para>
/// <para>
/// 安全区（<see cref="SafeArea"/>）模拟刘海 / 状态栏 / Home 指示条，转发给应用的引擎，
/// 使应用内容内缩到安全区内——与真机 iOS/Android 宿主行为一致。
/// </para>
/// </summary>
public sealed record DeviceProfile(
    string Name,
    int LogicalWidth,
    int LogicalHeight,
    float Scale,
    SafeAreaInsets SafeArea)
{
    /// <summary>设备类别（用于面板分组与边框样式）。</summary>
    public DeviceKind Kind { get; init; } = DeviceKind.Phone;

    /// <summary>物理像素宽度（离屏画面以此分辨率渲染，保证缩放后清晰）。</summary>
    public int PixelWidth => (int)MathF.Round(LogicalWidth * Scale);

    /// <summary>物理像素高度。</summary>
    public int PixelHeight => (int)MathF.Round(LogicalHeight * Scale);

    public override string ToString() => $"{Name} ({LogicalWidth}×{LogicalHeight} @{Scale:0.#}x)";

    // 内置常用设备预设。数值取自各设备的逻辑点（point）分辨率与安全区。
    public static DeviceProfile IPhone15Pro { get; } = new(
        "iPhone 15 Pro", 393, 852, 3f, new SafeAreaInsets(0, 59, 0, 34)) { Kind = DeviceKind.Phone };

    public static DeviceProfile IPhoneSE { get; } = new(
        "iPhone SE", 375, 667, 2f, new SafeAreaInsets(0, 20, 0, 0)) { Kind = DeviceKind.Phone };

    public static DeviceProfile Pixel7 { get; } = new(
        "Pixel 7", 412, 915, 2.625f, new SafeAreaInsets(0, 24, 0, 0)) { Kind = DeviceKind.Phone };

    public static DeviceProfile IPadMini { get; } = new(
        "iPad mini", 744, 1133, 2f, new SafeAreaInsets(0, 24, 0, 20)) { Kind = DeviceKind.Tablet };

    /// <summary>内置设备预设列表（面板下拉默认使用）。</summary>
    public static IReadOnlyList<DeviceProfile> Defaults { get; } = new[]
    {
        IPhone15Pro, IPhoneSE, Pixel7, IPadMini,
    };
}

/// <summary>模拟设备的类别。</summary>
public enum DeviceKind
{
    Phone,
    Tablet,
}
