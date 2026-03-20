using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using WorkoutTrackerV2.Helpers;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(Exercise), "Exercise")]
    public partial class ExerciseProgressViewModel(ISettingsService settingsService) : BaseViewModel
    {
        [ObservableProperty] private ExerciseProgress? _exercise;
        [ObservableProperty] private LineChart? _chart;

        // Exposes the user's preferred weight unit so XAML can bind to it
        // instead of hardcoding "lbs" in the stats labels.
        public string WeightUnitLabel => settingsService.WeightUnit;

        partial void OnExerciseChanged(ExerciseProgress? value)
        {
            if (value is null || value.Points.Count == 0) return;
            // FIX: BuildChart inlined — it was a one-liner wrapper with no
            // other callers, so the extra method and indirection are removed.
            Chart = ChartHelper.BuildProgressChart(value.Points);
        }

        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
    }
}
