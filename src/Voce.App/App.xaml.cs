using Microsoft.UI.Xaml;
using Voce.Core.Data;

namespace Voce.App;

public partial class App : Application
{
    public static MainWindow MainWindow { get; private set; } = null!;

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppServices.Initialize(VoceDatabase.DefaultPath);
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }

    private static void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            var log = Path.Combine(AppContext.BaseDirectory, "Data", "error.log");
            Directory.CreateDirectory(Path.GetDirectoryName(log)!);
            File.AppendAllText(log, $"[{DateTime.Now:s}] {e.Exception}\n\n");
        }
        catch { /* never throw from the crash handler */ }
    }
}
