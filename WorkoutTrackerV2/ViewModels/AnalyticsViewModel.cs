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
        [ObservableProperty]
        private List<DailyStats> _dailyStats = [];

        [ObservableProperty]
        private ObservableCollection<ExerciseProgress> _topExercises = [];

        [ObservableProperty]
        private ObservableCollection<string> _muscleGroups = ["Chest", "Back", "Legs", "Shoulders", "Arms", "Core"];

        [ObservableProperty]
        private string _selectedMuscleGroup = string.Empty;

        [ObservableProperty]
        private int _selectedDays = 0;

        [ObservableProperty]
        private double _totalVolumeLifted = 0;

        [ObservableProperty]
        private double _averageVolume = 0;

        [ObservableProperty]
        private int _totalSets = 0;
        #endregion

        #region "LOAD ANALYTICS"
        [RelayCommand]
        private async Task LoadAnalytics()
        {
            try
            {
                IsLoading = true;

                var stats = await analyticsService.GetDailyStatsAsync(SelectedDays);
                DailyStats = stats;

                TotalVolumeLifted = stats.Sum(s => s.TotalWeightLifted);
                AverageVolume = stats.Count > 0 ? TotalVolumeLifted / stats.Count : 0;
                TotalSets = stats.Sum(s => s.SetsCompleted);

                var strengthProgress = await analyticsService.GetStrengthProgressAsync(SelectedDays);
                TopExercises.Clear();

                foreach (var kvp in strengthProgress.Take(5))
                {
                    var exercises = await workoutService.GetAllExercisesAsync();
                    var exercise = exercises.FirstOrDefault(e => e.Name == kvp.Key);
                    if (exercise != null)
                    {
                        var progress = await analyticsService.GetExerciseProgressAsync(exercise.Id, SelectedDays);
                        TopExercises.Add(progress);
                    }
                }

                if (!string.IsNullOrEmpty(SelectedMuscleGroup))
                {
                    await LoadMuscleGroupAnalytics();
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("LoadAnalytics Error", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region "LOAD MUSCLE GROUP ANALYTICS"
        private async Task LoadMuscleGroupAnalytics()
        {
            if (string.IsNullOrEmpty(SelectedMuscleGroup))
            {
                return;
            }
                
            try
            {
                var groupProgress = await analyticsService.GetProgressForMuscleGroupAsync(SelectedMuscleGroup, SelectedDays);
                TopExercises.Clear();
                foreach (var progress in groupProgress)
                {
                    TopExercises.Add(progress);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("LoadMuscleGroupAnalytics Error", ex.Message, "OK");
            }
        }
        #endregion


    }
}
