using Android.App;
using Android.Content.PM;
using Android.OS;
using Miko.Android;

namespace MikoAppSidemenu.Android;

// Theme.Material.Light.NoActionBar hides Android's native title/action bar so the Ionic
// layout's own IonHeader is the only header on screen (it would otherwise stack a second
// bar above it). It's a built-in platform theme, so no Resources/values/styles.xml is needed.
[Activity(
    Label = "MikoAppSidemenu",
    MainLauncher = true,
    Icon = "@mipmap/appicon",
    RoundIcon = "@mipmap/appicon_round",
    Theme = "@android:style/Theme.Material.Light.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.UiMode
        | ConfigChanges.FontScale | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
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
        _view = MikoAndroidApp.CreateView(this, () => MikoAppSidemenu.App.CreateContext());
        SetContentView(_view);
    }

    // Give an open overlay (menu / modal / action sheet) first refusal on the hardware back
    // button (ISSUE-138). Without this the system default runs and the app exits with the
    // drawer still open.
    public override void OnBackPressed()
    {
        if (_view is not null && _view.HandleBackPressed()) return;

        base.OnBackPressed();
    }
}
