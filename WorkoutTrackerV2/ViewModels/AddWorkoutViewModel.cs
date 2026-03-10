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

        [ObservableProperty]
        private Exercise _selectedExercise;

        [ObservableProperty]
        private int _currentSetNumber = 1;

        [ObservableProperty]
        private int _currentReps = 0;

        [ObservableProperty]
        private double _currentWeight = 0;

        [ObservableProperty]
        private string _weightUnit = "lbs";

        [ObservableProperty]
        private ObservableCollection<WorkoutSet> _workoutExercises = [];

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

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync(Routes.Home);
        }

        [RelayCommand]
        private void AddSet()
        {
            if (SelectedExercise == null)
            {
                ErrorMessage = "Please select an exercise first";
                return;
            }

            WorkoutExercises.Add(new WorkoutSet
            {
                Exercise = SelectedExercise,    
                ExerciseId = SelectedExercise.Id, 
                SetNumber = CurrentSetNumber,
                Reps = CurrentReps,
                Weight = CurrentWeight,
                WeightUnit = WeightUnit
            });
            CurrentSetNumber++;
        }

        [RelayCommand]
        private void RemoveSet(WorkoutSet set)
        {
            WorkoutExercises.Remove(set);
            for (int i = 0; i < WorkoutExercises.Count; i++)
            {
                WorkoutExercises[i].SetNumber = i + 1;
            }
            CurrentSetNumber = WorkoutExercises.Count + 1;
        }

    }
}
