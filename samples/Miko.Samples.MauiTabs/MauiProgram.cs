using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Miko.Samples.MauiTabs
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
#if DEBUG
		    builder.Services.AddBlazorWebViewDeveloperTools();
#endif

            builder.Services.AddMiko(options =>
            {
                options.Mode = GetMode();
            });

            return builder.Build();
        }

        public static string GetMode()
        {
            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                return "md";
            }

            if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
            {
                return "ios";
            }

            return "md";
        }
    }
}