// The Chaldea licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Android.App;
using Android.Content.PM;
using Android.OS;
using Miko.Android;
using Miko.Android.Native;
using Miko.Android.Video;

namespace Anime.Android;

// Theme.Material.Light.NoActionBar hides Android's native title/action bar so the Ionic
// layout's own IonHeader is the only header on screen (it would otherwise stack a second
// bar above it). It's a built-in platform theme, so no Resources/values/styles.xml is needed.
// ConfigurationChanges keeps the activity (and the whole Miko app) alive across rotation, dark-mode
// switches, font / display-size changes and foldable resizes: the player locks landscape when it
// goes fullscreen (ISSUE-149), and a recreated activity would rebuild the app on its first page.
[Activity(
    Label = "Anime",
    MainLauncher = true,
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

        // ISSUE-144 现场探针：用
        // `adb shell am start -n <pkg>/<activity> --ez frameprobe true` 启动即开启，
        // 每帧的分段耗时写进 logcat（tag: MikoFrameProbe）。默认关闭。
        // 再加 `--ez frameprobe-verbose true` 则逐帧输出（测量转场帧率用，见 FrameProbe）。
        if (Intent?.GetBooleanExtra("frameprobe", false) == true)
        {
            FrameProbe.Enable(
                message => global::Android.Util.Log.Info("MikoFrameProbe", message),
                verbose: Intent.GetBooleanExtra("frameprobe-verbose", false));
        }

        // Draw edge-to-edge so Miko owns the full surface; the engine reserves a safe area
        // from the system-bar insets (see MikoSurfaceView.OnApplyWindowInsets) so content is
        // not occluded. Default on Android 15 (API 35); set explicitly for API 30+.
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Window?.SetDecorFitsSystemWindows(false);
        }

        // Reuse the shared app configuration; Miko.Android drives rendering and touch input.
        // UseAndroidNative() supplies the component-facing platform services the player uses in
        // fullscreen: landscape lock and keep-awake (ISSUE-149).
        _view = MikoAndroidApp.CreateView(this, () => Anime.App.CreateContext(builder =>
        {
            builder.UseAndroidVideo();
            builder.UseAndroidNative();
        }));
        SetContentView(_view);
    }

    // Give an open overlay (menu / modal / action sheet) first refusal on the hardware back
    // button (ISSUE-138). Without this the system default runs and the app exits with the
    // overlay still open.
    public override void OnBackPressed()
    {
        if (_view is not null && _view.HandleBackPressed()) return;

        base.OnBackPressed();
    }
}
