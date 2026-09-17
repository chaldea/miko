namespace Miko.Native.Device;

/// <summary>设备所属平台。</summary>
public enum DevicePlatform
{
    /// <summary>Apple iOS / iPadOS。</summary>
    Ios,

    /// <summary>Google Android。</summary>
    Android,

    /// <summary>Web / 桌面。</summary>
    Web,
}

/// <summary>设备操作系统。</summary>
public enum OperatingSystem
{
    /// <summary>Apple iOS。</summary>
    Ios,

    /// <summary>Google Android。</summary>
    Android,

    /// <summary>Microsoft Windows。</summary>
    Windows,

    /// <summary>Apple macOS。</summary>
    Mac,

    /// <summary>未知系统。</summary>
    Unknown,
}

/// <summary>应用可用的设备标识。</summary>
/// <param name="Identifier">设备标识。iOS 为 <c>identifierForVendor</c>，
/// Android 为持久化的随机 UUID——均非硬件 ID，卸载重装后可能变化。</param>
public sealed record DeviceId(string Identifier);

/// <summary>设备、系统与 WebView 信息。</summary>
/// <param name="Name">设备名称（用户可设置，如「我的 iPhone」）。</param>
/// <param name="Model">设备型号。</param>
/// <param name="Platform">设备平台。</param>
/// <param name="OperatingSystem">操作系统。</param>
/// <param name="OsVersion">操作系统版本字符串。</param>
/// <param name="IosVersion">iOS 主版本号（仅 iOS）。</param>
/// <param name="AndroidSdkVersion">Android SDK 级别（仅 Android）。</param>
/// <param name="Manufacturer">制造商。</param>
/// <param name="IsVirtual">是否运行在模拟器 / 虚拟机上。</param>
/// <param name="MemUsed">当前已用内存（字节）。</param>
/// <param name="WebViewVersion">WebView 版本；Miko 不使用 WebView，平台实现填渲染宿主标识。</param>
public sealed record DeviceInfo(
    string Name,
    string Model,
    DevicePlatform Platform,
    OperatingSystem OperatingSystem,
    string OsVersion,
    int? IosVersion,
    int? AndroidSdkVersion,
    string Manufacturer,
    bool IsVirtual,
    long MemUsed,
    string WebViewVersion);

/// <summary>电池电量与充电状态。</summary>
/// <param name="BatteryLevel">电量，0～1。</param>
/// <param name="IsCharging">是否正在充电。</param>
public sealed record BatteryInfo(double BatteryLevel, bool IsCharging);

/// <summary>两位语言代码。</summary>
/// <param name="Value">如 <c>zh</c>、<c>en</c>。</param>
public sealed record LanguageCode(string Value);

/// <summary>BCP 47 语言标签。</summary>
/// <param name="Value">如 <c>zh-Hans-CN</c>、<c>en-US</c>。</param>
public sealed record LanguageTag(string Value);
