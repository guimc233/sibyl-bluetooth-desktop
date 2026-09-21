using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SibylEarbuds.App.ViewModels;
using SibylEarbuds.Core.Transport;

namespace SibylEarbuds.App.Views;

public sealed partial class DeviceDiscoveryPage : Page
{
    public MainViewModel Vm { get; }

    public DeviceDiscoveryPage()
    {
        Vm = ((App)Application.Current).MainViewModel!;
        InitializeComponent();
    }

    private void OnConnectDeviceClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DiscoveredBleDevice device })
        {
            Vm.ConnectCommand.Execute(device);
        }
    }
}
