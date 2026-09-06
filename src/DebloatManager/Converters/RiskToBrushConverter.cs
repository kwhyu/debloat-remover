using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DebloatManager.Models;

namespace DebloatManager.Converters;

public sealed class RiskToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Safe = new(Color.FromRgb(0x2E, 0xA4, 0x4A));
    private static readonly SolidColorBrush Caution = new(Color.FromRgb(0xD9, 0x8E, 0x04));
    private static readonly SolidColorBrush Risky = new(Color.FromRgb(0xD1, 0x34, 0x38));
    private static readonly SolidColorBrush Unknown = new(Color.FromRgb(0x8A, 0x8A, 0x8A));

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value as RiskLevel?) switch
        {
            RiskLevel.Safe => Safe,
            RiskLevel.Caution => Caution,
            RiskLevel.Risky => Risky,
            _ => Unknown
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
