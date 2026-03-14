using SQLite;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutSet
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; } = 0;

        [Indexed]
        public int ExerciseId { get; set; } = 0;

        [Indexed]
        public int WorkoutSessionId { get; set; } = 0;

        [Ignore] // SQLite will ignore this property
        public Exercise Exercise { get; set; } = new();

        public int SetNumber { get; set; } = 0;
        public int Reps { get; set; } = 0;
        public double Weight { get; set; } = 0;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string WeightUnit { get; set; } = "lbs";
    }
}
