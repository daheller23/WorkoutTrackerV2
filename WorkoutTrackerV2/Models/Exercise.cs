using SQLite;

namespace WorkoutTrackerV2.Models
{
    public class Exercise
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } = 0;
        public bool IsCustom { get; set; } = false;
        public double WeightIncrement { get; set; } = 5.0; // Default to standard 5lb/2.5kg plate jump
        public string Name { get; set; } = string.Empty;
        public string MuscleGroup { get; set; } = string.Empty;
        public string SubMuscleGroup { get; set; } = "General";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
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
