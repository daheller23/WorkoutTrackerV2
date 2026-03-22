namespace WorkoutTrackerV2.Services
{
    public interface ISettingsService
    {
        string WeightUnit { get; set; }   // "lbs" or "kg"
        bool IsDarkMode { get; set; }

        // Height in centimetres — used by BodyWeightService for BMI calculation.
        // 0 means not set; the BMI card is hidden until this is populated.
        double HeightCm { get; set; }

        // 1RM formula preference — "Epley" (default) or "Brzycki".
        string RmFormula { get; set; }
    }
}