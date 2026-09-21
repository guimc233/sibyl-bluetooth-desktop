using System.Runtime.InteropServices;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace SibylEarbuds.App;

public sealed partial class MainWindow : Window
{
    // Target size in DIPs (device independent pixels).
    private const double ContentWidthDip = 1200;
    private const double ContentHeightDip = 820;

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    public MainWindow()
    {
        InitializeComponent();

        var rootShell = new ShellPage();
        Content = rootShell;

        Title = "SIBYL MUSIC - 电脑版耳机控制中枢";
        SystemBackdrop = new MicaBackdrop();
        // 使用系统原生自带顶栏，不自己画顶栏
        ExtendsContentIntoTitleBar = false;

        ResizeToContent();
    }

    /// <summary>
    /// WinUI 3 has no SizeToContent. AppWindow.Resize takes physical pixels,
    /// so the DIP target is scaled by the window's current DPI.
    /// </summary>
    private void ResizeToContent()
    {
        IntPtr hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        uint dpi = GetDpiForWindow(hwnd);
        double scale = dpi == 0 ? 1.0 : dpi / 96.0;

        AppWindow.Resize(new SizeInt32(
            (int)(ContentWidthDip * scale),
            (int)(ContentHeightDip * scale)));
    }
}
