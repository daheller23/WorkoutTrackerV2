namespace WorkoutTrackerV2.Services
{
    /// <summary>
    /// Persists settings via Preferences (backed by SharedPreferences on Android
    /// and NSUserDefaults on iOS). All values survive app restarts.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private const string KeyBodyWeight = "body_weight";
        private const string KeyDarkMode = "dark_mode";
        private const string KeyHeightCm = "height_cm";
        private const string KeyRmFormula = "rm_formula";
        private const string KeyWeightUnit = "weight_unit";

        public double BodyWeight
        {
            get => Preferences.Get(KeyBodyWeight, 0.0);
            set => Preferences.Set(KeyBodyWeight, value);
        }

        public double HeightCm
        {
            get => Preferences.Get(KeyHeightCm, 0.0);
            set => Preferences.Set(KeyHeightCm, value);
        }

        public bool IsDarkMode
        {
            get => Preferences.Get(KeyDarkMode, false);
            set => Preferences.Set(KeyDarkMode, value);
        }

        public string RmFormula
        {
            get => Preferences.Get(KeyRmFormula, "Epley");
            set => Preferences.Set(KeyRmFormula, value);
        }

        public string WeightUnit
        {
            get => Preferences.Get(KeyWeightUnit, "lbs");
            set => Preferences.Set(KeyWeightUnit, value);
        }
    }
}
