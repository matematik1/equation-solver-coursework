using Avalonia;
using System;

namespace EquationSolver;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("App starting...");
        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
