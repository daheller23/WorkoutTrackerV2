using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class MuscleGroupColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string muscleGroup ? muscleGroup switch
            {
                "Chest" => Color.FromArgb("#1F77F0"),
                "Back" => Color.FromArgb("#4CAF50"),
                "Legs" => Color.FromArgb("#FF9800"),
                "Shoulders" => Color.FromArgb("#9C27B0"),
                "Arms" => Color.FromArgb("#FF6B6B"),
                "Core" => Color.FromArgb("#00BCD4"),
                _ => Color.FromArgb("#1F77F0")
            } : Color.FromArgb("#1F77F0");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}