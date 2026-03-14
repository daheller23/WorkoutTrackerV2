namespace WorkoutTrackerV2.Models
{
    public class WorkoutSessionDetail
    {
        public WorkoutSession Session { get; set; } = new();
        public int SetCount { get; set; } = 0;
        public int TotalReps { get; set; } = 0;
        public double TotalWeight { get; set; } = 0;
        public List<WorkoutSet> Sets { get; set; } = [];
    }
}
