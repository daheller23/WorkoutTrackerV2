using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(Session), "Session")]
    [QueryProperty(nameof(SelectedExercise), "SelectedExercise")]
    public partial class EditWorkoutViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private WorkoutSession _session = new();
        [ObservableProperty] private ObservableCollection<ExerciseGroup> _exerciseGroups = [];
        [ObservableProperty] private Exercise? _selectedExercise;
        [ObservableProperty] private string _workoutName = string.Empty;
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
        [ObservableProperty] private string _dayName = string.Empty;
        [ObservableProperty] private TimeSpan _startTime;
        [ObservableProperty] private TimeSpan _endTime;
        #endregion

        #region "ON SESSION CHANGED"
        partial void OnSessionChanged(WorkoutSession value)
        {
            if (value is not null)
            {
                LoadDataCommand.Execute(null);
            }         
        }
        #endregion

        #region "ON SELECTED EXERCISE CHANGED"
        partial void OnSelectedExerciseChanged(Exercise? value)
        {
            if (value is null)
            {
                return;
            }
                
            var existing = ExerciseGroups.FirstOrDefault(i => i.Exercise.Id == value.Id);
            if (existing is not null)
            {
                existing.AddSet();
            }          
            else
            {
                var group = new ExerciseGroup(value);
                group.AddSet();
                ExerciseGroups.Add(group);
            }
            SelectedExercise = null;
        }
        #endregion

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading)
            {
                return;
            }

            try
            {
                IsLoading = true;

                WorkoutName = Session.DayName;
                Notes = Session.Notes;
                SelectedDate = Session.Date;
                DayName = Session.Date.ToString("dddd");

                ExerciseGroups.Clear();
                var sets = await workoutService.GetSetsForSessionAsync(Session.Id);
                foreach (var set in sets)
                {
                    set.Exercise = await workoutService.GetExerciseAsync(set.ExerciseId);
                    var existing = ExerciseGroups.FirstOrDefault(i => i.Exercise.Id == set.ExerciseId);
                    if (existing is not null)
                    {
                        set.ParentGroup = existing;
                        set.DeleteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => existing.RemoveSet(set));
                        existing.Sets.Add(set);
                    }
                    else
                    {
                        var group = new ExerciseGroup(set.Exercise);
                        set.ParentGroup = group;
                        set.DeleteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => group.RemoveSet(set));
                        group.Sets.Add(set);
                        ExerciseGroups.Add(group);
                    }
                }
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

        #region "OPEN EXERCISE PICKER"
        [RelayCommand]
        private static async Task OpenExercisePicker()
        {
            await Shell.Current.GoToAsync(Routes.ExercisePicker);
        }
        #endregion

        #region "ADD SET"
        [RelayCommand]
        private static void AddSet(ExerciseGroup group) => group.AddSet();
        #endregion

        #region "REMOVE EXERCISE"
        [RelayCommand]
        private void RemoveExercise(ExerciseGroup group) => ExerciseGroups.Remove(group);
        #endregion

        #region "SAVE WORKOUT"
        [RelayCommand]
        private async Task SaveWorkout()
        {
            if (ExerciseGroups.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Please add at least one exercise", "OK");
                return;
            }

            try
            {
                IsLoading = true;

                Session.DayName = string.IsNullOrWhiteSpace(WorkoutName) ? DayName : WorkoutName;
                Session.Notes = Notes;
                Session.Date = SelectedDate;
                Session.TotalExercises = ExerciseGroups.Count;

                await workoutService.SaveSessionAsync(Session);

                var oldSets = await workoutService.GetSetsForSessionAsync(Session.Id);
                foreach (var old in oldSets)
                {
                    await workoutService.DeleteSetAsync(old.Id);
                }
                    
                int setNumber = 1;
                foreach (var group in ExerciseGroups)
                {
                    foreach (var set in group.Sets)
                    {
                        await workoutService.SaveSetAsync(new WorkoutSet
                        {
                            ExerciseId = group.Exercise.Id,
                            WorkoutSessionId = Session.Id,
                            SetNumber = setNumber++,
                            Reps = set.Reps,
                            Weight = set.Weight,
                            WeightUnit = set.WeightUnit,
                            CreatedDate = SelectedDate
                        });
                    }
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
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
        #endregion

    }
}