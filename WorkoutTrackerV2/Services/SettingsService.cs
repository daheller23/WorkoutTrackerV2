namespace WorkoutTrackerV2.Services
{
    public class SettingsService : ISettingsService
    {
        private const string WeightUnitKey = "weight_unit";
        private const string DarkModeKey = "dark_mode";

        public string WeightUnit
        {
            get => Preferences.Get(WeightUnitKey, "lbs");
            set => Preferences.Set(WeightUnitKey, value);
        }

        public bool IsDarkMode
        {
            get => Preferences.Get(DarkModeKey, false);
            set => Preferences.Set(DarkModeKey, value);
        }
    }
}