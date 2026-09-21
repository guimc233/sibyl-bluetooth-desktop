using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using SibylEarbuds.App.ViewModels;
using SibylEarbuds.App.Views;

namespace SibylEarbuds.App;

public sealed partial class ShellPage : Page
{
    public MainViewModel Vm { get; }

    /// <summary>Exposed so the hosting Window can register it as the drag region.</summary>
    public UIElement TitleBarElement => TitleBar;

    public ShellPage()
    {
        Vm = ((App)Application.Current).MainViewModel!;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Idempotent: Loaded can fire again if the shell is re-parented.
        Vm.NavigateToDashboardRequested -= NavigateToDashboard;
        Vm.NavigateToDiscoveryRequested -= NavigateToDiscovery;
        Vm.NavigateToDashboardRequested += NavigateToDashboard;
        Vm.NavigateToDiscoveryRequested += NavigateToDiscovery;

        if (ContentFrame.CurrentSourcePageType is null)
        {
            ContentFrame.Navigate(typeof(DeviceDiscoveryPage), null, new DrillInNavigationTransitionInfo());
        }
    }

    private void NavigateToDashboard()
    {
        if (ContentFrame.CurrentSourcePageType == typeof(DeviceDashboardPage))
        {
            return;
        }

        ContentFrame.Navigate(typeof(DeviceDashboardPage), null, new DrillInNavigationTransitionInfo());
    }

    private void NavigateToDiscovery()
    {
        if (ContentFrame.CurrentSourcePageType == typeof(DeviceDiscoveryPage))
        {
            return;
        }

        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack(new SlideNavigationTransitionInfo());
        }
        else
        {
            ContentFrame.Navigate(typeof(DeviceDiscoveryPage), null, new SlideNavigationTransitionInfo());
        }
    }

    private void OnToggleAdvancedSettingsClick(object sender, RoutedEventArgs e)
        => Vm.ToggleAdvancedSettingsCommand.Execute(null);
}
