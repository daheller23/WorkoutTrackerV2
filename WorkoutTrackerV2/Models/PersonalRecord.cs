using CommunityToolkit.Mvvm.ComponentModel;

namespace WorkoutTrackerV2.Models
{
    public partial class PersonalRecord : ObservableObject
    {
        [ObservableProperty]
        private bool _isExpanded;
        public int BestReps { get; set; }
        public int ExerciseId { get; set; }
        public double BestWeight { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public string MuscleGroup { get; set; } = string.Empty;    
        public DateTime BestDate { get; set; }
        public List<PersonalRecordEntry> History { get; set; } = [];

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
    }

    // ==============================================================================================================
    //
    //      PUBLIC CLASSES
    //
    // ==============================================================================================================
    public class PersonalRecordEntry
    {
        public int Reps { get; set; }
        public double Weight { get; set; }      
        public DateTime Date { get; set; }
    }
}