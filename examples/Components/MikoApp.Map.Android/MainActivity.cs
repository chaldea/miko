using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Miko.Android;
using Miko.Android.Native;

namespace MikoApp.Map.Android;

[Activity(Name = "com.miko.mapdemo.MainActivity", Label = "Miko Map", MainLauncher = true, Exported = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.UiMode
        | ConfigChanges.FontScale | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : Activity
{
    private const int LocalNetworkRequest = 1;
    private Uri _mockServer = null!;
    private string? _amapKey;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _mockServer = new Uri(Setting("MIKO_MOCK_HTTP") ?? "http://10.0.2.2:8080");
        _amapKey = Setting("AMAP_WEB_KEY");
        if (OperatingSystem.IsAndroidVersionAtLeast(37) && string.IsNullOrWhiteSpace(_amapKey)
            && !_mockServer.IsLoopback
            && CheckSelfPermission(global::Android.Manifest.Permission.AccessLocalNetwork) != Permission.Granted)
        {
            RequestPermissions([global::Android.Manifest.Permission.AccessLocalNetwork], LocalNetworkRequest);
            return;
        }
        ShowMap();
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        if (requestCode == LocalNetworkRequest) ShowMap();
    }

    private string? Setting(string name) => Intent?.GetStringExtra(name) ?? System.Environment.GetEnvironmentVariable(name);

    private void ShowMap()
    {
        var app = App.CreateContext(builder =>
        {
            builder.UseAndroidNative();
            // The emulator's host-loopback alias is only reachable directly from the guest.
            if (string.IsNullOrWhiteSpace(_amapKey) && _mockServer.Host == "10.0.2.2")
                builder.Services.AddSingleton(_ => new HttpClient(new HttpClientHandler { UseProxy = false }));
        }, _mockServer.AbsoluteUri, _amapKey);
        SetContentView(MikoAndroidApp.CreateView(this, app));
    }
}
