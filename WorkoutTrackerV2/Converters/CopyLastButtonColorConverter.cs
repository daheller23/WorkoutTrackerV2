using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class CopyLastButtonColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool hasSets && hasSets
                ? Color.FromArgb("#F0F7FF")
                : Color.FromArgb("#F5F5F5");

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}