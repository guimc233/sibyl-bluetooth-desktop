using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using SibylEarbuds.Core.Models;

namespace SibylEarbuds.App.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Reverse { get; set; } = false;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is true;
        if (Reverse) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}

public class ReverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(value is true);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(value is true);
    }
}

public class AncActiveConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AncModeType currentMode && parameter is string targetStr && int.TryParse(targetStr, out int targetModeInt))
        {
            return (int)currentMode == targetModeInt;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public class AncActiveToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AncModeType currentMode && parameter is string targetStr && int.TryParse(targetStr, out int targetModeInt))
        {
            if ((int)currentMode == targetModeInt)
            {
                return new SolidColorBrush(Color.FromRgb(0, 120, 212)); // Windows 11 Fluent Blue
            }
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public class AncActiveToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AncModeType currentMode && parameter is string targetStr && int.TryParse(targetStr, out int targetModeInt))
        {
            if ((int)currentMode == targetModeInt)
            {
                return Brushes.White;
            }
        }
        return new SolidColorBrush(Color.FromRgb(209, 213, 219));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public class BatteryToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int bat)
        {
            if (bat > 50) return new SolidColorBrush(Color.FromRgb(16, 185, 129)); // 翡翠绿
            if (bat > 20) return new SolidColorBrush(Color.FromRgb(245, 158, 11)); // 琥珀黄
            return new SolidColorBrush(Color.FromRgb(239, 68, 68));                // 警示红
        }
        return new SolidColorBrush(Color.FromRgb(156, 163, 175));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public class EqualToIntToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int cur && parameter is string p && int.TryParse(p, out int target))
        {
            return cur == target ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public class EqualToIntToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int cur && parameter is string p && int.TryParse(p, out int target))
        {
            if (cur == target)
            {
                return new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)); // WinUI Active Item Background
            }
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public class EqualToIntToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int cur && parameter is string p && int.TryParse(p, out int target))
        {
            if (cur == target)
            {
                return Brushes.White;
            }
        }
        return new SolidColorBrush(Color.FromRgb(161, 161, 170)); // #A1A1AA
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public class EqualToIntToIndicatorVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int cur && parameter is string p && int.TryParse(p, out int target))
        {
            return cur == target ? Visibility.Visible : Visibility.Hidden;
        }
        return Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
