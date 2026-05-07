#if ANDROID
using Android.App;
using Android.OS;

namespace MikoApp1.Platforms.Android;

[Activity(Label = "Miko Demo App", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var view = new MikoGLView(this);
        SetContentView(view);
    }
}
#endif
