using Android.App;
using Android.OS;
using Miko.Android;

namespace MikoApp1.Droid;

[Activity(Label = "Miko Demo App", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // 复用共享 UI 工程的应用配置，由 Miko.Android 承载渲染与触摸输入。
        SetContentView(MikoAndroidApp.CreateView(this, MikoApp1.App.CreateContext));
    }
}
