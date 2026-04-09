using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(SelectedExercise), "SelectedExercise")]
    public partial class AddWorkoutViewModel(IWorkoutService workoutService, ITemplateService templateService, ISettingsService settingsService, IRestTimerService restTimerService, IAnalyticsService analyticsService) : BaseViewModel, IDisposable
    {
        private bool                        _ignoreNextExerciseSelection;
        private Dictionary<int, double>?    _prBaselines;

        [ObservableProperty] private int    _totalSets;

        [ObservableProperty] private double _totalVolume;

        [ObservableProperty] private bool   _isMenuVisible;

        [ObservableProperty] private string _dayName = $"{DateTime.Today:dddd} · Today";
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private string _weightUnitLabel = string.Empty;
        [ObservableProperty] private string _workoutName = string.Empty;

        [ObservableProperty] private TimeSpan _endTime;
        [ObservableProperty] private TimeSpan _startTime;

        [ObservableProperty] private ObservableCollection<ExerciseGroup>    _exerciseGroups = [];    
        [ObservableProperty] private DateTime                               _selectedDate = DateTime.Today;
        [ObservableProperty] private Exercise?                              _selectedExercise;

        public RestTimerViewModel TimerViewModel { get; } = new RestTimerViewModel(restTimerService);

        // ==============================================================================================================
        //
        //      PUBLIC METHODS
        //
        // ==============================================================================================================
        
        public void Dispose()
        {
            TimerViewModel.Unsubscribe();
            TimerViewModel.Dispose();
        }

        // ==============================================================================================================
        //
        //      PARTIAL METHODS
        //
        // ==============================================================================================================

        partial void OnSelectedExerciseChanged(Exercise? value)
        {
            if (value is null) 
            { 
                return; 
            }                   
            if (ShouldIgnoreSelection()) 
            {
                return; 
            }
            if (TryHandlePendingTemplate()) 
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
                _ = LoadLastSessionAsync(group, value.Id);
            }
            SelectedExercise = null;
            UpdateTotals();
        }

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

            if (string.IsNullOrWhiteSpace(WorkoutName))
            {
                WorkoutName = value.ToString("dddd");
            }           
        }

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private void StartRestTimer(string muscleGroup) =>
            restTimerService.StartDefault(muscleGroup);

        [RelayCommand]
        private Task OpenTemplatePicker()
        {
            templateService.PendingTemplate = null;
            _ignoreNextExerciseSelection = true;
            return Shell.Current.GoToAsync(Routes.TemplatePicker);
        }

        [RelayCommand]
        private static Task ViewExercisePicker()
            => Shell.Current.GoToAsync(Routes.ExercisePicker);

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

            if (allSets.Any(s => s.Reps <= 0 || s.Weight < 0))
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
                if (!proceed) return;
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
                var workoutSets = ExerciseGroups
                    .SelectMany(group => group.Sets.Select(set => new WorkoutSet
                    {
                        ExerciseId = group.Exercise.Id,
                        WorkoutSessionId = sessionId,
                        SetNumber = setNumber++,
                        Reps = set.Reps,
                        Weight = set.Weight,
                        WeightUnit = set.WeightUnit,
                        CreatedDate = SelectedDate
                    }))
                    .ToList();

                var saveTask = workoutService.SaveAllSetsAsync(workoutSets);
                var newPr = DetectWeightPr(workoutSets);
                await saveTask;

                HomeViewModel.PendingPrMessage = newPr ?? string.Empty;

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

        [RelayCommand]
        private async Task Clear()
        {
            bool confirmed = await Shell.Current.DisplayAlertAsync("Clear Workout Session", "Are you sure you want to clear this workout session?", "Yes", "No");
            if (!confirmed)
            {
                return;
            }
            ResetForm();
        }

        [RelayCommand]
        private void ClearSelectedExercise() => SelectedExercise = null;

        [RelayCommand]
        private void RefreshWeightUnit()
        {
            WeightUnitLabel = $"{settingsService.WeightUnit} total";
            _ = LoadPrBaselinesAsync();
        }

        [RelayCommand]
        private async Task LoadFromTemplateSets((WorkoutTemplate template, List<WorkoutTemplateSet> sets) args)
        {
            try
            {
                await ApplyTemplateSetsAsync(args.template, args.sets);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task LoadFromTemplate(WorkoutTemplate template)
            => await LoadFromTemplateAsync(template);

        [RelayCommand]
        private void CopyLastSet(ExerciseGroup group)
        {
            var lastSet = group.Sets.LastOrDefault();
            group.AddSet(lastSet?.WeightUnit ?? settingsService.WeightUnit);

            if (lastSet is not null)
            {
                var newSet = group.Sets[^1];
                newSet.Reps = lastSet.Reps;
                newSet.Weight = lastSet.Weight;
                newSet.WeightUnit = lastSet.WeightUnit;
                newSet.SuggestedWeightPlaceholder = lastSet.SuggestedWeightPlaceholder;
            }

            UpdateTotals();
        }

        [RelayCommand]
        private async Task SaveAsTemplate()
        {
            if (ExerciseGroups.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Please add at least one exercise first", "OK");
                return;
            }

            string name = await Shell.Current.DisplayPromptAsync("Save Template", "Enter a name for this template", placeholder: string.IsNullOrWhiteSpace(WorkoutName) ? "My Template" : WorkoutName);

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            try
            {
                var template = new WorkoutTemplate { Name = name, Notes = Notes };
                int templateId = await workoutService.SaveTemplateAsync(template);

                int setNumber = 1;
                var templateSets = ExerciseGroups
                    .SelectMany(group => group.Sets.Select(set => new WorkoutTemplateSet
                    {
                        TemplateId = templateId,
                        ExerciseId = group.Exercise.Id,
                        SetNumber = setNumber++,
                        Reps = set.Reps,
                        Weight = set.Weight,
                        WeightUnit = set.WeightUnit
                    }))
                    .ToList();

                await workoutService.SaveAllTemplateSetsAsync(templateSets);
                await Shell.Current.DisplayAlertAsync("Saved", $"'{name}' saved as a template!", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private void ToggleMenu() => IsMenuVisible = !IsMenuVisible;

        [RelayCommand]
        private void HandleMenuAction(string action)
        {
            IsMenuVisible = false;
            switch (action.ToLower())
            {
                case "load":
                    OpenTemplatePickerCommand.Execute(null);
                    break;
                case "save":
                    SaveAsTemplateCommand.Execute(null);
                    break;
            }
        }

        [RelayCommand]
        private void PrepareForTemplatePicker() => _ignoreNextExerciseSelection = true;


        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================

        private bool ShouldIgnoreSelection()
        {
            if (!_ignoreNextExerciseSelection)
            {
                return false;
            }
            _ignoreNextExerciseSelection = false;
            SelectedExercise = null;
            return true;
        }

        private bool TryHandlePendingTemplate()
        {
            if (templateService.PendingTemplate is null)
            {
                return false;
            }
            var template = templateService.PendingTemplate;
            templateService.PendingTemplate = null;
            _ = LoadFromTemplateAsync(template);
            return true;
        }

        private async Task LoadFromTemplateAsync(WorkoutTemplate template)
        {
            try
            {
                var sets = await workoutService.GetTemplateSetsAsync(template.Id);
                await ApplyTemplateSetsAsync(template, sets);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        private async Task ApplyTemplateSetsAsync(WorkoutTemplate template, List<WorkoutTemplateSet> sets)
        {
            var exercises = await workoutService.GetAllExercisesAsync();
            var exerciseDict = exercises.ToDictionary(e => e.Id);

            ExerciseGroups.Clear();
            WorkoutName = template.Name;
            Notes = template.Notes;

            foreach (var set in sets)
            {
                if (!exerciseDict.TryGetValue(set.ExerciseId, out var exercise))
                {
                    continue;
                }

                var existing = ExerciseGroups.FirstOrDefault(g => g.Exercise.Id == exercise.Id);
                if (existing is not null)
                {
                    var workoutSet = CreateWorkoutSet(exercise, existing, existing.Sets.Count + 1, set.Reps, set.Weight, set.WeightUnit);
                    existing.Sets.Add(workoutSet);
                    existing.NotifySetStatsPublic();
                }
                else
                {
                    var group = new ExerciseGroup(exercise, settingsService.WeightUnit);
                    var workoutSet = CreateWorkoutSet(exercise, group, 1, set.Reps, set.Weight, set.WeightUnit);
                    group.Sets.Add(workoutSet);
                    group.NotifySetStatsPublic();
                    ExerciseGroups.Add(group);
                }
            }

            UpdateTotals();
        }

        private WorkoutSet CreateWorkoutSet(Exercise exercise, ExerciseGroup group, int setNumber, int reps, double weight, string weightUnit)
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
            set.DeleteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                group.RemoveSet(set);
                if (group.Sets.Count == 0)
                {
                    ExerciseGroups.Remove(group);
                }               
                UpdateTotals();
            });
            return set;
        }
        private string? DetectWeightPr(List<WorkoutSet> savedSets)
        {
            if (_prBaselines is null)
            {
                return null;
            }

            string? message = null;
            var maxByExercise = savedSets.GroupBy(s => s.ExerciseId).Select(g => (ExerciseId: g.Key, Max: g.Max(s => s.Weight))).ToList();

            foreach (var (exerciseId, max) in maxByExercise)
            {
                var baseline = _prBaselines.TryGetValue(exerciseId, out double b) ? b : 0;
                if (max > baseline)
                {
                    var group = ExerciseGroups.FirstOrDefault(g => g.Exercise.Id == exerciseId);
                    var name = group?.Exercise.Name ?? "exercise";
                    var unit = group?.Sets.FirstOrDefault()?.WeightUnit ?? settingsService.WeightUnit;
                    message = $"New PR on {name}! 🏆 {max} {unit}";
                    break;
                }
            }

            return message;
        }

        private async Task LoadPrBaselinesAsync()
        {
            try
            {
                var records = await analyticsService.GetPersonalRecordsAsync(0);
                _prBaselines = records.ToDictionary(r => r.ExerciseId, r => r.BestWeight);
            }
            catch
            {
                _prBaselines = [];
            }
        }

        private async Task LoadLastSessionAsync(ExerciseGroup group, int exerciseId)
        {
            try
            {
                var history = await workoutService.GetExerciseHistoryAsync(exerciseId, 0);
                group.SetLastSession(history, settingsService.WeightUnit);
            }
            catch
            {
                // Non-critical — silently swallow. The last session display is
                // a convenience feature; a fetch failure should not surface as
                // an error to the user.
            }
        }

        private void ResetForm()
        {
            _ignoreNextExerciseSelection = false;
            templateService.PendingTemplate = null;
            WorkoutName = string.Empty;
            Notes = string.Empty;
            SelectedDate = DateTime.Today;
            StartTime = TimeSpan.Zero;
            EndTime = TimeSpan.Zero;
            ExerciseGroups.Clear();
            WeightUnitLabel = $"{settingsService.WeightUnit} total";
            UpdateTotals();
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
    }
}
