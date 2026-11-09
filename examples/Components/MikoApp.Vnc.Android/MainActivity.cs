using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Miko.Android;
using Miko.Android.Native;

namespace MikoApp.Vnc.Android;

// ConfigurationChanges keeps the activity (and the session) alive across rotation and the keyboard
// coming and going; AdjustResize shrinks the view above the soft keyboard, so the keys bar sits on it.
[Activity(Label = "MikoVnc", MainLauncher = true, Theme = "@android:style/Theme.Material.NoActionBar",
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.UiMode
        | ConfigChanges.FontScale | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : Activity
{
    // Launch extras that become the variables App reads (an Android app has no environment of its own):
    // adb shell am start -n com.miko.vncdemo/crc….MainActivity -e VNC_HOST 192.168.1.10 -e VNC_PASSWORD …
    private static readonly string[] Settings = ["VNC_HOST", "VNC_PASSWORD"];

    private MikoSurfaceView? _view;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        foreach (var name in Settings)
            if (Intent?.GetStringExtra(name) is { Length: > 0 } value) System.Environment.SetEnvironmentVariable(name, value);

        // UseAndroidNative(): fullscreen locks landscape and hides the system bars; a held touch ticks the haptics.
        _view = MikoAndroidApp.CreateView(this, () => MikoApp.Vnc.App.CreateContext(builder => builder.UseAndroidNative()));
        SetContentView(_view);
    }

    // Back closes the password dialog and the settings, then leaves fullscreen, then the app.
    public override void OnBackPressed()
    {
        if (_view is not null && _view.HandleBackPressed()) return;
        base.OnBackPressed();
    }
}
