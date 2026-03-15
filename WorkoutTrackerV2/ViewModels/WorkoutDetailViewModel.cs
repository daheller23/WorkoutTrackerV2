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
        #endregion

        #region "ON SESSION CHANGED"
        partial void OnSessionChanged(WorkoutSession value)
        {
            if (value is not null)
            {
                LoadSetsCommand.Execute(null);
            }             
        }
        #endregion

        #region "LOAD SETS"
        [RelayCommand]
        private async Task LoadSets()
        {
            if (IsLoading)
            {
                return;
            }

            try
            {
                IsLoading = true;
                ExerciseGroups.Clear();

                var sets = await workoutService.GetSetsForSessionAsync(Session.Id);
                foreach (var set in sets)
                {
                    set.Exercise = await workoutService.GetExerciseAsync(set.ExerciseId);
                    var existing = ExerciseGroups.FirstOrDefault(g => g.Exercise.Id == set.ExerciseId);
                    if (existing is not null)
                    {
                        existing.Sets.Add(set);
                    }
                    else
                    {
                        var group = new ExerciseGroup(set.Exercise);
                        group.Sets.Add(set);
                        ExerciseGroups.Add(group);
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("LoadSets Error", ex.Message, "OK");
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