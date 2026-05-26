using SQLite;
using WorkoutTrackerV2.Helpers;

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
        public Color MuscleGroupColor => Color.FromArgb(ColorHelper.GetMuscleGroupColor(MuscleGroup));
        public string MuscleGroupEmoji => MuscleGroup switch
        {
            "Chest" => "🔵",
            "Back" => "🟢",
            "Legs" => "🟠",
            "Shoulders" => "🟣",
            "Biceps" => "🔴",
            "Triceps" => "🔴",
            "Forearms" => "🟡",
            "Core" => "⚪",
            _ => "⭐"
        };
    }
}