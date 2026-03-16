using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using WorkoutTrackerV2.Helpers;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(Exercise), "Exercise")]
    public partial class ExerciseProgressViewModel : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ExerciseProgress? _exercise;
        [ObservableProperty] private LineChart? _chart;
        #endregion

        #region "ON EXERCISE CHANGED"
        partial void OnExerciseChanged(ExerciseProgress? value)
        {
            if (value is null)
            {
                return;
            }
            BuildChart(value);
        }
        #endregion

        #region "GO BACK"
        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
        #endregion

        #region "BUILD CHART"
        private void BuildChart(ExerciseProgress exercise)
        {
            if (exercise.Points.Count == 0) return;
            Chart = ChartHelper.BuildProgressChart(exercise.Points);
        }
        #endregion
    }
}