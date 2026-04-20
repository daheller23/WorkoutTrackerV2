namespace WorkoutTrackerV2.Models
{
    public class MuscleGroupProgress
    {
        public double EarliestMaxWeight { get; set; }
        public double LatestMaxWeight { get; set; }
        public string MuscleGroup { get; set; } = string.Empty;
        public bool IsTrending => LatestMaxWeight >= EarliestMaxWeight;
        public List<ExerciseProgress> Exercises { get; set; } = [];   
    }
}