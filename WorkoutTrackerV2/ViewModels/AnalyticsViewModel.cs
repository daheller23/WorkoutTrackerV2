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
        #endregion

        #region "PARTIAL METHODS"
        partial void OnSelectedDaysChanged(int value) => LoadAnalyticsCommand.Execute(null);
        #endregion

        #region "LOAD ANALYTICS"
        [RelayCommand]
        private async Task LoadAnalytics()
        {
            try
            {
                IsLoading = true;

                // Summary stats
                var stats = await analyticsService.GetDailyStatsAsync(SelectedDays);
                DailyStats = stats;
                TotalVolumeLifted = stats.Sum(s => s.TotalWeightLifted);
                AverageVolume = stats.Count > 0 ? TotalVolumeLifted / stats.Count : 0;
                TotalSets = stats.Sum(s => s.SetsCompleted);

                // Top exercises
                var strengthProgress = await analyticsService.GetStrengthProgressAsync(SelectedDays);
                TopExercises.Clear();
                var exercises = await workoutService.GetAllExercisesAsync();
                foreach (var kvp in strengthProgress.Take(5))
                {
                    var exercise = exercises.FirstOrDefault(e => e.Name == kvp.Key);
                    if (exercise is not null)
                    {
                        var progress = await analyticsService.GetExerciseProgressAsync(exercise.Id, SelectedDays);
                        TopExercises.Add(progress);
                    }
                }

                // Muscle group progress
                var muscleProgress = await analyticsService.GetMuscleGroupProgressAsync(SelectedDays);
                MuscleGroupProgress = new ObservableCollection<MuscleGroupProgress>(muscleProgress);
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

        #region "COMMANDS"
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