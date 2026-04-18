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
        [ObservableProperty] private int _totalSets;

        [ObservableProperty] private double _totalVolume;

        [ObservableProperty] private string _dayName = $"{DateTime.Today:dddd} · Today";
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private string _weightUnitLabel = string.Empty;
        [ObservableProperty] private string _workoutName = string.Empty;

        [ObservableProperty] private TimeSpan _startTime;
        [ObservableProperty] private TimeSpan _endTime;

        [ObservableProperty] private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty] private WorkoutSession _session = new();
        [ObservableProperty] private ObservableCollection<ExerciseGroup> _exerciseGroups = [];
        [ObservableProperty] private Exercise? _selectedExercise;

        // ==============================================================================================================
        //
        //      PARTIAL METHODS
        //
        // ==============================================================================================================

        partial void OnSessionChanged(WorkoutSession value)
        {
            if (value is null)
            {
                return;
            }
            _ = LoadDataAsync();
        }

        partial void OnSelectedExerciseChanged(Exercise? value)
        {
            if (value is null)
            {
                return;
            }

            var existing = ExerciseGroups.FirstOrDefault(g => g.Exercise.Id == value.Id);
            if (existing is not null)
            {
                existing.AddSet(settingsService.WeightUnit);
            }              
            else
            {
                var group = new ExerciseGroup(value, settingsService.WeightUnit);
                group.AddSet(settingsService.WeightUnit);
                ExerciseGroups.Add(group);
            }
            SelectedExercise = null;
            UpdateTotals();
        }

        partial void OnSelectedDateChanged(DateTime value)
        {
            var days = (DateTime.Today - value.Date).Days;
            DayName = $"{value:dddd} · {days switch
            {
                0 => "Today",
                1 => "Yesterday",
                -1 => "Tomorrow",
                _ => days > 0 ? $"{days} days ago" : $"In {-days} days"
            }}";
        }

        // ==============================================================================================================
        //
        //      PRIVATE RELAY METHODS
        //
        // ==============================================================================================================

        [RelayCommand]
        private async Task LoadData() => await LoadDataAsync();

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

            var newSet = CreateWorkoutSet(group.Exercise, group, group.Sets.Count + 1, lastSet.Reps, lastSet.Weight, lastSet.WeightUnit);
            group.Sets.Add(newSet);
            OnPropertyChanged(nameof(group.SetCountLabel));
            UpdateTotals();
        }

        [RelayCommand]
        private static async Task OpenExercisePicker()
            => await Shell.Current.GoToAsync(Routes.ExercisePicker);

        [RelayCommand]
        private void AddSet(ExerciseGroup group)
        {
            group.AddSet(settingsService.WeightUnit);
            UpdateTotals();
        }

        [RelayCommand]
        private void RemoveSet((ExerciseGroup Group, WorkoutSet Set) args)
        {
            args.Group.RemoveSet(args.Set);
            if (args.Group.Sets.Count == 0)
            {
                ExerciseGroups.Remove(args.Group);
            }           
            UpdateTotals();
        }

        [RelayCommand]
        private void RemoveExercise(ExerciseGroup group)
        {
            ExerciseGroups.Remove(group);
            UpdateTotals();
        }

        [RelayCommand]
        private async Task SaveWorkout()
        {
            if (ExerciseGroups.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Please add at least one exercise", "OK");
                return;
            }

            var allSets = ExerciseGroups.SelectMany(g => g.Sets).ToList();
            if (allSets.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Please add at least one set", "OK");
                return;
            }

            if (allSets.Any(s => s.Reps <= 0))
            {
                await Shell.Current.DisplayAlertAsync("Error", "All sets must have reps greater than 0", "OK");
                return;
            }

            if (StartTime > TimeSpan.Zero && EndTime > TimeSpan.Zero && StartTime >= EndTime)
            {
                bool proceed = await Shell.Current.DisplayAlertAsync(
                    "Time Warning",
                    "Start time is after or equal to end time. Duration will default to 60 minutes. Continue?",
                    "Yes", "No");
                if (!proceed)
                {
                    return;
                }
            }

            try
            {
                IsLoading = true;

                var duration = EndTime - StartTime;
                if (duration.TotalSeconds <= 0)
                {
                    duration = TimeSpan.FromMinutes(60);
                }         

                Session.DayName = string.IsNullOrWhiteSpace(WorkoutName) ? DayName : WorkoutName;
                Session.Notes = Notes;
                Session.Date = SelectedDate;
                Session.Duration = duration;
                Session.TotalExercises = ExerciseGroups.Count;

                await workoutService.SaveSessionAsync(Session);

                await workoutService.DeleteSetsForSessionAsync(Session.Id);

                int setNumber = 1;
                var newSets = ExerciseGroups.SelectMany(group => group.Sets.Select(set => new WorkoutSet
                {
                    ExerciseId = group.Exercise.Id,
                    WorkoutSessionId = Session.Id,
                    SetNumber = setNumber++,
                    Reps = set.Reps,
                    Weight = set.Weight,
                    WeightUnit = set.WeightUnit,
                    CreatedDate = SelectedDate
                }))
                .ToList();

                await workoutService.SaveAllSetsAsync(newSets);
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

        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================

        private static WorkoutSet CreateWorkoutSet(Exercise exercise, ExerciseGroup group, int setNumber, int reps, double weight, string weightUnit)
        {
            var set = new WorkoutSet
            {
                Exercise = exercise,
                ExerciseId = exercise.Id,
                SetNumber = setNumber,
                Reps = reps,
                Weight = weight,
                WeightUnit = weightUnit,
                ParentGroup = group
            };
            set.DeleteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => group.RemoveSet(set));
            return set;
        }

        private void UpdateTotals()
        {
            int sets = 0;
            double volume = 0;
            foreach (var group in ExerciseGroups)
            {
                sets += group.Sets.Count;
                foreach (var set in group.Sets)
                {
                    volume += set.Weight * set.Reps;
                }                 
            }
            TotalSets = sets;
            TotalVolume = volume;
        }

        private async Task LoadDataAsync()
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
                WeightUnitLabel = $"{settingsService.WeightUnit} total";
                StartTime = Session.Date.TimeOfDay;
                EndTime = Session.Date.TimeOfDay + Session.Duration;

                var setsTask = workoutService.GetSetsForSessionAsync(Session.Id);
                var exercisesTask = workoutService.GetAllExercisesAsync();
                await Task.WhenAll(setsTask, exercisesTask);

                var sets = setsTask.Result;
                var exerciseDict = exercisesTask.Result.ToDictionary(e => e.Id);

                var groups = new List<ExerciseGroup>();
                foreach (var set in sets)
                {
                    if (!exerciseDict.TryGetValue(set.ExerciseId, out var exercise))
                    {
                        continue;
                    }
                    set.Exercise = exercise;

                    var existing = groups.FirstOrDefault(g => g.Exercise.Id == set.ExerciseId);
                    if (existing is not null)
                    {
                        var ws = CreateWorkoutSet(exercise, existing, existing.Sets.Count + 1, set.Reps, set.Weight, set.WeightUnit);
                        ws.Id = set.Id;
                        existing.Sets.Add(ws);
                    }
                    else
                    {
                        var group = new ExerciseGroup(exercise);
                        var ws = CreateWorkoutSet(exercise, group, 1, set.Reps, set.Weight, set.WeightUnit);
                        ws.Id = set.Id;
                        group.Sets.Add(ws);
                        groups.Add(group);
                    }
                }

                ExerciseGroups = new ObservableCollection<ExerciseGroup>(groups);
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
    }
}
