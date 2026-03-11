using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class AddWorkoutViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTY"
        [ObservableProperty]
        private ObservableCollection<Exercise> allExercises = [];

        [ObservableProperty]
        private int _currentReps = 0;

        [ObservableProperty]
        private int _currentSetNumber = 1;

        [ObservableProperty]
        private double _currentWeight = 0;

        [ObservableProperty]
        private string _dayName = string.Empty;

        [ObservableProperty]
        private TimeSpan _endTime;

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private Exercise _selectedExercise;

        [ObservableProperty]
        private TimeSpan _startTime;

        [ObservableProperty]
        private string _weightUnit = "lbs";

        [ObservableProperty]
        private string _workoutName = "";

        [ObservableProperty]
        private ObservableCollection<WorkoutSet> _workoutExercises = [];
        #endregion

        #region "LOAD EXERCISES"
        [RelayCommand]
        private async Task LoadExercises()
        {
            try
            {
                IsLoading = true;
                AllExercises.Clear();

                var exercises = await workoutService.GetAllExercisesAsync();       
                foreach (var exercise in exercises)
                {
                    AllExercises.Add(exercise);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("LoadExercises Error", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region "CANCEL"
        [RelayCommand]
        private static async Task Cancel()
        {
            await Shell.Current.GoToAsync(Routes.Home);
        }
        #endregion

        #region "ADD SET"
        [RelayCommand]
        private async Task AddSet()
        {
            if (SelectedExercise == null)
            {
                await Shell.Current.DisplayAlertAsync("AddSet Error", "Please select an exercise first", "OK");
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

        #region "SAVE WORKOUT"
        [RelayCommand]
        private async Task SaveWorkout()
        {
            try
            {
                if (WorkoutExercises.Count == 0)
                {
                    await Shell.Current.DisplayAlertAsync("SaveWorkout Error", "Please add at least one set", "OK");
                    return;
                }

                IsLoading = true;

                var duration = EndTime.Subtract(StartTime);
                if (duration.TotalSeconds <= 0)
                {
                    duration = TimeSpan.FromMinutes(60);
                }
                
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
                    var set = new WorkoutSet
                    {
                        ExerciseId = workoutSet.Exercise.Id,
                        WorkoutSessionId = sessionId,
                        SetNumber = workoutSet.SetNumber,
                        Reps = workoutSet.Reps,
                        Weight = workoutSet.Weight,
                        WeightUnit = workoutSet.WeightUnit,
                        CreatedDate = SelectedDate
                    };
                    await workoutService.SaveSetAsync(set);
                }
                await Shell.Current.GoToAsync(Routes.Home);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                await Shell.Current.DisplayAlertAsync("SaveWorkout Error", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

    }
}
