namespace WorkoutTrackerV2.Models
{
    public class HomeDashboardSummary
    {
        public int CurrentStreak { get; init; }
        public int SetsThisWeek { get; init; }
        public int TotalWorkouts { get; init; }
        public int WorkoutsThisWeek { get; init; }
        public double AverageDuration { get; init; }
        public double VolumeThisWeek { get; init; }
        public string TopMuscleGroup { get; init; } = string.Empty;
        public DateTime? LastWorkoutDate { get; init; }
        public WorkoutSession? LastWorkoutSession { get; init; }
        public List<WorkoutSession> RecentSessions { get; init; } = [];
    }
}
