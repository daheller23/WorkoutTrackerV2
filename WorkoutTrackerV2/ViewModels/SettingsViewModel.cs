using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class SettingsViewModel(ISettingsService settingsService, IWorkoutService workoutService) : BaseViewModel
    {
        [ObservableProperty] private string _bodyWeight = string.Empty;
        [ObservableProperty] private string _heightCm = string.Empty;
        [ObservableProperty] private bool _isDarkMode;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEpleySelected))]
        [NotifyPropertyChangedFor(nameof(IsBrzyckiSelected))]
        private string _rmFormula = "Epley";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLbsSelected))]
        [NotifyPropertyChangedFor(nameof(IsKgSelected))]
        private string _weightUnit = "lbs";

        public bool IsBrzyckiSelected => RmFormula == "Brzycki";
        public bool IsEpleySelected => RmFormula == "Epley";
        public bool IsKgSelected => WeightUnit == "kg";
        public bool IsLbsSelected => WeightUnit == "lbs";

        public static string AppVersion { get; } = $"Version {AppInfo.VersionString} ({AppInfo.BuildString})";

        // ==============================================================================================================
        //
        //      PARTIAL METHODS
        //
        // ==============================================================================================================

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

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private void LoadSettings()
        {
            BodyWeight = settingsService.BodyWeight > 0
                ? settingsService.BodyWeight.ToString("F1") // F1 allows for decimals (e.g., 185.5)
                : string.Empty;
            HeightCm = settingsService.HeightCm > 0
                ? settingsService.HeightCm.ToString("F0")
                : string.Empty;
            IsDarkMode = settingsService.IsDarkMode;          
            RmFormula = settingsService.RmFormula;
            WeightUnit = settingsService.WeightUnit;
        }

        [RelayCommand]
        private void SetWeightUnit(string unit) => WeightUnit = unit;

        [RelayCommand]
        private void SetRmFormula(string formula) => RmFormula = formula;

        [RelayCommand]
        private void SaveHeight()
        {
            if (double.TryParse(HeightCm, out double cm) && cm > 0)
            {
                settingsService.HeightCm = cm;
            }           
        }

        [RelayCommand]
        private void SaveBodyWeight()
        {
            if (double.TryParse(BodyWeight, out double weight) && weight > 0)
            {
                settingsService.BodyWeight = weight;
            }
        }

        [RelayCommand]
        private async Task ClearAllData()
        {
            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Clear All Data",
                "This will permanently delete all your workouts, sets and history. This cannot be undone. Are you sure?",
                "Yes, delete everything", "Cancel");
            if (!confirmed)
            {
                return;
            }

            bool doubleConfirmed = await Shell.Current.DisplayAlertAsync(
                "Are you absolutely sure?",
                "All your workout data will be lost forever.",
                "Yes, I'm sure", "Cancel");
            if (!doubleConfirmed)
            {
                return;
            }

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
            var pm = (Android.OS.PowerManager?)context.GetSystemService(Android.Content.Context.PowerService);

            if (pm is not null && pm.IsIgnoringBatteryOptimizations(context.PackageName))
            {
                await Shell.Current.DisplayAlertAsync(
                    "Already Enabled",
                    "Battery optimisation is already disabled for this app. The rest timer should work in the background.",
                    "OK");
                return;
            }

            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse($"package:{context.PackageName}"));
            Platform.CurrentActivity?.StartActivity(intent);
#else
            await Shell.Current.DisplayAlertAsync(
                "Not Required",
                "This setting is only needed on Android. On iOS, notifications work automatically.",
                "OK");
#endif
        }

        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
    }
}
