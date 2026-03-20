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
        // FIX 5: NotifyPropertyChangedFor drives IsLbsSelected and IsKgSelected,
        // which replace WeightUnitColorConverter and WeightUnitTextColorConverter
        // in the XAML (4 converter calls per tap → 0).
        [NotifyPropertyChangedFor(nameof(IsLbsSelected))]
        [NotifyPropertyChangedFor(nameof(IsKgSelected))]
        private string _weightUnit = "lbs";

        [ObservableProperty] private bool _isDarkMode;

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
        #endregion

        #region "PARTIAL METHODS"
        partial void OnWeightUnitChanged(string value)
        {
            settingsService.WeightUnit = value;
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
        }
        #endregion

        #region "SET WEIGHT UNIT"
        [RelayCommand]
        private void SetWeightUnit(string unit) => WeightUnit = unit;
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

        #region "GO BACK"
        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
        #endregion
    }
}
