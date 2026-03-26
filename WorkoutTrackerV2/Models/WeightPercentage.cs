namespace WorkoutTrackerV2.Models
{
    public class WeightPercentage
    {
        public int Percent { get; set; }
        public double Weight { get; set; }
        public string Label => $"{Percent}%";
    }
}