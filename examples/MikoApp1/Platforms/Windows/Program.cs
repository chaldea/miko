namespace MikoApp1.Platforms.Windows;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = MikoProgram.CreateMikoApp();
        app.Run();
    }
}
