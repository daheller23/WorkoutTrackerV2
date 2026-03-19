namespace WorkoutTrackerV2.Models
{
    public class ExerciseProgress
    {
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public string MuscleGroup { get; set; } = string.Empty;
        public List<WorkoutSet> Sets { get; set; } = [];
        public double MaxWeight { get; set; }
        public double AverageWeight { get; set; }
        public int TotalReps { get; set; }
        public List<ProgressPoint> Points { get; set; } = [];
        public double EarliestMaxWeight { get; set; }
        public double LatestMaxWeight { get; set; }
        public bool IsTrending => LatestMaxWeight >= EarliestMaxWeight;
        public string ChartColor { get; set; } = "#1F77F0";
        public string ImprovementColor { get; set; } = "#4CAF50";
        public string ImprovementLabel { get; set; } = string.Empty;
        public bool HasImprovement { get; set; }
    }

    public class ProgressPoint
    {
        public DateTime Date { get; set; }
        public double MaxWeight { get; set; }
    }
}