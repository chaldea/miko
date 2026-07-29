using Miko.Windowing;

namespace IonicComponents;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        RazorApp.CreateContext().RunDesktop();
    }
}
