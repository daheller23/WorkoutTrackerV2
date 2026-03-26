using SQLite;

namespace WorkoutTrackerV2.Models
{
    public class Exercise
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } = 0;
        public string Name { get; set; } = string.Empty;
        public string MuscleGroup { get; set; } = string.Empty;
        public string SubMuscleGroup { get; set; } = "General";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsCustom { get; set; } = false;
        public Color MuscleGroupColor => MuscleGroup switch
        {
            "Chest" => Color.FromArgb("#4A90D9"),
            "Back" => Color.FromArgb("#27AE60"),
            "Legs" => Color.FromArgb("#E67E22"),
            "Shoulders" => Color.FromArgb("#8E44AD"),
            "Arms" => Color.FromArgb("#E74C3C"),
            "Core" => Color.FromArgb("#5DADE2"),
            _ => Color.FromArgb("#999999")
        };

        public string MuscleGroupEmoji => MuscleGroup switch
        {
            "Chest" => "🔵",
            "Back" => "🟢",
            "Legs" => "🟠",
            "Shoulders" => "🟣",
            "Arms" => "🔴",
            "Core" => "🩵",
            _ => "⭐"
        };
    }
}
