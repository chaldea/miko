using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Miko.Browser.Native;

[SupportedOSPlatform("browser")]
internal static partial class BrowserNativeInterop
{
    private const string Module = "miko-browser-native";

    [JSImport("getLocationPath", Module)] internal static partial string GetLocationPath();
    [JSImport("setLocationPath", Module)] internal static partial void SetLocationPath(string path);
    [JSImport("replaceLocationPath", Module)] internal static partial void ReplaceLocationPath(string path);
    [JSImport("navigateBack", Module)] internal static partial void NavigateBack();
    [JSImport("isAppActive", Module)] internal static partial bool IsAppActive();
    [JSImport("getLaunchUrl", Module)] internal static partial string GetLaunchUrl();
    [JSImport("getLanguage", Module)] internal static partial string GetLanguage();

    [JSImport("openBrowser", Module)] internal static partial bool OpenBrowser(string url, string windowName);
    [JSImport("closeBrowser", Module)] internal static partial bool CloseBrowser();

    [JSImport("writeClipboardText", Module)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    internal static partial Task WriteClipboardText(string text);

    [JSImport("writeClipboardImage", Module)]
    [return: JSMarshalAs<JSType.Promise<JSType.Void>>]
    internal static partial Task WriteClipboardImage(string dataUrl);

    [JSImport("readClipboard", Module)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    internal static partial Task<string> ReadClipboard();

    [JSImport("getDeviceId", Module)] internal static partial string GetDeviceId();
    [JSImport("getDeviceInfo", Module)] internal static partial string GetDeviceInfo();

    [JSImport("getBatteryInfo", Module)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    internal static partial Task<string> GetBatteryInfo();

    [JSImport("getNetworkStatus", Module)] internal static partial string GetNetworkStatus();
    [JSImport("subscribeNetwork", Module)] internal static partial void SubscribeNetwork();
    [JSImport("unsubscribeNetwork", Module)] internal static partial void UnsubscribeNetwork();

    [JSImport("speak", Module)] internal static partial bool Speak(string text, string? language);

    [JSImport("supportsVibration", Module)] internal static partial bool SupportsVibration();
    [JSImport("vibrate", Module)] internal static partial bool Vibrate(string pattern);

    [JSImport("supportsGeolocation", Module)] internal static partial bool SupportsGeolocation();

    [JSImport("getCurrentPosition", Module)]    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    internal static partial Task<string> GetCurrentPosition(string optionsJson);

    [JSImport("watchPosition", Module)] internal static partial string WatchPosition(string optionsJson);
    [JSImport("clearPositionWatch", Module)] internal static partial void ClearPositionWatch(string watchId);

    [JSImport("checkGeolocationPermission", Module)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    internal static partial Task<string> CheckGeolocationPermission();

    [JSImport("requestGeolocationPermission", Module)]
    [return: JSMarshalAs<JSType.Promise<JSType.String>>]
    internal static partial Task<string> RequestGeolocationPermission();
}
