using Android.App;
using Android.Content.PM;
using Android.OS;
using Miko.Android;
using Miko.Android.Native;
using Miko.Android.Video;

namespace MikoApp.Player.Android;

// ConfigurationChanges keeps the activity (and the player) alive across rotation: fullscreen locks
// landscape (ISSUE-149), and a recreated activity would start over with a new, windowed player.
[Activity(Label = "MikoPlayer", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.UiMode
        | ConfigChanges.FontScale | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : Activity
{
    private MikoSurfaceView? _view;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // MediaPlayer needs a file path, not an asset stream: extract the bundled clip once.
        string local = Path.Combine(FilesDir!.AbsolutePath, "miko-local.mp4");
        using (var source = Assets!.Open("miko-local.mp4"))
        using (var destination = File.Create(local)) source.CopyTo(destination);

        // UseAndroidVideo(): MediaPlayer with zero-copy OES frames. UseAndroidNative(): fullscreen
        // locks landscape, keeps the screen on and hides the system bars; the long press ticks the
        // haptics. A launch intent's "source" extra opens another URI first.
        _view = MikoAndroidApp.CreateView(this, () => MikoApp.Player.App.CreateContext(builder =>
        {
            builder.UseAndroidVideo();
            builder.UseAndroidNative();
        }, localSource: local, initialSource: Intent?.GetStringExtra("source")));
        SetContentView(_view);
    }

    // The back button leaves the settings panel and fullscreen before it leaves the app (ISSUE-138).
    public override void OnBackPressed()
    {
        if (_view is not null && _view.HandleBackPressed()) return;

        base.OnBackPressed();
    }
}
