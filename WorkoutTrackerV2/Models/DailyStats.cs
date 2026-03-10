
namespace WorkoutTrackerV2.Models
{
    public class DailyStats
    {
        public DateTime Date { get; set; }
        public int ExercisesCompleted { get; set; }
        public int SetsCompleted { get; set; }
        public int TotalRepsCompleted { get; set; }
        public double TotalWeightLifted { get; set; }
    }
}
