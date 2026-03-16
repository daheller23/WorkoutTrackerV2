using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class RelativeDateConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not DateTime date) return string.Empty;
            int days = (DateTime.Today - date.Date).Days;
            return days switch
            {
                0 => "Today",
                1 => "Yesterday",
                _ => $"{days} days ago"
            };
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}