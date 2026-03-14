using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class AddWorkoutViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ObservableCollection<Exercise> _allExercises = [];
        [ObservableProperty] private int _currentReps;
        [ObservableProperty] private int _currentSetNumber = 1;
        [ObservableProperty] private double _currentWeight;
        [ObservableProperty] private string _dayName = string.Empty;
        [ObservableProperty] private TimeSpan _endTime;
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
        [ObservableProperty] private Exercise? _selectedExercise;
        [ObservableProperty] private TimeSpan _startTime;
        [ObservableProperty] private string _weightUnit = "lbs";
        [ObservableProperty] private string _workoutName = string.Empty;
        [ObservableProperty] private ObservableCollection<WorkoutSet> _workoutExercises = [];
        #endregion

        #region "ADD SET"
        [RelayCommand]
        private async Task AddSet()
        {
            if (SelectedExercise is null)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Please select an exercise first", "OK");
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
        #endregion

        #region "CANCEL"
        [RelayCommand]
        private static Task Cancel() => Shell.Current.GoToAsync(Routes.Home);
        #endregion

        #region "LOAD EXERCISES"
        [RelayCommand]
        private async Task LoadExercises()
        {
            try
            {
                IsLoading = true;
                var exercises = await workoutService.GetAllExercisesAsync();
                AllExercises = new ObservableCollection<Exercise>(exercises);
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

        #region "REMOVE SET"
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
        #endregion

        #region "RESET FORM"
        private void ResetForm()
        {
            WorkoutName = string.Empty;
            Notes = string.Empty;
            SelectedExercise = null;
            CurrentReps = 0;
            CurrentWeight = 0;
            CurrentSetNumber = 1;
            WeightUnit = "lbs";
            SelectedDate = DateTime.Today;
            StartTime = TimeSpan.Zero;
            EndTime = TimeSpan.Zero;
            WorkoutExercises.Clear();
        }
        #endregion

        #region "SAVE WORKOUT"
        [RelayCommand]
        private async Task SaveWorkout()
        {
            if (WorkoutExercises.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Please add at least one set", "OK");
                return;
            }

            try
            {
                IsLoading = true;

                var duration = EndTime - StartTime;
                if (duration.TotalSeconds <= 0)
                    duration = TimeSpan.FromMinutes(60);

                var session = new WorkoutSession
                {
                    Date = SelectedDate,
                    DayName = string.IsNullOrWhiteSpace(WorkoutName) ? DayName : WorkoutName,
                    Notes = Notes,
                    Duration = duration,
                    TotalExercises = WorkoutExercises.Select(w => w.Exercise.Id).Distinct().Count()
                };

                int sessionId = await workoutService.SaveSessionAsync(session);

                foreach (var workoutSet in WorkoutExercises)
                {
                    await workoutService.SaveSetAsync(new WorkoutSet
                    {
                        ExerciseId = workoutSet.Exercise.Id,
                        WorkoutSessionId = sessionId,
                        SetNumber = workoutSet.SetNumber,
                        Reps = workoutSet.Reps,
                        Weight = workoutSet.Weight,
                        WeightUnit = workoutSet.WeightUnit,
                        CreatedDate = SelectedDate
                    });
                }
                ResetForm();
                await Shell.Current.GoToAsync(Routes.Home);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
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