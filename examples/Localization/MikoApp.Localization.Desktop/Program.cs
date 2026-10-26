using Miko.Windowing;
using MikoApp.Localization;

namespace MikoApp.Localization.Desktop;

public static class Program
{
    /// <param name="args">Optional starting culture, e.g. <c>ar-EG</c>; the OS language otherwise.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        var context = App.CreateContext(args.FirstOrDefault());
        App.InitializeHotReload(context);
        context.RunDesktop();
    }
}
