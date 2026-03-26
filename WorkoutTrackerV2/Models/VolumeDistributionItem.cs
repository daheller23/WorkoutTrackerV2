namespace WorkoutTrackerV2.Models
{
    public class VolumeDistributionItem
    {
        public string Name { get; set; } = string.Empty;
        public int Sets { get; set; }
        public double Percentage { get; set; } // 0.0 to 1.0 for the progress bar
        public string DisplayPercentage => $"{Percentage:P0}"; // "45%"
        public string ColorHex { get; set; } = "#1F77F0";
    }
}
