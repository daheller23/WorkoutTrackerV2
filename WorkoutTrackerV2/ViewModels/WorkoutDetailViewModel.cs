using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(Session), "Session")]
    public partial class WorkoutDetailViewModel : BaseViewModel
    {
        private readonly IWorkoutService _workoutService;

        [ObservableProperty]
        private WorkoutSession _session;

        [ObservableProperty]
        private ObservableCollection<WorkoutSet> _sets = [];

        public WorkoutDetailViewModel(IWorkoutService workoutService)
        {
            _workoutService = workoutService;
        }

        partial void OnSessionChanged(WorkoutSession value)
        {
            if (value != null)
                LoadSetsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadSets()
        {
            try
            {
                IsLoading = true;
                var sets = await _workoutService.GetSetsForSessionAsync(Session.Id);
                Sets.Clear();
                foreach (var set in sets)
                {
                    set.Exercise = await _workoutService.GetExerciseAsync(set.ExerciseId);
                    Sets.Add(set);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        //[RelayCommand]
        //private async Task EditWorkout()
        //{
        //    await Shell.Current.GoToAsync(Routes.EditWorkout, new Dictionary<string, object>
        //    {
        //        { "Session", Session }
        //    });
        //}
    }
}