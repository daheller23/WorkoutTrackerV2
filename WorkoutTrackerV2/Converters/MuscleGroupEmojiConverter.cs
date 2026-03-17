using System.Globalization;

namespace WorkoutTrackerV2.Converters
{
    public class MuscleGroupEmojiConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is string muscleGroup ? muscleGroup switch
            {
                "Chest" => "🔵",
                "Back" => "🟢",
                "Legs" => "🟠",
                "Shoulders" => "🟣",
                "Arms" => "🔴",
                "Core" => "🩵",
                _ => "⚪"
            } : "⚪";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}