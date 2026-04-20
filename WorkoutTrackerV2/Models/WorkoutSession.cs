using SQLite;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutSession
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } = 0;
        public int TotalExercises { get; set; } = 0;
        public string DayName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;    
    }
}
