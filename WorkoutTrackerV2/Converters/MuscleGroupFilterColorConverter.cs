using System.Globalization;
namespace WorkoutTrackerV2.Converters
{
    public class MuscleGroupFilterColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var selected = value as string;
            var pill = parameter as string;
            return selected == pill
                ? Color.FromArgb("#1F77F0")
                : Color.FromArgb("#F0F0F0");
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}