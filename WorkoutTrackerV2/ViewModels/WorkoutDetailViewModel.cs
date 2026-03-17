using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(Session), "Session")]
    public partial class WorkoutDetailViewModel(IWorkoutService workoutService, ISettingsService settingsService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private WorkoutSession _session = new();
        [ObservableProperty] private ObservableCollection<ExerciseGroup> _exerciseGroups = [];
        [ObservableProperty] private int _totalSets;
        [ObservableProperty] private double _totalVolume;
        [ObservableProperty] private int _totalReps;
        [ObservableProperty] private string _weightUnitLabel = "lbs";
        #endregion

        #region "LOAD SETS"
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading || Session?.Id == 0) return;
            try
            {
                IsLoading = true;

                // Reload session from DB to get latest TotalExercises and other fields
                var freshSession = await workoutService.GetSessionAsync(Session.Id);
                if (freshSession is not null)
                    Session = freshSession;

                var setsTask = workoutService.GetSetsForSessionAsync(Session.Id);
                var exercisesTask = workoutService.GetAllExercisesAsync();
                await Task.WhenAll(setsTask, exercisesTask);

                var sets = setsTask.Result;
                var exerciseDict = exercisesTask.Result.ToDictionary(e => e.Id);

                ExerciseGroups.Clear();
                foreach (var set in sets)
                {
                    if (!exerciseDict.TryGetValue(set.ExerciseId, out var exercise)) continue;
                    set.Exercise = exercise;
                    var existing = ExerciseGroups.FirstOrDefault(g => g.Exercise.Id == set.ExerciseId);
                    if (existing is not null)
                        existing.Sets.Add(set);
                    else
                    {
                        var group = new ExerciseGroup(set.Exercise);
                        group.Sets.Add(set);
                        ExerciseGroups.Add(group);
                    }
                }

                TotalSets = ExerciseGroups.Sum(g => g.Sets.Count);
                TotalVolume = ExerciseGroups.SelectMany(g => g.Sets).Sum(s => s.Weight * s.Reps);
                TotalReps = ExerciseGroups.SelectMany(g => g.Sets).Sum(s => s.Reps);
                WeightUnitLabel = settingsService.WeightUnit;
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

        #region "EDIT WORKOUT"
        [RelayCommand]
        private async Task EditWorkout()
        {
            await Shell.Current.GoToAsync(Routes.EditWorkout, new Dictionary<string, object>
            {
                { "Session", Session }
            });
        }
        #endregion

        #region "DELETE WORKOUT"
        [RelayCommand]
        private async Task DeleteWorkout()
        {
            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Delete Workout",
                $"Are you sure you want to delete '{Session.DayName}'?",
                "Yes", "No");

            if (!confirmed) return;

            bool doubleConfirmed = await Shell.Current.DisplayAlertAsync(
                "Are you sure?",
                "This cannot be undone.",
                "Yes, delete", "Cancel");

            if (!doubleConfirmed) return;

            try
            {
                IsLoading = true;
                var sets = await workoutService.GetSetsForSessionAsync(Session.Id);
                foreach (var set in sets)
                    await workoutService.DeleteSetAsync(set.Id);
                await workoutService.DeleteSessionAsync(Session);
                await Shell.Current.GoToAsync(Routes.Back);
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
    }
}