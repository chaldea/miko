using Android.App;
using Android.Content.PM;
using Android.OS;
using Miko.Android;
using Miko.Android.Native;
using Miko.Android.Video;

namespace MikoApp.Webcam.Android;

// ConfigurationChanges keeps the activity (and the stream) alive across rotation: fullscreen locks
// landscape, and a recreated activity would reconnect from scratch.
[Activity(Label = "MikoWebcam", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.UiMode
        | ConfigChanges.FontScale | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : Activity
{
    // Launch extras that become the variables App reads (an Android app has no environment of its own):
    // adb shell am start -n com.miko.webcamdemo/crc….MainActivity -e EZVIZ_DEVICE ezopen://CODE@open.ys7.com/SERIAL/1.live …
    private static readonly string[] Settings =
        ["EZVIZ_APP_KEY", "EZVIZ_APP_SECRET", "EZVIZ_ACCESS_TOKEN", "EZVIZ_DEVICE", "EZVIZ_DEVICE_IP", "EZVIZ_CLOUD_RELAY"];

    private MikoSurfaceView? _view;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        foreach (var name in Settings)
            if (Intent?.GetStringExtra(name) is { Length: > 0 } value) System.Environment.SetEnvironmentVariable(name, value);

        // UseAndroidVideo(): MediaCodec straight into the renderer's OES texture, for RTSP streams too.
        // UseAndroidNative(): fullscreen locks landscape and hides the system bars; the disk ticks the haptics.
        _view = MikoAndroidApp.CreateView(this, () => MikoApp.Webcam.App.CreateContext(builder =>
        {
            builder.UseAndroidVideo();
            builder.UseAndroidNative();
        }));
        SetContentView(_view);
    }

    // The back button leaves fullscreen before it leaves the app.
    public override void OnBackPressed()
    {
        if (_view is not null && _view.HandleBackPressed()) return;
        base.OnBackPressed();
    }
}
