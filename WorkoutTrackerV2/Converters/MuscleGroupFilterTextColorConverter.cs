using System.Globalization;
namespace WorkoutTrackerV2.Converters
{
    public class MuscleGroupFilterTextColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var selected = value as string;
            var pill = parameter as string;
            return selected == pill
                ? Color.FromArgb("#FFFFFF")
                : Color.FromArgb("#666666");
        }
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}