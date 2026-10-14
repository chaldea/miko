using Android.App;
using Android.OS;
using Miko.Android;

namespace MikoApp1.Droid;

[Activity(Label = "Miko Demo App", MainLauncher = true)]
public class MainActivity : Activity
{
    private MikoSurfaceView? _view;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // 复用共享 UI 工程的应用配置，由 Miko.Android 承载渲染与触摸输入。
        _view = MikoAndroidApp.CreateView(this, MikoApp1.App.CreateContext);
        SetContentView(_view);
    }

    // 硬件返回键先交给已打开的覆盖层（菜单/模态框/动作面板）消费，无人消费才交还系统
    // （ISSUE-138）。缺了这段转发，系统默认行为会直接退出应用，覆盖层还开着。
    public override void OnBackPressed()
    {
        if (_view is not null && _view.HandleBackPressed()) return;

        base.OnBackPressed();
    }
}
