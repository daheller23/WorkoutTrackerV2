using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutSet : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int ExerciseId { get; set; }

        [Indexed]
        public int WorkoutSessionId { get; set; }

        [Ignore] // SQLite will ignore this property
        public Exercise Exercise { get; set; }

        public int SetNumber { get; set; }
        public int Reps { get; set; }
        public double Weight { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string WeightUnit { get; set; } = "lbs";
    }
}
