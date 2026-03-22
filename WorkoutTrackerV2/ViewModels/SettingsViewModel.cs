using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class SettingsViewModel(
        ISettingsService settingsService,
        IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLbsSelected))]
        [NotifyPropertyChangedFor(nameof(IsKgSelected))]
        private string _weightUnit = "lbs";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEpleySelected))]
        [NotifyPropertyChangedFor(nameof(IsBrzyckiSelected))]
        private string _rmFormula = "Epley";

        [ObservableProperty] private bool _isDarkMode;
        [ObservableProperty] private string _heightCm = string.Empty;

        // FIX 4: AppVersion computed once as a static field — AppInfo values are
        // compile-time constants that never change, no reason to recompute on
        // every LoadSettings call.
        public static string AppVersion { get; } =
            $"Version {AppInfo.VersionString} ({AppInfo.BuildString})";
        #endregion

        #region "COMPUTED PROPERTIES"
        // FIX 5: Simple bool properties — XAML DataTriggers bind to these instead
        // of running WeightUnitColorConverter and WeightUnitTextColorConverter.
        public bool IsLbsSelected => WeightUnit == "lbs";
        public bool IsKgSelected => WeightUnit == "kg";
        public bool IsEpleySelected => RmFormula == "Epley";
        public bool IsBrzyckiSelected => RmFormula == "Brzycki";
        #endregion

        #region "PARTIAL METHODS"
        partial void OnWeightUnitChanged(string value)
        {
            settingsService.WeightUnit = value;
        }

        partial void OnRmFormulaChanged(string value)
        {
            settingsService.RmFormula = value;
        }

        partial void OnIsDarkModeChanged(bool value)
        {
            settingsService.IsDarkMode = value;
            Application.Current!.UserAppTheme = value ? AppTheme.Dark : AppTheme.Light;
        }
        #endregion

        #region "LOAD SETTINGS"
        // Synchronous — no async work needed. Execute() from code-behind is correct.
        [RelayCommand]
        private void LoadSettings()
        {
            WeightUnit = settingsService.WeightUnit;
            IsDarkMode = settingsService.IsDarkMode;
            RmFormula = settingsService.RmFormula;
            HeightCm = settingsService.HeightCm > 0
                ? settingsService.HeightCm.ToString("F0")
                : string.Empty;
        }
        #endregion

        #region "SET WEIGHT UNIT"
        [RelayCommand]
        private void SetWeightUnit(string unit) => WeightUnit = unit;
        #endregion

        #region "SET RM FORMULA"
        [RelayCommand]
        private void SetRmFormula(string formula) => RmFormula = formula;
        #endregion

        #region "SAVE HEIGHT"
        [RelayCommand]
        private void SaveHeight()
        {
            if (double.TryParse(HeightCm, out double cm) && cm > 0)
                settingsService.HeightCm = cm;
        }
        #endregion

        #region "CLEAR ALL DATA"
        [RelayCommand]
        private async Task ClearAllData()
        {
            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Clear All Data",
                "This will permanently delete all your workouts, sets and history. This cannot be undone. Are you sure?",
                "Yes, delete everything", "Cancel");
            if (!confirmed) return;

            bool doubleConfirmed = await Shell.Current.DisplayAlertAsync(
                "Are you absolutely sure?",
                "All your workout data will be lost forever.",
                "Yes, I'm sure", "Cancel");
            if (!doubleConfirmed) return;

            try
            {
                IsLoading = true;
                await workoutService.ClearAllDataAsync();
                await Shell.Current.DisplayAlertAsync("Done", "All data has been cleared.", "OK");
                await Shell.Current.GoToAsync(Routes.Home);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region "REQUEST BATTERY OPTIMISATION EXEMPTION"
        // Opens the system dialog asking the user to disable battery optimisation
        // for this app. Required on Android for the rest timer notification to fire
        // reliably when the screen is off. No-op on iOS.
        [RelayCommand]
        private async Task RequestBatteryExemption()
        {
#if ANDROID
            if (!OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Not Required",
                    "Battery optimisation exemption is not needed on this Android version.",
                    "OK");
                return;
            }

            var context = Android.App.Application.Context;
            var pm = (Android.OS.PowerManager?)context.GetSystemService(
                Android.Content.Context.PowerService);

            if (pm is not null &&
                pm.IsIgnoringBatteryOptimizations(context.PackageName))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Already Enabled",
                    "Battery optimisation is already disabled for this app. The rest timer should work in the background.",
                    "OK");
                return;
            }

            var intent = new Android.Content.Intent(
                Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse(
                $"package:{context.PackageName}"));
            Platform.CurrentActivity?.StartActivity(intent);
#else
            await Shell.Current.DisplayAlertAsync(
                "Not Required",
                "This setting is only needed on Android. On iOS, notifications work automatically.",
                "OK");
#endif
        }
        #endregion

        #region "GO BACK"
        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
        #endregion
    }
}
