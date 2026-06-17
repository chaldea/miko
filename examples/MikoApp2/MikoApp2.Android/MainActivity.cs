using Android.App;
using Android.OS;
using Miko.Android;

namespace MikoApp2.Android;

[Activity(
    Label = "Miko Multiplatform App",
    MainLauncher = true,
    Theme = "@android:style/Theme.Material.Light.NoActionBar")]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            Window?.SetDecorFitsSystemWindows(false);
        }
        
        // Reuse the shared app configuration; Miko.Android drives rendering and touch input.
        SetContentView(MikoAndroidApp.CreateView(this, MikoApp2.App.CreateContext));
    }
}
