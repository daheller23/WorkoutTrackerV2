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
        [ObservableProperty]
        private WorkoutSession _session;
        partial void OnSessionChanged(WorkoutSession value)
        {
            if (value != null)
            {
                LoadSetsCommand.Execute(null);
            }
        }

        [ObservableProperty]
        private ObservableCollection<WorkoutSet> _sets = [];
        #endregion

        #region "LOAD SETS"
        [RelayCommand]
        private async Task LoadSets()
        {
            try
            {
                IsLoading = true;
                Sets.Clear();
                var sets = await workoutService.GetSetsForSessionAsync(Session.Id);
                foreach (var set in sets)
                {
                    set.Exercise = await workoutService.GetExerciseAsync(set.ExerciseId);
                    Sets.Add(set);
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
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync(Routes.Back);
        }
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