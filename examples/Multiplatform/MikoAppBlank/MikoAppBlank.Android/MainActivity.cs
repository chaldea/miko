using Android.App;
using Android.Content;
using Android.OS;
using Miko.Android;
using Miko.Android.Native;

namespace MikoAppBlank.Android;

// Theme.Material.Light.NoActionBar hides Android's native title/action bar so the Ionic
// layout's own IonHeader is the only header on screen (it would otherwise stack a second
// bar above it). It's a built-in platform theme, so no Resources/values/styles.xml is needed.
[Activity(
    Label = "MikoAppBlank",
    MainLauncher = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
    private MikoSurfaceView? _view;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Draw edge-to-edge so Miko owns the full surface; the engine reserves a safe area
        // from the system-bar insets (see MikoSurfaceView.OnApplyWindowInsets) so content is
        // not occluded. Default on Android 15 (API 35); set explicitly for API 30+.
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Window?.SetDecorFitsSystemWindows(false);
        }

        // Reuse the shared app configuration; Miko.Android drives rendering and touch input.
        // UseAndroidNative() supplies the Android implementations of the Miko.Native capability
        // interfaces; the view attaches this Activity to them as it is constructed.
        _view = MikoAndroidApp.CreateView(
            this,
            () => MikoAppBlank.App.CreateContext(builder => builder.UseAndroidNative()));

        SetContentView(_view);
    }

    // Capabilities that start a system Intent (camera, gallery, photo editing) get their result
    // here, not as a return value. Without this forwarding they would wait forever.
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (_view is not null && MikoAndroidApp.HandleActivityResult(_view, requestCode, resultCode, data))
            return;

        base.OnActivityResult(requestCode, resultCode, data);
    }
}
