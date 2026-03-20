namespace WorkoutTrackerV2.Services
{
    public interface ISettingsService
    {
        string WeightUnit { get; set; }
        bool IsDarkMode { get; set; }

        // Height in centimetres — used by BodyWeightService for BMI calculation.
        // 0 means not set; the BMI card is hidden until this is populated.
        double HeightCm { get; set; }
    }
}