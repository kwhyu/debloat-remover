using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DebloatManager;

public sealed class BoolToVisibility : IValueConverter
{
    public static readonly BoolToVisibility Instance = new();

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
