namespace WorkoutTrackerV2.Helpers
{
    public static class ColorHelper
    {
        public static string _defaultColor = "#1F77F0";

        public static string GetMuscleGroupColor(string? muscleGroup) => muscleGroup switch
        {
            "Chest" => "#1F77F0",
            "Back" => "#4CAF50",
            "Legs" => "#FF9800",
            "Shoulders" => "#9C27B0",
            "Biceps" => "#FF6B6B",
            "Triceps" => "#F43F5E",
            "Forearms" => "#FB923C",
            "Core" => "#00BCD4",
            _ => "#1F77F0"
        };

        public static string GetDefaultColor()
        {
            return _defaultColor;
        }
    }
}