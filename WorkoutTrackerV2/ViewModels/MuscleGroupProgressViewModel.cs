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
    public partial class MuscleGroupProgressViewModel(
        IAnalyticsService analyticsService,
        ISettingsService settingsService) : BaseViewModel
    {
        #region "PRIVATE VARIABLES"
        private bool _isInitialized = false;
        private static readonly string[] ChartColors = [
            "#1F77F0", "#4CAF50", "#FF9800", "#FF6B6B",
            "#9C27B0", "#00BCD4", "#FF5722", "#607D8B"
        ];
        #endregion

        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private string _muscleGroup = string.Empty;
        [ObservableProperty] private ObservableCollection<ExerciseProgress> _exercises = [];
        [ObservableProperty] private LineChart? _combinedChart;
        [ObservableProperty] private int _selectedDays = 30;
        [ObservableProperty] private string _weightUnitLabel = "lbs";
        [ObservableProperty] private string _topExerciseName = string.Empty;
        [ObservableProperty] private int _totalSets;
        [ObservableProperty] private int _totalReps;
        [ObservableProperty] private double _maxWeight;
        #endregion

        #region "PARTIAL METHODS"
        partial void OnMuscleGroupChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _isInitialized = true;
                LoadDataCommand.Execute(null);
            }
        }

        partial void OnSelectedDaysChanged(int value)
        {
            if (_isInitialized)
                LoadDataCommand.Execute(null);
        }
        #endregion

        #region "SET TIME PERIOD"
        [RelayCommand]
        private void SetTimePeriod(string days)
        {
            if (int.TryParse(days, out int result))
                SelectedDays = result;
        }
        #endregion

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                WeightUnitLabel = settingsService.WeightUnit;

                var progress = await analyticsService.GetProgressForMuscleGroupAsync(MuscleGroup, SelectedDays);

                // Assign chart colors and improvement delta by index
                for (int i = 0; i < progress.Count; i++)
                {
                    progress[i].ChartColor = ChartColors[i % ChartColors.Length];

                    var diff = progress[i].LatestMaxWeight - progress[i].EarliestMaxWeight;
                    if (diff != 0 && progress[i].EarliestMaxWeight > 0)
                    {
                        var sign = diff > 0 ? "↑" : "↓";
                        progress[i].ImprovementLabel = $"{sign} {Math.Abs(diff):F0} {settingsService.WeightUnit}";
                        progress[i].HasImprovement = true;
                    }
                }

                // Set top exercise name for chart label
                var top = progress.OrderByDescending(e => e.MaxWeight).FirstOrDefault();
                TopExerciseName = top?.ExerciseName ?? string.Empty;

                Exercises = new ObservableCollection<ExerciseProgress>(progress);

                // Summary stats
                TotalSets = progress.Sum(e => e.Sets.Count);
                TotalReps = progress.Sum(e => e.TotalReps);
                MaxWeight = progress.Count > 0 ? progress.Max(e => e.MaxWeight) : 0;

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
            if (Exercises.Count == 0) return;

            var topExercise = Exercises
                .OrderByDescending(e => e.MaxWeight)
                .FirstOrDefault();

            if (topExercise?.Points.Count == 0) return;

            CombinedChart = ChartHelper.BuildProgressChart(topExercise!.Points);
        }
        #endregion
    }
}