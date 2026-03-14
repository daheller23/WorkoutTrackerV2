namespace WorkoutTrackerV2.Models
{
    public class ExerciseProgress
    {
        public string ExerciseName { get; set; } = string.Empty;
        public List<WorkoutSet> Sets { get; set; } = [];
        public double MaxWeight { get; set; } = 0;
        public double AverageWeight { get; set; } = 0;
        public int TotalReps { get; set; } = 0;
    }
}
