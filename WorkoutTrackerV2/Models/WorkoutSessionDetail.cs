namespace WorkoutTrackerV2.Models
{
    public class WorkoutSessionDetail
    {
        public WorkoutSession Session { get; set; } = new();
        public int SetCount { get; set; } = 0;
        public int TotalReps { get; set; } = 0;
        public double TotalWeight { get; set; } = 0;
        public List<WorkoutSet> Sets { get; set; } = [];
        public string MuscleGroup { get; set; } = string.Empty;
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
}