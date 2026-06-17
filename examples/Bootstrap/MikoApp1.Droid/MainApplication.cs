using Android.App;
using Android.Runtime;

namespace MikoApp1.Droid;

[Application]
public class MainApplication : Application
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership) { }
}
