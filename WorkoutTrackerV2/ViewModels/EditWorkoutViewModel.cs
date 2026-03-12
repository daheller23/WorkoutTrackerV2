using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(Session), "Session")]
    public partial class EditWorkoutViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty]
        private WorkoutSession _session;
        partial void OnSessionChanged(WorkoutSession value)
        {
            if (value != null)
                LoadDataCommand.Execute(null);
        }

        [ObservableProperty]
        private ObservableCollection<Exercise> _allExercises = [];

        [ObservableProperty]
        private ObservableCollection<WorkoutSet> _workoutExercises = [];

        [ObservableProperty]
        private Exercise _selectedExercise = null;

        [ObservableProperty]
        private int _currentSetNumber = 1;

        [ObservableProperty]
        private int _currentReps = 0;

        [ObservableProperty]
        private double _currentWeight = 0;

        [ObservableProperty]
        private string _weightUnit = "lbs";

        [ObservableProperty]
        private string _workoutName = "";

        [ObservableProperty]
        private string _notes = "";

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        private string _dayName = string.Empty;

        [ObservableProperty]
        private TimeSpan _startTime = TimeSpan.Zero;

        [ObservableProperty]
        private TimeSpan _endTime = TimeSpan.Zero;
        #endregion

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData()
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
                    
                WorkoutName = Session.DayName;
                Notes = Session.Notes;
                SelectedDate = Session.Date;
                DayName = Session.Date.ToString("dddd");

                WorkoutExercises.Clear();
                var sets = await workoutService.GetSetsForSessionAsync(Session.Id);
                foreach (var set in sets)
                {
                    set.Exercise = await workoutService.GetExerciseAsync(set.ExerciseId);
                    WorkoutExercises.Add(set);
                }
                CurrentSetNumber = WorkoutExercises.Count + 1;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("LoadData Error", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region "ADD SET"
        [RelayCommand]
        private async Task AddSet()
        {
            if (SelectedExercise == null)
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
                WeightUnit = WeightUnit,
                WorkoutSessionId = Session.Id
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
                    await Shell.Current.DisplayAlertAsync("Error", "Please add at least one set", "OK");
                    return;
                }

                IsLoading = true;

                Session.DayName = string.IsNullOrWhiteSpace(WorkoutName) ? DayName : WorkoutName;
                Session.Notes = Notes;
                Session.Date = SelectedDate;
                Session.TotalExercises = WorkoutExercises.Select(w => w.Exercise.Id).Distinct().Count();

                await workoutService.SaveSessionAsync(Session);

                var oldSets = await workoutService.GetSetsForSessionAsync(Session.Id);
                foreach (var old in oldSets)
                {
                    await workoutService.DeleteSetAsync(old.Id);
                }
                    
                foreach (var workoutSet in WorkoutExercises)
                {
                    var set = new WorkoutSet
                    {
                        ExerciseId = workoutSet.Exercise.Id,
                        WorkoutSessionId = Session.Id,
                        SetNumber = workoutSet.SetNumber,
                        Reps = workoutSet.Reps,
                        Weight = workoutSet.Weight,
                        WeightUnit = workoutSet.WeightUnit,
                        CreatedDate = SelectedDate
                    };
                    await workoutService.SaveSetAsync(set);
                }

                await Shell.Current.GoToAsync(Routes.Back);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Save Error", ex.Message, "OK");
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
    }
}