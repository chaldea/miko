using Android.App;
using Android.OS;
using Miko.Android;

namespace MikoMultiplatformApp.Android;

[Activity(Label = "Miko Multiplatform App", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Reuse the shared app configuration; Miko.Android drives rendering and touch input.
        SetContentView(MikoAndroidApp.CreateView(this, MikoMultiplatformApp.App.CreateContext));
    }
}
