using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Miko.Android;
using Miko.Android.Native;

namespace MikoApp.Reader.Android;

[Activity(Label = "Miko Reader", MainLauncher = true, Theme = "@android:style/Theme.Material.Light.NoActionBar",
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.UiMode
        | ConfigChanges.FontScale | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden)]
public class MainActivity : Activity
{
    private MikoSurfaceView? _view;
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _view = MikoAndroidApp.CreateView(this, () => MikoApp.Reader.App.CreateContext(builder => builder.UseAndroidNative()));
        SetContentView(_view);
    }
    public override void OnBackPressed()
    {
        if (_view is not null && _view.HandleBackPressed()) return;
        base.OnBackPressed();
    }
}
