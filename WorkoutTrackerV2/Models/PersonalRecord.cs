using CommunityToolkit.Mvvm.ComponentModel;

namespace WorkoutTrackerV2.Models
{
    public partial class PersonalRecord : ObservableObject
    {
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public string MuscleGroup { get; set; } = string.Empty;
        public double BestWeight { get; set; }
        public int BestReps { get; set; }
        public DateTime BestDate { get; set; }
        public List<PersonalRecordEntry> History { get; set; } = [];

        // FIX 5: Pre-computed color string — eliminates MuscleGroupColorConverter.
        public string MuscleGroupColor => MuscleGroup switch
        {
            "Chest" => "#4A90D9",
            "Back" => "#27AE60",
            "Legs" => "#E67E22",
            "Shoulders" => "#8E44AD",
            "Arms" => "#E74C3C",
            "Core" => "#5DADE2",
            _ => "#1F77F0"
        };

        // FIX 2: Observable IsExpanded — the UI updates automatically when this
        // changes, so ToggleExpanded no longer needs RemoveAt+Insert to force a
        // CollectionView refresh.
        [ObservableProperty]
        private bool _isExpanded;
    }

    public class PersonalRecordEntry
    {
        public double Weight { get; set; }
        public int Reps { get; set; }
        public DateTime Date { get; set; }
    }
}