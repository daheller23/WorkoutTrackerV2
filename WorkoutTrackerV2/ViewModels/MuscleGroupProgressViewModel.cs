using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Helpers;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(MuscleGroup), "MuscleGroup")]
    public partial class MuscleGroupProgressViewModel(IAnalyticsService analyticsService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private string _muscleGroup = string.Empty;
        [ObservableProperty] private ObservableCollection<ExerciseProgress> _exercises = [];
        [ObservableProperty] private LineChart? _combinedChart;
        [ObservableProperty] private int _selectedDays = 30;
        #endregion

        #region "ON MUSCLE GROUP CHANGED"
        partial void OnMuscleGroupChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                LoadDataCommand.Execute(null);
            }             
        }
        #endregion

        #region "ON SELECTED DAYS CHANGED"
        partial void OnSelectedDaysChanged(int value) => LoadDataCommand.Execute(null);
        #endregion

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading)
            {
                return;
            }

            try
            {
                IsLoading = true;
                var progress = await analyticsService.GetProgressForMuscleGroupAsync(MuscleGroup, SelectedDays);
                Exercises = new ObservableCollection<ExerciseProgress>(progress);
                BuildCombinedChart();
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

        #region "SELECT EXERCISE"
        [RelayCommand]
        private static async Task SelectExercise(ExerciseProgress exercise)
        {
            await Shell.Current.GoToAsync(Routes.ExerciseProgress, new Dictionary<string, object>
            {
                { "Exercise", exercise }
            });
        }
        #endregion

        #region "GO BACK"
        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
        #endregion

        #region "BUILD COMBINED CHART"
        private void BuildCombinedChart()
        {
            var allPoints = Exercises
                .SelectMany(e => e.Points)
                .GroupBy(p => p.Date.Date)
                .Select(g => new ProgressPoint
                {
                    Date = g.Key,
                    MaxWeight = g.Max(p => p.MaxWeight)
                })
                .OrderBy(p => p.Date)
                .ToList();

            if (allPoints.Count == 0) return;
            CombinedChart = ChartHelper.BuildProgressChart(allPoints);
        }
        #endregion
    }
}