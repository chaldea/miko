using Android.App;
using Android.OS;
using Miko.Android;

namespace MikoMultiplatformApp.Android;

// Theme.Material.Light.NoActionBar hides Android's native title/action bar so the Ionic
// layout's own IonHeader is the only header on screen (it would otherwise stack a second
// bar above it). It's a built-in platform theme, so no Resources/values/styles.xml is needed.
[Activity(
    Label = "Miko Multiplatform App",
    MainLauncher = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
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
        SetContentView(MikoAndroidApp.CreateView(this, MikoMultiplatformApp.App.CreateContext));
    }
}
