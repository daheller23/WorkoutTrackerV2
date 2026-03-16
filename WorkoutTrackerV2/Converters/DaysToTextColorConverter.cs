using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class DaysToTextColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int selectedDays && parameter is string paramStr && int.TryParse(paramStr, out int days))
                return selectedDays == days ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#999999");
            return Color.FromArgb("#999999");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}