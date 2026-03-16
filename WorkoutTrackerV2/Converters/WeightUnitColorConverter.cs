using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class WeightUnitColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string selected && parameter is string unit)
                return selected == unit ? Color.FromArgb("#1F77F0") : Color.FromArgb("#F5F5F5");
            return Color.FromArgb("#F5F5F5");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}