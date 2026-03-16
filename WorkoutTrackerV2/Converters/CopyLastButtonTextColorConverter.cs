using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class CopyLastButtonTextColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool hasSets && hasSets
                ? Color.FromArgb("#1F77F0")
                : Color.FromArgb("#999999");

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}