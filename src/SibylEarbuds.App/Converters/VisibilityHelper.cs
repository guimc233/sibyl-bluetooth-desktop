using Microsoft.UI.Xaml;

namespace SibylEarbuds.App.Converters;

/// <summary>
/// Static x:Bind helpers. Preferred over IValueConverter: no resource lookup,
/// no null-converter crash, and compile-time checked.
/// </summary>
public static class VisibilityHelper
{
    public static Visibility Visible(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility Collapsed(bool value) => value ? Visibility.Collapsed : Visibility.Visible;

    public static bool Not(bool value) => !value;
}

public static class DisplayHelper
{
    public static string Percent(int value) => value < 0 ? "--" : $"{value}%";

    public static string SignedDb(double value) => value >= 0 ? $"+{value:0.#}" : $"{value:0.#}";

    public static string Frequency(int hz) => hz >= 1000 ? $"{hz / 1000.0:0.#}k" : hz.ToString();
}
