using System.Globalization;
using System.Windows.Data;
using DebloatManager.Models;

namespace DebloatManager.Converters;

public sealed class AppTypeToLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value as AppType?) switch
        {
            AppType.UwpApp => "UWP App",
            AppType.Win32App => "Program",
            AppType.SystemFeature => "System Feature",
            AppType.BackgroundService => "Service",
            _ => "Unknown"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
