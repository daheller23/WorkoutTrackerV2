
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class AddWorkoutViewModel : BaseViewModel
    {
        private readonly IWorkoutService _workoutService;

        [ObservableProperty]
        private ObservableCollection<Exercise> allExercises = [];


        public AddWorkoutViewModel(IWorkoutService workoutService)
        {
            _workoutService = workoutService;
        }

        [RelayCommand]
        private async Task LoadExercises()
        {
            try
            {
                IsLoading = true;
                var exercises = await _workoutService.GetAllExercisesAsync();

                AllExercises.Clear();
                foreach (var exercise in exercises)
                {
                    AllExercises.Add(exercise);
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


    }
}
