using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class CreateExerciseViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private string _exerciseName = string.Empty;
        [ObservableProperty] private string _selectedMuscleGroup = string.Empty;
        [ObservableProperty] private string _nameError = string.Empty;
        [ObservableProperty] private string _muscleGroupError = string.Empty;
        [ObservableProperty] private bool _hasNameError;
        [ObservableProperty] private bool _hasMuscleGroupError;
        [ObservableProperty] private bool _hasValidInput;
        #endregion

        #region "PARTIAL METHODS"
        partial void OnExerciseNameChanged(string value)
        {
            HasNameError = false;
            NameError = string.Empty;
            UpdateHasValidInput();
        }

        partial void OnSelectedMuscleGroupChanged(string value)
        {
            HasMuscleGroupError = false;
            MuscleGroupError = string.Empty;
            UpdateHasValidInput();
        }
        #endregion

        #region "SELECT MUSCLE GROUP"
        [RelayCommand]
        private void SelectMuscleGroup(string muscleGroup)
        {
            SelectedMuscleGroup = muscleGroup;
        }
        #endregion

        #region "UPDATE HAS VALID INPUT"
        private void UpdateHasValidInput()
        {
            HasValidInput = !string.IsNullOrWhiteSpace(ExerciseName)
                && !string.IsNullOrWhiteSpace(SelectedMuscleGroup);
        }
        #endregion

        #region "SAVE EXERCISE"
        [RelayCommand]
        private async Task SaveExercise()
        {
            HasNameError = false;
            HasMuscleGroupError = false;

            if (string.IsNullOrWhiteSpace(ExerciseName))
            {
                NameError = "Please enter an exercise name.";
                HasNameError = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedMuscleGroup))
            {
                MuscleGroupError = "Please select a muscle group.";
                HasMuscleGroupError = true;
                return;
            }

            try
            {
                IsLoading = true;
                var exercise = new Exercise
                {
                    Name = ExerciseName.Trim(),
                    MuscleGroup = SelectedMuscleGroup,
                    CreatedDate = DateTime.Now,
                    IsCustom = true
                };

                await workoutService.SaveExerciseAsync(exercise);

                // Pass the new exercise back to the picker
                await Shell.Current.GoToAsync("..", new Dictionary<string, object>
                {
                    { "SelectedExercise", exercise },
                    { "EditSelectedExercise", exercise }
                });
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

        #region "CANCEL"
        [RelayCommand]
        private static Task Cancel() => Shell.Current.GoToAsync(Routes.Back);
        #endregion
    }
}