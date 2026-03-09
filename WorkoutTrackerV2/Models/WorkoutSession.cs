
using SQLite;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutSession
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string DayName { get; set; }
        public string Notes { get; set; }
        public TimeSpan Duration { get; set; }
        public int TotalExercises { get; set; }
    }
}
