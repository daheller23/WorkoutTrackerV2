using CommunityToolkit.Mvvm.ComponentModel;
using WorkoutTrackerV2.Helpers; // Added this using statement

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
        public string MuscleGroupColor => ColorHelper.GetMuscleGroupColor(MuscleGroup);
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