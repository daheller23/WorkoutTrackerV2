
namespace WorkoutTrackerV2.Models
{
    public class ExerciseProgress
    {
        public string ExerciseName { get; set; }
        public List<WorkoutSet> Sets { get; set; } = new();
        public double MaxWeight { get; set; }
        public double AverageWeight { get; set; }
        public int TotalReps { get; set; }
    }
}
