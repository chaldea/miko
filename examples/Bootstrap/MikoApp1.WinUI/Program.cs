using Miko.Windowing;

namespace MikoApp1.WinUI;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        MikoApp1.MikoProgram.CreateContext().RunDesktop();
    }
}
