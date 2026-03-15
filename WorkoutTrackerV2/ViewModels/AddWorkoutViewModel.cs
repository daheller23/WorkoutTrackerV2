using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(SelectedExercise), "SelectedExercise")]
    public partial class AddWorkoutViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ObservableCollection<ExerciseGroup> _exerciseGroups = [];
        [ObservableProperty] private string _dayName = string.Empty;
        [ObservableProperty] private TimeSpan _endTime;
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
        [ObservableProperty] private Exercise? _selectedExercise;
        [ObservableProperty] private TimeSpan _startTime;
        [ObservableProperty] private string _workoutName = string.Empty;
        #endregion

        #region "QUERY PROPERTY HANDLER"
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

        #region "VIEW EXERCISE PICKER"
        [RelayCommand]
        private static async Task ViewExercisePicker()
        {
            await Shell.Current.GoToAsync(Routes.ExercisePicker);
        }
        #endregion

        #region "ADD SET"
        [RelayCommand]
        private static void AddSet(ExerciseGroup group)
        {
            group.AddSet();
        }
        #endregion

        #region "REMOVE SET"
        [RelayCommand]
        private void RemoveSet((ExerciseGroup Group, WorkoutSet Set) args)
        {
            args.Group.RemoveSet(args.Set);
            if (args.Group.Sets.Count == 0)
            {
                ExerciseGroups.Remove(args.Group);
            }              
        }
        #endregion

        #region "REMOVE EXERCISE"
        [RelayCommand]
        private void RemoveExercise(ExerciseGroup group)
        {
            ExerciseGroups.Remove(group);
        }
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

            var allSets = ExerciseGroups.SelectMany(i => i.Sets).ToList();
            if (allSets.Count == 0)
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
                    TotalExercises = ExerciseGroups.Count
                };

                int sessionId = await workoutService.SaveSessionAsync(session);
                int setNumber = 1;
                foreach (var group in ExerciseGroups)
                {
                    foreach (var set in group.Sets)
                    {
                        await workoutService.SaveSetAsync(new WorkoutSet
                        {
                            ExerciseId = group.Exercise.Id,
                            WorkoutSessionId = sessionId,
                            SetNumber = setNumber++,
                            Reps = set.Reps,
                            Weight = set.Weight,
                            WeightUnit = set.WeightUnit,
                            CreatedDate = SelectedDate
                        });
                    }
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

        #region "CANCEL"
        [RelayCommand]
        private static Task Cancel() => Shell.Current.GoToAsync(Routes.Home);
        #endregion

        #region "RESET FORM"
        private void ResetForm()
        {
            WorkoutName = string.Empty;
            Notes = string.Empty;
            SelectedDate = DateTime.Today;
            StartTime = TimeSpan.Zero;
            EndTime = TimeSpan.Zero;
            ExerciseGroups.Clear();
        }
        #endregion

    }
}