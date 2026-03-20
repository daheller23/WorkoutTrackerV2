namespace WorkoutTrackerV2.Services
{
    /// <summary>
    /// Persists settings via Preferences (backed by SharedPreferences on Android
    /// and NSUserDefaults on iOS). All values survive app restarts.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private const string KeyWeightUnit = "weight_unit";
        private const string KeyDarkMode = "dark_mode";
        private const string KeyHeightCm = "height_cm";

        public string WeightUnit
        {
            get => Preferences.Get(KeyWeightUnit, "lbs");
            set => Preferences.Set(KeyWeightUnit, value);
        }

        public bool IsDarkMode
        {
            get => Preferences.Get(KeyDarkMode, false);
            set => Preferences.Set(KeyDarkMode, value);
        }

        public double HeightCm
        {
            get => Preferences.Get(KeyHeightCm, 0.0);
            set => Preferences.Set(KeyHeightCm, value);
        }
    }
}
