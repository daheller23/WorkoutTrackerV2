using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class DaysToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int selectedDays && parameter is string paramStr && int.TryParse(paramStr, out int days))
                return selectedDays == days ? Color.FromArgb("#1F77F0") : Color.FromArgb("#F0F0F0");
            return Color.FromArgb("#F0F0F0");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}