using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace SibylEarbuds.App.Controls;

public sealed partial class EarbudVisualControl : UserControl
{
    public static readonly DependencyProperty BatteryProperty = DependencyProperty.Register(
        nameof(Battery), typeof(int), typeof(EarbudVisualControl), new PropertyMetadata(-1, OnBatteryChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(EarbudVisualControl), new PropertyMetadata("左耳 (L)", OnLabelChanged));

    public static readonly DependencyProperty IsChargingProperty = DependencyProperty.Register(
        nameof(IsCharging), typeof(bool), typeof(EarbudVisualControl), new PropertyMetadata(false, OnChargingChanged));

    public static readonly DependencyProperty IsRightSideProperty = DependencyProperty.Register(
        nameof(IsRightSide), typeof(bool), typeof(EarbudVisualControl), new PropertyMetadata(false, OnSideChanged));

    public static readonly DependencyProperty IsCaseModeProperty = DependencyProperty.Register(
        nameof(IsCaseMode), typeof(bool), typeof(EarbudVisualControl), new PropertyMetadata(false, OnCaseModeChanged));

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
        UpdateBatteryVisual();
    }

    private static void OnBatteryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EarbudVisualControl c)
        {
            c.UpdateBatteryVisual();
        }
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EarbudVisualControl c)
        {
            c.TxtLabel.Text = e.NewValue?.ToString() ?? string.Empty;
        }
    }

    private static void OnChargingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EarbudVisualControl c)
        {
            c.UpdateBatteryVisual();
        }
    }

    private static void OnSideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not EarbudVisualControl c)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            c.EarbudVisual.RenderTransformOrigin = new Point(0.5, 0.5);
            c.EarbudVisual.RenderTransform = new ScaleTransform { ScaleX = -1, ScaleY = 1 };
        }
        else
        {
            c.EarbudVisual.RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };
        }
    }

    private static void OnCaseModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not EarbudVisualControl c)
        {
            return;
        }

        bool isCase = (bool)e.NewValue;
        c.BoxVisual.Visibility = isCase ? Visibility.Visible : Visibility.Collapsed;
        c.EarbudVisual.Visibility = isCase ? Visibility.Collapsed : Visibility.Visible;
    }

    private void UpdateBatteryVisual()
    {
        int battery = Battery;
        if (battery < 0)
        {
            TxtPercent.Text = IsCharging ? "⚡ --" : "--";
            TxtPercent.ClearValue(TextBlock.ForegroundProperty);
            TouchSensor.Fill = GetBrush("EarbudLedBrush");
            return;
        }

        TxtPercent.Text = IsCharging ? $"⚡ {battery}%" : $"{battery}%";

        string brushKey = battery switch
        {
            > 50 => "BatteryGoodBrush",
            > 20 => "BatteryMediumBrush",
            _ => "BatteryLowBrush"
        };
        var brush = GetBrush(brushKey);
        TxtPercent.Foreground = brush;
        TouchSensor.Fill = brush;
    }

    private static Brush GetBrush(string key)
    {
        // WinUI 3 ResourceDictionary 走索引器；缺失键会抛异常，因此用 try/catch 兜底。
        try
        {
            if (Application.Current.Resources[key] is Brush brush)
            {
                return brush;
            }
        }
        catch
        {
            // resource not found
        }

        return new SolidColorBrush();
    }
}
