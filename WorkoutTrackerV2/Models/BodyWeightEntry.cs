using SQLite;

namespace WorkoutTrackerV2.Models
{
    [Table("BodyWeightEntries")]
    public class BodyWeightEntry
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public double Weight { get; set; }
        public string Unit { get; set; } = "lbs";
        public DateTime Date { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;
    }
}
