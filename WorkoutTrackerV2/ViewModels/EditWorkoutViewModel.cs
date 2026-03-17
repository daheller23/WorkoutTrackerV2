using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(SelectedExercise), "EditSelectedExercise")]
    [QueryProperty(nameof(Session), "Session")]
    public partial class EditWorkoutViewModel(IWorkoutService workoutService, ISettingsService settingsService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private WorkoutSession _session = new();
        [ObservableProperty] private ObservableCollection<ExerciseGroup> _exerciseGroups = [];
        [ObservableProperty] private Exercise? _selectedExercise;
        [ObservableProperty] private string _workoutName = string.Empty;
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
        [ObservableProperty] private string _dayName = $"{DateTime.Today:dddd} · Today";
        [ObservableProperty] private TimeSpan _startTime;
        [ObservableProperty] private TimeSpan _endTime;
        [ObservableProperty] private double _totalVolume;
        [ObservableProperty] private int _totalSets;
        [ObservableProperty] private string _weightUnitLabel = "lbs total";
        #endregion

        #region "ON SESSION CHANGED"
        partial void OnSessionChanged(WorkoutSession value)
        {
            if (value is not null)
                LoadDataCommand.Execute(null);
        }
        #endregion

        #region "ON SELECTED EXERCISE CHANGED"
        partial void OnSelectedExerciseChanged(Exercise? value)
        {
            if (value is null) return;

            var existing = ExerciseGroups.FirstOrDefault(g => g.Exercise.Id == value.Id);
            if (existing is not null)
                existing.AddSet(settingsService.WeightUnit);
            else
            {
                var group = new ExerciseGroup(value, settingsService.WeightUnit);
                group.AddSet(settingsService.WeightUnit);
                ExerciseGroups.Add(group);
            }
            SelectedExercise = null;
            UpdateTotals();
        }
        #endregion

        #region "ON SELECTED DATE CHANGED"
        partial void OnSelectedDateChanged(DateTime value)
        {
            var days = (DateTime.Today - value.Date).Days;
            string relative = days switch
            {
                0 => "Today",
                1 => "Yesterday",
                -1 => "Tomorrow",
                _ => days > 0 ? $"{days} days ago" : $"In {-days} days"
            };
            DayName = $"{value:dddd} · {relative}";
        }
        #endregion

        #region "UPDATE TOTALS"
        private void UpdateTotals()
        {
            TotalSets = ExerciseGroups.Sum(g => g.Sets.Count);
            TotalVolume = ExerciseGroups
                .SelectMany(g => g.Sets)
                .Sum(s => s.Weight * s.Reps);
        }
        #endregion

        #region "COPY LAST SET"
        [RelayCommand]
        private void CopyLastSet(ExerciseGroup group)
        {
            var lastSet = group.Sets.LastOrDefault();
            if (lastSet is null)
            {
                group.AddSet(settingsService.WeightUnit);
                UpdateTotals();
                return;
            }

            var newSet = new WorkoutSet
            {
                Exercise = group.Exercise,
                ExerciseId = group.Exercise.Id,
                SetNumber = group.Sets.Count + 1,
                Reps = lastSet.Reps,
                Weight = lastSet.Weight,
                WeightUnit = lastSet.WeightUnit,
                ParentGroup = group
            };
            newSet.DeleteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => group.RemoveSet(newSet));
            group.Sets.Add(newSet);
            OnPropertyChanged(nameof(group.SetCountLabel));
            UpdateTotals();
        }
        #endregion

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;

                WorkoutName = Session.DayName;
                Notes = Session.Notes;
                SelectedDate = Session.Date;
                WeightUnitLabel = $"{settingsService.WeightUnit} total";

                // Restore start/end time from session duration
                StartTime = Session.Date.TimeOfDay;
                EndTime = Session.Date.TimeOfDay + Session.Duration;

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
                UpdateTotals();
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
        private void AddSet(ExerciseGroup group)
        {
            group.AddSet(settingsService.WeightUnit);
            UpdateTotals();
        }
        #endregion

        #region "REMOVE SET"
        [RelayCommand]
        private void RemoveSet((ExerciseGroup Group, WorkoutSet Set) args)
        {
            args.Group.RemoveSet(args.Set);
            if (args.Group.Sets.Count == 0)
                ExerciseGroups.Remove(args.Group);
            UpdateTotals();
        }
        #endregion

        #region "REMOVE EXERCISE"
        [RelayCommand]
        private void RemoveExercise(ExerciseGroup group)
        {
            ExerciseGroups.Remove(group);
            UpdateTotals();
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

            // Validate reps
            if (allSets.Any(s => s.Reps <= 0))
            {
                await Shell.Current.DisplayAlertAsync("Error", "All sets must have reps greater than 0", "OK");
                return;
            }

            // Warn if start time is after end time
            if (StartTime > TimeSpan.Zero && EndTime > TimeSpan.Zero && StartTime >= EndTime)
            {
                bool proceed = await Shell.Current.DisplayAlertAsync(
                    "Time Warning",
                    "Start time is after or equal to end time. Duration will default to 60 minutes. Continue?",
                    "Yes", "No");
                if (!proceed) return;
            }

            try
            {
                IsLoading = true;

                var duration = EndTime - StartTime;
                if (duration.TotalSeconds <= 0)
                    duration = TimeSpan.FromMinutes(60);

                Session.DayName = string.IsNullOrWhiteSpace(WorkoutName) ? DayName : WorkoutName;
                Session.Notes = Notes;
                Session.Date = SelectedDate;
                Session.Duration = duration;
                Session.TotalExercises = ExerciseGroups.Count;

                await workoutService.SaveSessionAsync(Session);

                var oldSets = await workoutService.GetSetsForSessionAsync(Session.Id);
                foreach (var old in oldSets)
                    await workoutService.DeleteSetAsync(old.Id);

                int setNumber = 1;
                foreach (var group in ExerciseGroups)
                    foreach (var set in group.Sets)
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