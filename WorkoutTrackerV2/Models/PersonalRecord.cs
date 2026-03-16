namespace WorkoutTrackerV2.Models
{
    public class PersonalRecord
    {
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public string MuscleGroup { get; set; } = string.Empty;
        public double BestWeight { get; set; }
        public int BestReps { get; set; }
        public DateTime BestDate { get; set; }
        public List<PersonalRecordEntry> History { get; set; } = [];
        public bool IsExpanded { get; set; }
    }

    public class PersonalRecordEntry
    {
        public double Weight { get; set; }
        public int Reps { get; set; }
        public DateTime Date { get; set; }
    }
}