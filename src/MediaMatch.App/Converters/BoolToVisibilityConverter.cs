using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MediaMatch.App.Converters;

/// <summary>
/// Converts a boolean value to a Visibility value.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}
