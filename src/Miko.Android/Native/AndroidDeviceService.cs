using System.Globalization;
using Android.Content;
using Android.OS;
using Android.Provider;
using Miko.Native;
using Miko.Native.Device;
using AndroidBuild = Android.OS.Build;
using DeviceOperatingSystem = Miko.Native.Device.OperatingSystem;

namespace Miko.Android.Native;

/// <summary>Android 设备信息，基于 <see cref="AndroidBuild"/> 与 <see cref="BatteryManager"/>。</summary>
internal sealed class AndroidDeviceService : NativeServiceBase, IDeviceService
{
    private readonly INativeHostContext _hostContext;

    public AndroidDeviceService(INativeHostContext hostContext) => _hostContext = hostContext;

    private Context Context => _hostContext.RequireHost<AndroidNativeHost>().Context;

    public Task<DeviceId> GetIdAsync()
    {
        // ANDROID_ID 按「应用签名 + 用户」派生，卸载重装会变——与 Capacitor 的语义一致，
        // 也正因如此它不是硬件标识。
        var id = Settings.Secure.GetString(Context.ContentResolver, Settings.Secure.AndroidId);

        return Task.FromResult(new DeviceId(id ?? Guid.NewGuid().ToString("n")));
    }

    public Task<DeviceInfo> GetInfoAsync()
    {
        var activityManager = Context.GetSystemService(Context.ActivityService) as ActivityManager;
        var memoryInfo = new ActivityManager.MemoryInfo();
        activityManager?.GetMemoryInfo(memoryInfo);

        // 「已用」= 总内存 - 可用内存。TotalMem 需要 API 16+，本项目最低版本远高于此。
        var memUsed = memoryInfo.TotalMem - memoryInfo.AvailMem;

        return Task.FromResult(new DeviceInfo(
            Name: AndroidBuild.Device ?? AndroidBuild.Model ?? "android",
            Model: AndroidBuild.Model ?? "unknown",
            Platform: DevicePlatform.Android,
            OperatingSystem: DeviceOperatingSystem.Android,
            OsVersion: AndroidBuild.VERSION.Release ?? "unknown",
            IosVersion: null,
            AndroidSdkVersion: (int)AndroidBuild.VERSION.SdkInt,
            Manufacturer: AndroidBuild.Manufacturer ?? "unknown",
            // 模拟器的 Build.Fingerprint 以 "generic" 开头，这是 Android 上判断虚拟设备的惯用手法。
            IsVirtual: (AndroidBuild.Fingerprint?.StartsWith("generic", StringComparison.Ordinal) ?? false)
                       || (AndroidBuild.Fingerprint?.Contains("vbox", StringComparison.OrdinalIgnoreCase) ?? false)
                       || (AndroidBuild.Model?.Contains("Emulator", StringComparison.OrdinalIgnoreCase) ?? false),
            MemUsed: memUsed,
            // Miko 不用 WebView；上报渲染宿主标识。
            WebViewVersion: "Miko.Android"));
    }

    public Task<BatteryInfo> GetBatteryInfoAsync()
    {
        if (Context.GetSystemService(Context.BatteryService) is not BatteryManager battery)
            throw new InvalidOperationException("BatteryManager is unavailable.");

        var level = battery.GetIntProperty((int)BatteryProperty.Capacity);

        // 充电状态没有 BatteryManager 属性，只能读粘性广播 ACTION_BATTERY_CHANGED。
        using var filter = new IntentFilter(Intent.ActionBatteryChanged);
        using var status = Context.RegisterReceiver(null, filter);

        var plugged = status?.GetIntExtra(BatteryManager.ExtraPlugged, 0) ?? 0;

        return Task.FromResult(new BatteryInfo(
            BatteryLevel: level / 100.0,
            IsCharging: plugged != 0));
    }

    public Task<LanguageCode> GetLanguageCodeAsync()
        => Task.FromResult(new LanguageCode(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

    public Task<LanguageTag> GetLanguageTagAsync()
        => Task.FromResult(new LanguageTag(CultureInfo.CurrentUICulture.IetfLanguageTag));
}
