using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SibylEarbuds.App.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is true;
        if (parameter?.ToString() == "SibylBadge")
        {
            return b ? new SolidColorBrush(Color.FromRgb(16, 124, 65)) : new SolidColorBrush(Color.FromRgb(90, 90, 90));
        }
        return b ? new SolidColorBrush(Color.FromRgb(0, 120, 212)) : new SolidColorBrush(Color.FromRgb(128, 128, 128));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
