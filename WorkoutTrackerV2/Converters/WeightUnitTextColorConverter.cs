using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class WeightUnitTextColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string selected && parameter is string unit)
                return selected == unit ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#999999");
            return Color.FromArgb("#999999");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}