using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SibylEarbuds.App.Controls;

public partial class EarbudVisualControl : UserControl
{
    public static readonly DependencyProperty BatteryProperty =
        DependencyProperty.Register(nameof(Battery), typeof(int), typeof(EarbudVisualControl),
            new PropertyMetadata(100, OnBatteryChanged));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(EarbudVisualControl),
            new PropertyMetadata("左耳 (L)", OnLabelChanged));

    public static readonly DependencyProperty IsChargingProperty =
        DependencyProperty.Register(nameof(IsCharging), typeof(bool), typeof(EarbudVisualControl),
            new PropertyMetadata(false, OnChargingChanged));

    public static readonly DependencyProperty IsRightSideProperty =
        DependencyProperty.Register(nameof(IsRightSide), typeof(bool), typeof(EarbudVisualControl),
            new PropertyMetadata(false, OnSideChanged));

    public static readonly DependencyProperty IsCaseModeProperty =
        DependencyProperty.Register(nameof(IsCaseMode), typeof(bool), typeof(EarbudVisualControl),
            new PropertyMetadata(false, OnCaseModeChanged));

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

    public bool IsRightSide
    {
        get => (bool)GetValue(IsRightSideProperty);
        set => SetValue(IsRightSideProperty, value);
    }

    public bool IsCaseMode
    {
        get => (bool)GetValue(IsCaseModeProperty);
        set => SetValue(IsCaseModeProperty, value);
    }

    public EarbudVisualControl()
    {
        InitializeComponent();
        UpdateUI();
    }

    private static void OnBatteryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EarbudVisualControl c) c.UpdateUI();
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EarbudVisualControl c) c.TxtLabel.Text = e.NewValue?.ToString() ?? string.Empty;
    }

    private static void OnChargingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EarbudVisualControl c) c.UpdateUI();
    }

    private static void OnSideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EarbudVisualControl c)
        {
            if ((bool)e.NewValue)
            {
                // 水平翻转以展示右耳镜像外观
                c.EarbudVisual.RenderTransformOrigin = new Point(0.5, 0.5);
                c.EarbudVisual.RenderTransform = new ScaleTransform(-1, 1);
            }
            else
            {
                c.EarbudVisual.RenderTransform = Transform.Identity;
            }
        }
    }

    private static void OnCaseModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EarbudVisualControl c)
        {
            bool isCase = (bool)e.NewValue;
            c.BoxVisual.Visibility = isCase ? Visibility.Visible : Visibility.Collapsed;
            c.EarbudVisual.Visibility = isCase ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void UpdateUI()
    {
        TxtPercent.Text = IsCharging ? $"⚡ {Battery}%" : $"{Battery}%";
        if (Battery > 50)
            TxtPercent.Foreground = new SolidColorBrush(Color.FromRgb(52, 211, 153));
        else if (Battery > 20)
            TxtPercent.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
        else
            TxtPercent.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
    }
}
