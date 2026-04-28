using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MediaMatch.App.Converters;

/// <summary>
/// Converts match mode category strings to themed SolidColorBrush values.
/// Returns the mode accent color when active, or DefaultBrush when inactive.
/// </summary>
public sealed class MatchModeBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush _cardStrokeFallback =
        new(Color.FromArgb(40, 128, 128, 128)); // Subtle neutral border

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string mode || string.IsNullOrEmpty(mode) || mode == "none")
            return _cardStrokeFallback;

        return mode switch
        {
            "episode" => new SolidColorBrush(Color.FromArgb(255, 0, 120, 212)),   // Fluent Blue
            "movie"   => new SolidColorBrush(Color.FromArgb(255, 255, 185, 0)),   // Fluent Yellow
            "music"   => new SolidColorBrush(Color.FromArgb(255, 231, 72, 86)),   // Fluent Red
            "smart"   => new SolidColorBrush(Color.FromArgb(255, 0, 204, 106)),   // Fluent Green
            _         => _cardStrokeFallback
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
