using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DebloatManager.Models;

namespace DebloatManager.Converters;

public sealed class RiskToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Strong = new(Color.FromRgb(0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush Normal = new(Color.FromRgb(0xD0, 0xD0, 0xD0));
    private static readonly SolidColorBrush Muted = new(Color.FromRgb(0x8C, 0x8C, 0x8C));

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value as RiskLevel?) switch
        {
            RiskLevel.Risky => Strong,
            RiskLevel.Protected => Muted,
            RiskLevel.Safe => Muted,
            _ => Normal
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
