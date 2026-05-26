using WorkoutTrackerV2.Helpers;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutSessionDetail
    {
        public int SetCount { get; set; } = 0;
        public int TotalReps { get; set; } = 0;
        public double TotalWeight { get; set; } = 0;
        public string MuscleGroup { get; set; } = string.Empty;
        public string MuscleGroupColor => ColorHelper.GetMuscleGroupColor(MuscleGroup);
        public WorkoutSession Session { get; set; } = new();
        public List<WorkoutSet> Sets { get; set; } = [];
    }
}