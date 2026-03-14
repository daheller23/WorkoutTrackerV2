using SQLite;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutSession
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } = 0;
        public DateTime Date { get; set; } = DateTime.Today;
        public string DayName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public int TotalExercises { get; set; } = 0;
    }
}
