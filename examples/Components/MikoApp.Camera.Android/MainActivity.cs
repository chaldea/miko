using Android.App;
using Android.Content.PM;
using Android.OS;
using Miko.Android;
using Miko.Android.Camera;
using Miko.Android.Native;

namespace MikoApp.Camera.Android;

// ConfigurationChanges keeps the activity (and the open camera) alive across rotation; a recreated
// activity would close the camera and start the app over.
[Activity(Label = "Miko Camera", MainLauncher = true, Theme = "@android:style/Theme.Material.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.UiMode
        | ConfigChanges.FontScale | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : Activity
{
    private MikoSurfaceView? _view;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // UseAndroidCamera(): Camera2 preview (zero-copy OES), analysis frames and stills; it asks for
        // the CAMERA permission itself. UseAndroidNative(): haptics and the system back button.
        _view = MikoAndroidApp.CreateView(this, () => MikoApp.Camera.App.CreateContext(builder =>
        {
            builder.UseAndroidCamera();
            builder.UseAndroidNative();
        }));
        SetContentView(_view);
    }

    // The back button closes overlays before it leaves the app (ISSUE-138).
    public override void OnBackPressed()
    {
        if (_view is not null && _view.HandleBackPressed()) return;

        base.OnBackPressed();
    }
}
