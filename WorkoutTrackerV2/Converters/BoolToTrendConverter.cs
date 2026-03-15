using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class BoolToTrendConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? "📈" : "📉";

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}