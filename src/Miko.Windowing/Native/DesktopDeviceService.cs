using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Miko.Native;
using Miko.Native.Device;
using DeviceOperatingSystem = Miko.Native.Device.OperatingSystem;
using SystemOperatingSystem = System.OperatingSystem;

namespace Miko.Windowing.Native;

/// <summary>
/// 桌面设备信息。系统与硬件信息来自 <see cref="RuntimeInformation"/> 与 <see cref="Environment"/>；
/// 电池信息在 Windows 上走 <c>GetSystemPowerStatus</c>，Linux/macOS 无统一接口故抛异常。
/// </summary>
internal sealed class DesktopDeviceService : NativeServiceBase, IDeviceService
{
    public Task<DeviceId> GetIdAsync()
    {
        // 与移动端语义对齐：不是硬件 ID，而是「本机 + 本应用」稳定的派生标识。
        // 用机器名与用户名哈希，既稳定又不泄露原始信息。
        var seed = $"{Environment.MachineName}|{Environment.UserName}|{AppDomain.CurrentDomain.FriendlyName}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));

        return Task.FromResult(new DeviceId(Convert.ToHexString(hash, 0, 16).ToLowerInvariant()));
    }

    public Task<DeviceInfo> GetInfoAsync()
    {
        var os = DetectOperatingSystem();

        return Task.FromResult(new DeviceInfo(
            Name: Environment.MachineName,
            Model: RuntimeInformation.OSDescription,
            // 桌面在 Capacitor 的三分法里归入 Web——它既不是 ios 也不是 android。
            Platform: DevicePlatform.Web,
            OperatingSystem: os,
            OsVersion: Environment.OSVersion.Version.ToString(),
            IosVersion: null,
            AndroidSdkVersion: null,
            Manufacturer: RuntimeInformation.RuntimeIdentifier,
            IsVirtual: false,
            // Miko 不用 WebView；上报进程工作集，与移动端「当前已用内存」语义一致。
            MemUsed: Environment.WorkingSet,
            // 没有 WebView，用渲染宿主标识填充而不是留空字符串。
            WebViewVersion: $"Miko.Windowing/{RuntimeInformation.FrameworkDescription}"));
    }

    public Task<BatteryInfo> GetBatteryInfoAsync()
    {
        if (!SystemOperatingSystem.IsWindows())
            throw Unsupported();

        if (!GetSystemPowerStatus(out var status))
            throw new InvalidOperationException("GetSystemPowerStatus failed.");

        // BatteryLifePercent 为 255 表示「未知」；此时不编造一个电量。
        if (status.BatteryLifePercent == 255)
            throw new InvalidOperationException("Battery level is unavailable on this machine.");

        // ACLineStatus: 0=离线 1=在线 255=未知。只有明确在线才算充电中。
        return Task.FromResult(new BatteryInfo(
            BatteryLevel: status.BatteryLifePercent / 100.0,
            IsCharging: status.ACLineStatus == 1));
    }

    public Task<LanguageCode> GetLanguageCodeAsync()
        => Task.FromResult(new LanguageCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

    public Task<LanguageTag> GetLanguageTagAsync()
        => Task.FromResult(new LanguageTag(CultureInfo.CurrentUICulture.IetfLanguageTag));

    /// <remarks>
    /// 平台判定走 <c>SystemOperatingSystem</c> 别名：本文件把 <c>OperatingSystem</c> 这个名字让给了
    /// 接口返回类型 <see cref="DeviceOperatingSystem"/>（<c>Miko.Native.Device.OperatingSystem</c>），
    /// 裸写 <c>OperatingSystem.IsWindows()</c> 会解析到那个枚举上。
    /// </remarks>
    private static DeviceOperatingSystem DetectOperatingSystem()
    {
        if (SystemOperatingSystem.IsWindows()) return DeviceOperatingSystem.Windows;
        if (SystemOperatingSystem.IsMacOS() || SystemOperatingSystem.IsMacCatalyst()) return DeviceOperatingSystem.Mac;
        if (SystemOperatingSystem.IsAndroid()) return DeviceOperatingSystem.Android;
        if (SystemOperatingSystem.IsIOS()) return DeviceOperatingSystem.Ios;
        // Capacitor 的 OperatingSystem 没有 Linux 取值；桌面 Linux 归入 Unknown。
        return DeviceOperatingSystem.Unknown;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public uint BatteryLifeTime;
        public uint BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);
}
