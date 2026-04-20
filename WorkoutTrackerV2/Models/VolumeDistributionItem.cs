namespace WorkoutTrackerV2.Models
{
    public class VolumeDistributionItem
    {
        public int Sets { get; set; }
        public double Percentage { get; set; }
        public string ColorHex { get; set; } = "#1F77F0";
        public string DisplayPercentage => $"{Percentage:P0}";
        public string Name { get; set; } = string.Empty;  
    }
}
