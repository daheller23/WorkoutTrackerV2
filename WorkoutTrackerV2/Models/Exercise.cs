using SQLite;

namespace WorkoutTrackerV2.Models
{
    public class Exercise
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string MuscleGroup { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
