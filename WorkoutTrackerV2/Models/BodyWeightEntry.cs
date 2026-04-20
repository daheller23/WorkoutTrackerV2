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
        public string Notes { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;    
    }
}
