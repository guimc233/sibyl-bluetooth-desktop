using System.IO;
using System.Text;
using System.Windows;

namespace SibylEarbuds.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 挂载全局异常拦截，确保在任何错误发生时，都会打印到控制台并在弹出对话框的同时记录到日志文件
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            HandleFatalException("AppDomain.UnhandledException", args.ExceptionObject as Exception);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            HandleFatalException("DispatcherUnhandledException", args.Exception);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            HandleFatalException("TaskScheduler.UnobservedTaskException", args.Exception);
            args.SetObserved();
        };

        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("==================================================================");
        Console.WriteLine("  SIBYL Earbuds Manager - Windows 10/11 电脑版耳机控制中枢启动");
        Console.WriteLine("==================================================================");
        Console.WriteLine($"  .NET Version: {Environment.Version}");
        Console.WriteLine($"  OS Version:   {Environment.OSVersion}");
        Console.WriteLine($"  Time:         {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine("  物理音频隔离保障: A2DP 纯净立体声保留，控制流走纯 BLE GATT 通道");
        Console.WriteLine("------------------------------------------------------------------\n");

        base.OnStartup(e);
    }

    private static void HandleFatalException(string source, Exception? ex)
    {
        string message = $"[致命错误] {source}: {ex?.Message}\n{ex?.StackTrace}";
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();

        try
        {
            File.AppendAllText("crash.log", $"[{DateTime.Now}] {message}\n\n");
        }
        catch { }

        MessageBox.Show(
            $"程序运行遇到错误:\n\n{ex?.Message}\n\n详细信息已记录至 crash.log，如有控制台窗口请查看控制台输出。",
            "SIBYL 耳机控制程序异常",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }
}
