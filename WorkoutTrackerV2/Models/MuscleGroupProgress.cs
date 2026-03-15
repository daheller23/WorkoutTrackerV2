namespace WorkoutTrackerV2.Models
{
    public class MuscleGroupProgress
    {
        public string MuscleGroup { get; set; } = string.Empty;
        public List<ExerciseProgress> Exercises { get; set; } = [];
        public double LatestMaxWeight { get; set; }
        public double EarliestMaxWeight { get; set; }
        public bool IsTrending => LatestMaxWeight >= EarliestMaxWeight;
    }
}