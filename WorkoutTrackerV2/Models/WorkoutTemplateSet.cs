using SQLite;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutTemplateSet
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public int TemplateId { get; set; }
        [Indexed]
        public int ExerciseId { get; set; }
        [Ignore]
        public Exercise Exercise { get; set; } = new();
        public int SetNumber { get; set; }
        public int Reps { get; set; }
        public double Weight { get; set; }
        public string WeightUnit { get; set; } = "lbs";
    }
}