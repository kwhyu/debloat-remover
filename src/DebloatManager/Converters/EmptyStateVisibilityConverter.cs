using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DebloatManager.Converters;

public sealed class EmptyStateVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is [int totalCount, bool isBusy])
        {
            return totalCount == 0 && !isBusy ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
