using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SibylEarbuds.App.Controls;

public partial class BatteryRingControl : UserControl
{
    public static readonly DependencyProperty BatteryProperty =
        DependencyProperty.Register(nameof(Battery), typeof(int), typeof(BatteryRingControl),
            new PropertyMetadata(100, OnBatteryChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(BatteryRingControl),
            new PropertyMetadata("耳机", OnLabelChanged));

    public static readonly DependencyProperty IsChargingProperty =
        DependencyProperty.Register(nameof(IsCharging), typeof(bool), typeof(BatteryRingControl),
            new PropertyMetadata(false, OnChargingChanged));

    public static readonly DependencyProperty ImageSourceProperty =
        DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(BatteryRingControl),
            new PropertyMetadata(null, OnImageSourceChanged));

    public ImageSource? ImageSource
    {
        get => (ImageSource?)GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public int Battery
    {
        get => (int)GetValue(BatteryProperty);
        set => SetValue(BatteryProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool IsCharging
    {
        get => (bool)GetValue(IsChargingProperty);
        set => SetValue(IsChargingProperty, value);
    }

    public BatteryRingControl()
    {
        InitializeComponent();
        UpdateUI();
    }

    private static void OnBatteryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BatteryRingControl control) control.UpdateUI();
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BatteryRingControl control) control.TxtLabel.Text = e.NewValue?.ToString() ?? string.Empty;
    }

    private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BatteryRingControl control) control.ImgDevice.Source = e.NewValue as ImageSource;
    }

    private static void OnChargingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BatteryRingControl control)
        {
            control.TxtCharging.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdateUI()
    {
        TxtPercent.Text = $"{Battery}%";
        if (Battery > 50)
            RingIndicator.Stroke = new SolidColorBrush(Color.FromRgb(16, 185, 129));
        else if (Battery > 20)
            RingIndicator.Stroke = new SolidColorBrush(Color.FromRgb(245, 158, 11));
        else
            RingIndicator.Stroke = new SolidColorBrush(Color.FromRgb(239, 68, 68));
    }
}
