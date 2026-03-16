namespace WorkoutTrackerV2.Services
{
    public interface ISettingsService
    {
        string WeightUnit { get; set; }
        bool IsDarkMode { get; set; }
    }
}