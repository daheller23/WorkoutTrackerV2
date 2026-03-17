using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class VolumeComparisonColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool isUp && isUp
                ? Color.FromArgb("#4CAF50")
                : Color.FromArgb("#FF6B6B");

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}