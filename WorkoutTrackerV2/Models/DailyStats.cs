namespace WorkoutTrackerV2.Models
{
    public class DailyStats
    {    
        public int ExercisesCompleted { get; set; } = 0;
        public int SetsCompleted { get; set; } = 0;
        public int TotalRepsCompleted { get; set; } = 0;
        public double TotalWeightLifted { get; set; } = 0;
        public DateTime Date { get; set; } = DateTime.Today;
    }
}
