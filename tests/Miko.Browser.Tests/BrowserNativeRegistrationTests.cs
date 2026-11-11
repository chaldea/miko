using Microsoft.Extensions.DependencyInjection;
using Miko.Browser.Native;
using Miko.Hosting;
using Miko.Native.App;
using Miko.Native.Browser;
using Miko.Native.Camera;
using Miko.Native.Clipboard;
using Miko.Native.Device;
using Miko.Native.Filesystem;
using Miko.Native.Geolocation;
using Miko.Native.Haptics;
using Miko.Native.Keyboard;
using Miko.Native.Network;
using Miko.Native.ScreenReader;
using Miko.Platform;
using Shouldly;

namespace Miko.Browser.Tests;

public class BrowserNativeRegistrationTests
{
    [Fact]
    public void Should_register_only_browser_backed_capability_implementations()
    {
        var context = MikoAppBuilder.CreateDefault().UseBrowserNative().Build();
        var services = context.Services;

        services.GetRequiredService<IAppService>().ShouldBeOfType<BrowserAppService>();
        services.GetRequiredService<IBrowserService>().ShouldBeOfType<BrowserBrowserService>();
        services.GetRequiredService<IClipboardService>().ShouldBeOfType<BrowserClipboardService>();
        services.GetRequiredService<IDeviceService>().ShouldBeOfType<BrowserDeviceService>();
        services.GetRequiredService<IGeolocationService>().ShouldBeOfType<BrowserGeolocationService>();
        services.GetRequiredService<IHapticsService>().ShouldBeOfType<BrowserHapticsService>();
        services.GetRequiredService<IKeyboardService>().ShouldBeOfType<BrowserKeyboardService>();
        services.GetRequiredService<INetworkService>().ShouldBeOfType<BrowserNetworkService>();
        services.GetRequiredService<IScreenReaderService>().ShouldBeOfType<BrowserScreenReaderService>();
        services.GetRequiredService<IHapticFeedback>().ShouldBeOfType<BrowserHapticFeedback>();

        services.GetRequiredService<ICameraService>().ShouldBeOfType<NullCameraService>();
        services.GetRequiredService<IFilesystemService>().ShouldBeOfType<NullFilesystemService>();
    }

    [Fact]
    public async Task Browser_service_should_reject_non_http_urls_before_calling_javascript()
    {
        var service = new BrowserBrowserService();
        await Should.ThrowAsync<ArgumentException>(() => service.OpenAsync(new OpenOptions { Url = "javascript:alert(1)" }));
        await Should.ThrowAsync<ArgumentException>(() => service.OpenAsync(new OpenOptions { Url = "/relative" }));
    }
}
