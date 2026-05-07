namespace MikoApp1.WinUI;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = MikoApp1.MikoProgram.CreateMikoApp();
        app.Run();
    }
}
