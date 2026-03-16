using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class AnalyticsViewModel(IWorkoutService workoutService, IAnalyticsService analyticsService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private List<DailyStats> _dailyStats = [];
        [ObservableProperty] private ObservableCollection<ExerciseProgress> _topExercises = [];
        [ObservableProperty] private ObservableCollection<MuscleGroupProgress> _muscleGroupProgress = [];
        [ObservableProperty] private int _selectedDays = 30;
        [ObservableProperty] private double _totalVolumeLifted;
        [ObservableProperty] private double _averageVolume;
        [ObservableProperty] private int _totalSets;
        [ObservableProperty] private Dictionary<DateTime, double> _heatmapData = [];
        [ObservableProperty] private DateTime _heatmapMonth = DateTime.Today;
        [ObservableProperty] private string _heatmapTitle = DateTime.Today.ToString("MMMM yyyy");
        [ObservableProperty] private List<double> _volumeSparkline = [];
        [ObservableProperty] private List<double> _setsSparkline = [];
        [ObservableProperty] private List<double> _daysSparkline = [];
        [ObservableProperty] private List<double> _avgVolumeSparkline = [];
        #endregion

        #region "PARTIAL METHODS"
        partial void OnSelectedDaysChanged(int value) => LoadAnalyticsCommand.Execute(null);
        #endregion

        #region "SET TIME PERIOD"
        [RelayCommand]
        private void SetTimePeriod(string days)
        {
            if (int.TryParse(days, out int result))
            {
                SelectedDays = result;
            }             
        }
        #endregion

        #region "VIEW PERSONAL RECORDS"
        [RelayCommand]
        private static Task ViewPersonalRecords() => Shell.Current.GoToAsync(Routes.PersonalRecords);
        #endregion

        #region "LOAD ANALYTICS"
        [RelayCommand]
        private async Task LoadAnalytics()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;

                // Run independent calls in parallel
                var statsTask = analyticsService.GetDailyStatsAsync(SelectedDays);
                var strengthTask = analyticsService.GetStrengthProgressAsync(SelectedDays);
                var muscleTask = analyticsService.GetMuscleGroupProgressAsync(SelectedDays);
                var exercisesTask = workoutService.GetAllExercisesAsync();

                await Task.WhenAll(statsTask, strengthTask, muscleTask, exercisesTask);

                // Summary stats
                var stats = statsTask.Result;
                DailyStats = stats;
                TotalVolumeLifted = stats.Sum(s => s.TotalWeightLifted);
                AverageVolume = stats.Count > 0 ? TotalVolumeLifted / stats.Count : 0;
                TotalSets = stats.Sum(s => s.SetsCompleted);

                // Heatmap data
                var heatmap = new Dictionary<DateTime, double>();
                foreach (var stat in stats)
                {
                    heatmap[stat.Date] = stat.TotalWeightLifted;
                }                  
                HeatmapData = heatmap;
                HeatmapTitle = DateTime.Today.ToString("MMMM yyyy");

                // Sparkline Data
                var orderedStats = stats.OrderBy(s => s.Date).ToList();
                VolumeSparkline = orderedStats.Select(s => s.TotalWeightLifted).ToList();
                SetsSparkline = orderedStats.Select(s => (double)s.SetsCompleted).ToList();
                DaysSparkline = orderedStats.Select((s, i) => (double)(i + 1)).ToList();
                AvgVolumeSparkline = orderedStats.Select(s => s.TotalWeightLifted).ToList();

                // Top exercises — fetch all progress in parallel
                var exercises = exercisesTask.Result;
                var topFive = strengthTask.Result.Take(5).ToList();
                var progressTasks = topFive
                    .Select(kvp => exercises.FirstOrDefault(e => e.Name == kvp.Key))
                    .Where(e => e is not null)
                    .Select(e => analyticsService.GetExerciseProgressAsync(e!.Id, SelectedDays))
                    .ToList();

                var topExercises = await Task.WhenAll(progressTasks);
                TopExercises = new ObservableCollection<ExerciseProgress>(topExercises);

                // Muscle group progress
                MuscleGroupProgress = new ObservableCollection<MuscleGroupProgress>(muscleTask.Result);
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

        #region "SELECT MUSCLE GROUP PROGRESS"
        [RelayCommand]
        private async Task SelectMuscleGroupProgress(MuscleGroupProgress group)
        {
            await Shell.Current.GoToAsync(Routes.MuscleGroupProgress, new Dictionary<string, object>
            {
                { "MuscleGroup", group.MuscleGroup }
            });
        }
        #endregion
    }
}