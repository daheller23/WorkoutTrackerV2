using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(SelectedExercise), "SelectedExercise")]
    public partial class AddWorkoutViewModel(IWorkoutService workoutService, ITemplateService templateService, ISettingsService settingsService) : BaseViewModel
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
        [ObservableProperty] private string _weightUnitLabel = "lbs total";
        #endregion

        [RelayCommand]
        private void PrepareForTemplatePicker()
        {
            _ignoreNextExerciseSelection = true;
        }

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
                LoadFromTemplateCommand.Execute(template);
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
                int setNumber = 1;

                foreach (var group in ExerciseGroups)
                    foreach (var set in group.Sets)
                        await workoutService.SaveTemplateSetAsync(new WorkoutTemplateSet
                        {
                            TemplateId = templateId,
                            ExerciseId = group.Exercise.Id,
                            SetNumber = setNumber++,
                            Reps = set.Reps,
                            Weight = set.Weight,
                            WeightUnit = set.WeightUnit
                        });

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
        {
            try
            {
                var sets = await workoutService.GetTemplateSetsAsync(template.Id);
                var exercises = await workoutService.GetAllExercisesAsync();

                ExerciseGroups.Clear();
                WorkoutName = template.Name;
                Notes = template.Notes;

                foreach (var set in sets)
                {
                    var exercise = exercises.FirstOrDefault(e => e.Id == set.ExerciseId);
                    if (exercise is null) continue;

                    var existing = ExerciseGroups.FirstOrDefault(g => g.Exercise.Id == exercise.Id);
                    if (existing is not null)
                    {
                        var workoutSet = new WorkoutSet
                        {
                            Exercise = exercise,
                            ExerciseId = exercise.Id,
                            SetNumber = existing.Sets.Count + 1,
                            Reps = set.Reps,
                            Weight = set.Weight,
                            WeightUnit = set.WeightUnit,
                            ParentGroup = existing
                        };
                        workoutSet.DeleteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => existing.RemoveSet(workoutSet));
                        existing.Sets.Add(workoutSet);
                    }
                    else
                    {
                        var group = new ExerciseGroup(exercise);
                        var workoutSet = new WorkoutSet
                        {
                            Exercise = exercise,
                            ExerciseId = exercise.Id,
                            SetNumber = 1,
                            Reps = set.Reps,
                            Weight = set.Weight,
                            WeightUnit = set.WeightUnit,
                            ParentGroup = group
                        };
                        workoutSet.DeleteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => group.RemoveSet(workoutSet));
                        group.Sets.Add(workoutSet);
                        ExerciseGroups.Add(group);
                    }
                }
                UpdateTotals();
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

            // Validate reps and weight
            var invalidSets = allSets.Where(s => s.Reps <= 0 || s.Weight < 0).ToList();
            if (invalidSets.Count > 0)
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
                    foreach (var set in group.Sets)
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

        #region "RESET FORM"
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
                var exercises = await workoutService.GetAllExercisesAsync();
                ExerciseGroups.Clear();
                WorkoutName = args.template.Name;
                Notes = args.template.Notes;

                foreach (var set in args.sets)
                {
                    var exercise = exercises.FirstOrDefault(e => e.Id == set.ExerciseId);
                    if (exercise is null) continue;

                    var existing = ExerciseGroups.FirstOrDefault(g => g.Exercise.Id == exercise.Id);
                    if (existing is not null)
                    {
                        var workoutSet = new WorkoutSet
                        {
                            Exercise = exercise,
                            ExerciseId = exercise.Id,
                            SetNumber = existing.Sets.Count + 1,
                            Reps = set.Reps,
                            Weight = set.Weight,
                            WeightUnit = set.WeightUnit,
                            ParentGroup = existing
                        };
                        workoutSet.DeleteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => existing.RemoveSet(workoutSet));
                        existing.Sets.Add(workoutSet);
                    }
                    else
                    {
                        var group = new ExerciseGroup(exercise, settingsService.WeightUnit);
                        var workoutSet = new WorkoutSet
                        {
                            Exercise = exercise,
                            ExerciseId = exercise.Id,
                            SetNumber = 1,
                            Reps = set.Reps,
                            Weight = set.Weight,
                            WeightUnit = set.WeightUnit,
                            ParentGroup = group
                        };
                        workoutSet.DeleteCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => group.RemoveSet(workoutSet));
                        group.Sets.Add(workoutSet);
                        ExerciseGroups.Add(group);
                    }
                }
                UpdateTotals();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
        #endregion

        [RelayCommand]
        private void ClearSelectedExercise()
        {
            SelectedExercise = null;
        }
    }
}