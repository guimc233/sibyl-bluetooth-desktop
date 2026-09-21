using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SibylEarbuds.App.ViewModels;

namespace SibylEarbuds.App.Views;

public sealed partial class DeviceDashboardPage : Page
{
    public MainViewModel Vm { get; }

    public DeviceDashboardPage()
    {
        Vm = ((App)Application.Current).MainViewModel!;
        InitializeComponent();
    }
}
