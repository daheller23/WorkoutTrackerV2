namespace WorkoutTrackerV2.Helpers
{
    public static class ColorHelper
    {
        public static string _defaultColor = "#1F77F0";
        public static string GetMuscleGroupColor(string? muscleGroup) => muscleGroup switch
        {
            "Chest" => "#4A90D9",
            "Back" => "#4CAF50",
            "Legs" => "#FF9800",
            "Shoulders" => "#9C27B0",
            "Arms" => "#FF6B6B",
            "Core" => "#00BCD4",
            _ => "#1F77F0"
        };

        public static string GetDefaultColor()
        {
            return _defaultColor;
        }
    }
}