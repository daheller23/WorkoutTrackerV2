using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(Session), "Session")]
    public partial class WorkoutDetailViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private WorkoutSession _session = new();
        [ObservableProperty] private ObservableCollection<ExerciseGroup> _exerciseGroups = [];
        [ObservableProperty] private int _totalSets;
        [ObservableProperty] private double _totalVolume;
        #endregion

        #region "LOAD SETS"
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading || Session?.Id == 0) return;
            try
            {
                IsLoading = true;

                // Reload session from DB to get latest data
                var sets = await workoutService.GetSetsForSessionAsync(Session.Id);
                ExerciseGroups.Clear();

                foreach (var set in sets)
                {
                    set.Exercise = await workoutService.GetExerciseAsync(set.ExerciseId);
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
    }
}