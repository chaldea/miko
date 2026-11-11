using Microsoft.Extensions.DependencyInjection;
using Miko.Hosting;
using Miko.Native;
using Miko.Native.App;
using Miko.Native.Browser;
using Miko.Native.Clipboard;
using Miko.Native.Device;
using Miko.Native.Geolocation;
using Miko.Native.Haptics;
using Miko.Native.Keyboard;
using Miko.Native.Network;
using Miko.Native.ScreenReader;

namespace Miko.Browser.Native;

/// <summary>Dependency-injection registration for capabilities provided by web APIs.</summary>
public static class BrowserNativeServiceExtensions
{
    /// <summary>
    /// Registers browser implementations where the web platform has an equivalent API. Capabilities
    /// with no browser equivalent keep Miko.Native's <c>Null*</c> implementation and throw
    /// <see cref="PlatformNotSupportedException"/> when called.
    /// </summary>
    public static MikoAppBuilder UseBrowserNative(this MikoAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddMikoNative();
        builder.Services.ReplaceNative<IAppService, BrowserAppService>();
        builder.Services.ReplaceNative<IBrowserService, BrowserBrowserService>();
        builder.Services.ReplaceNative<IClipboardService, BrowserClipboardService>();
        builder.Services.ReplaceNative<IDeviceService, BrowserDeviceService>();
        builder.Services.ReplaceNative<IGeolocationService, BrowserGeolocationService>();
        builder.Services.ReplaceNative<IHapticsService, BrowserHapticsService>();
        builder.Services.ReplaceNative<IKeyboardService, BrowserKeyboardService>();
        builder.Services.ReplaceNative<INetworkService, BrowserNetworkService>();
        builder.Services.ReplaceNative<IScreenReaderService, BrowserScreenReaderService>();
        builder.UseHaptics(_ => new BrowserHapticFeedback());
        return builder;
    }
}
