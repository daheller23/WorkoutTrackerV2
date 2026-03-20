using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(SelectedExercise), "SelectedExercise")]
    public partial class AddWorkoutViewModel(
        IWorkoutService workoutService,
        ITemplateService templateService,
        ISettingsService settingsService) : BaseViewModel
    {
        private bool _ignoreNextExerciseSelection = false;

        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ObservableCollection<ExerciseGroup> _exerciseGroups = [];
        [ObservableProperty] private string _dayName = $"{DateTime.Today:dddd} · Today";
        [ObservableProperty] private TimeSpan _endTime;
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
        [ObservableProperty] private Exercise? _selectedExercise;
        [ObservableProperty] private TimeSpan _startTime;
        [ObservableProperty] private string _workoutName = string.Empty;
        [ObservableProperty] private double _totalVolume;
        [ObservableProperty] private int _totalSets;
        [ObservableProperty] private string _weightUnitLabel = string.Empty;
        #endregion

        #region "PREPARE FOR TEMPLATE PICKER"
        [RelayCommand]
        private void PrepareForTemplatePicker()
        {
            _ignoreNextExerciseSelection = true;
        }
        #endregion

        #region "ON SELECTED EXERCISE CHANGED"
        partial void OnSelectedExerciseChanged(Exercise? value)
        {
            if (value is null) return;

            if (_ignoreNextExerciseSelection)
            {
                _ignoreNextExerciseSelection = false;
                SelectedExercise = null;
                return;
            }

            if (templateService.PendingTemplate is not null)
            {
                var template = templateService.PendingTemplate;
                templateService.PendingTemplate = null;
                // FIX 10: Call the method directly instead of going through
                // LoadFromTemplateCommand.Execute() inside a property-changed handler.
                // Executing commands from partial methods bypasses CanExecute guards
                // and obscures the call flow.
                _ = LoadFromTemplateAsync(template);
                return;
            }

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

            if (string.IsNullOrWhiteSpace(WorkoutName))
                WorkoutName = value.ToString("dddd");
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

            var newSet = CreateWorkoutSet(
                group.Exercise,
                group,
                group.Sets.Count + 1,
                lastSet.Reps,
                lastSet.Weight,
                lastSet.WeightUnit);

            group.Sets.Add(newSet);
            OnPropertyChanged(nameof(group.SetCountLabel));
            UpdateTotals();
        }
        #endregion

        #region "SAVE AS TEMPLATE"
        [RelayCommand]
        private async Task SaveAsTemplate()
        {
            if (ExerciseGroups.Count == 0)
            {
                await Shell.Current.DisplayAlertAsync("Error", "Please add at least one exercise first", "OK");
                return;
            }

            string name = await Shell.Current.DisplayPromptAsync(
                "Save Template",
                "Enter a name for this template",
                placeholder: string.IsNullOrWhiteSpace(WorkoutName) ? "My Template" : WorkoutName);

            if (string.IsNullOrWhiteSpace(name)) return;

            try
            {
                var template = new WorkoutTemplate { Name = name, Notes = Notes };
                int templateId = await workoutService.SaveTemplateAsync(template);

                // FIX 2: Build the full list of template sets first, then insert
                // them all in one SaveAllTemplateSetsAsync call instead of awaiting
                // SaveTemplateSetAsync N times in a nested loop.
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
        #endregion

        #region "LOAD FROM TEMPLATE"
        [RelayCommand]
        private async Task LoadFromTemplate(WorkoutTemplate template)
            => await LoadFromTemplateAsync(template);

        // FIX 3: Shared private implementation used by both the public RelayCommand
        // and the direct call from OnSelectedExerciseChanged. Eliminates duplication
        // and means the template-fetch logic only exists in one place.
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
        #endregion

        #region "OPEN TEMPLATE PICKER"
        [RelayCommand]
        private async Task OpenTemplatePicker()
        {
            templateService.PendingTemplate = null;
            _ignoreNextExerciseSelection = true;
            await Shell.Current.GoToAsync(Routes.TemplatePicker);
        }
        #endregion

        #region "VIEW EXERCISE PICKER"
        [RelayCommand]
        private static async Task ViewExercisePicker()
            => await Shell.Current.GoToAsync(Routes.ExercisePicker);
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

                // FIX 1: Build the full set list first, then insert in one batch call
                // instead of awaiting SaveSetAsync N times in a nested loop.
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

                await workoutService.SaveAllSetsAsync(workoutSets);

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

        #region "CLEAR"
        [RelayCommand]
        private void Clear() => ResetForm();
        #endregion

        #region "CLEAR SELECTED EXERCISE"
        [RelayCommand]
        private void ClearSelectedExercise()
        {
            SelectedExercise = null;
        }
        #endregion

        #region "REFRESH WEIGHT UNIT"
        // Called from OnAppearing so WeightUnitLabel always reflects the
        // current setting — even if the user changed it in Settings and returned.
        [RelayCommand]
        private void RefreshWeightUnit()
        {
            WeightUnitLabel = $"{settingsService.WeightUnit} total";
        }
        #endregion

        #region "VIEW SETTINGS"
        [RelayCommand]
        private static Task ViewSettings() => Shell.Current.GoToAsync(Routes.Settings);
        #endregion

        #region "LOAD FROM TEMPLATE SETS"
        [RelayCommand]
        private async Task LoadFromTemplateSets((WorkoutTemplate template, List<WorkoutTemplateSet> sets) args)
        {
            try
            {
                // FIX 3: Shared ApplyTemplateSetsAsync replaces the duplicated
                // group-building logic that was copied across LoadFromTemplate
                // and LoadFromTemplateSets.
                await ApplyTemplateSetsAsync(args.template, args.sets);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
        #endregion

        #region "PRIVATE HELPERS"

        // FIX 3: Single method that both LoadFromTemplate and LoadFromTemplateSets
        // delegate to. Exercise lookup, group building, and set construction all
        // live in one place.
        private async Task ApplyTemplateSetsAsync(
            WorkoutTemplate template,
            List<WorkoutTemplateSet> sets)
        {
            var exercises = await workoutService.GetAllExercisesAsync();
            // FIX: Build a dictionary for O(1) lookup instead of calling
            // FirstOrDefault (O(n)) per set inside the loop.
            var exerciseDict = exercises.ToDictionary(e => e.Id);

            ExerciseGroups.Clear();
            WorkoutName = template.Name;
            Notes = template.Notes;

            foreach (var set in sets)
            {
                if (!exerciseDict.TryGetValue(set.ExerciseId, out var exercise)) continue;

                var existing = ExerciseGroups.FirstOrDefault(g => g.Exercise.Id == exercise.Id);
                if (existing is not null)
                {
                    // FIX 5: CreateWorkoutSet factory method replaces the duplicated
                    // inline WorkoutSet construction + DeleteCommand wiring.
                    var workoutSet = CreateWorkoutSet(
                        exercise, existing, existing.Sets.Count + 1,
                        set.Reps, set.Weight, set.WeightUnit);
                    existing.Sets.Add(workoutSet);
                }
                else
                {
                    var group = new ExerciseGroup(exercise, settingsService.WeightUnit);
                    var workoutSet = CreateWorkoutSet(
                        exercise, group, 1,
                        set.Reps, set.Weight, set.WeightUnit);
                    group.Sets.Add(workoutSet);
                    ExerciseGroups.Add(group);
                }
            }
            UpdateTotals();
        }

        // FIX 5: Factory method centralises WorkoutSet construction and DeleteCommand
        // wiring. Previously this was duplicated verbatim in CopyLastSet,
        // LoadFromTemplate, and LoadFromTemplateSets.
        private static WorkoutSet CreateWorkoutSet(
            Exercise exercise,
            ExerciseGroup group,
            int setNumber,
            int reps,
            double weight,
            string weightUnit)
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
            set.DeleteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(
                () => group.RemoveSet(set));
            return set;
        }

        private void ResetForm()
        {
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

        // FIX 4: Single loop over all sets instead of two separate LINQ passes
        // (Sum over groups + SelectMany+Sum). Both totals computed in one iteration.
        private void UpdateTotals()
        {
            int sets = 0;
            double volume = 0;
            foreach (var group in ExerciseGroups)
            {
                sets += group.Sets.Count;
                foreach (var set in group.Sets)
                    volume += set.Weight * set.Reps;
            }
            TotalSets = sets;
            TotalVolume = volume;
        }

        #endregion
    }
}
