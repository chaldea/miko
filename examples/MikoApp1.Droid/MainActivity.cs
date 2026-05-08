using Android.App;
using Android.OS;

namespace MikoApp1.Droid;

[Activity(Label = "Miko Demo App", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(new MikoGLView(this));
    }
}
