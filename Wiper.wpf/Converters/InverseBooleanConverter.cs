using System;
using System.Globalization;
using System.Windows.Data;

namespace Wiper.wpf.Converters; // Kontrollera att detta matchar xmlns:conv i XAML

public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}