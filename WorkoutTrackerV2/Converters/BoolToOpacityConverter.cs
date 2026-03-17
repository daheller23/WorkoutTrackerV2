using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is string str && !string.IsNullOrEmpty(str) ? 1.0 : 0.0;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}