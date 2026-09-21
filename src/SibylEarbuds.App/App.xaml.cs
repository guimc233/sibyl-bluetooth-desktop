using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.UI.Xaml;
using SibylEarbuds.App.ViewModels;

namespace SibylEarbuds.App;

public partial class App : Application
{
    private Window? _window;

    /// <summary>
    /// Single shared view model for the whole shell (both pages bind to it).
    /// Created on the UI thread inside <see cref="OnLaunched"/>.
    /// </summary>
    public MainViewModel? MainViewModel { get; private set; }

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            WriteCrashLog("AppDomain.UnhandledException", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            WriteCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainViewModel = new MainViewModel();
        _window = new MainWindow();
        _window.Activate();

        Debug.WriteLine("SIBYL Earbuds Manager started (WinUI 3 / Windows App SDK).");
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        WriteCrashLog("Application.UnhandledException", e.Exception);
    }

    private static void WriteCrashLog(string source, Exception? ex)
    {
        string message = $"[致命错误] {source}: {ex?.Message}{Environment.NewLine}{ex?.StackTrace}";
        Debug.WriteLine(message);

        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.AppendAllText(
                path,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}{Environment.NewLine}",
                Encoding.UTF8);
        }
        catch
        {
            // Logging must never take the app down.
        }
    }
}
