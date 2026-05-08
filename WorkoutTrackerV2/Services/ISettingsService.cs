namespace WorkoutTrackerV2.Services
{
    public interface ISettingsService
    {
        string RmFormula { get; set; } // 1RM "Epley" (default) or "Brzycki"
        string WeightUnit { get; set; } // "lbs" or "kg"
        bool IsDarkMode { get; set; }
        double BodyWeight { get; set; }
        double HeightCm { get; set; }   
    }
}