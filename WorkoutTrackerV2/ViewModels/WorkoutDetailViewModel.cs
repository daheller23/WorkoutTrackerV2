using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public sealed class MuscleGroupChipViewModel
    {
        public string Name { get; init; } = string.Empty;
        public string Emoji { get; init; } = string.Empty;
        public string Color { get; init; } = "#1F77F0";
        public static MuscleGroupChipViewModel FromName(string name)
        {
            return name switch
            {
                "Chest" => new() { Name = name, Color = "#4A90D9", Emoji = "🔵" },
                "Back" => new() { Name = name, Color = "#27AE60", Emoji = "🟢" },
                "Legs" => new() { Name = name, Color = "#E67E22", Emoji = "🟠" },
                "Shoulders" => new() { Name = name, Color = "#8E44AD", Emoji = "🟣" },
                "Arms" => new() { Name = name, Color = "#E74C3C", Emoji = "🔴" },
                "Core" => new() { Name = name, Color = "#5DADE2", Emoji = "🩵" },
                _ => new() { Name = name, Color = "#1F77F0", Emoji = "⭐" }
            };
        }
    }

    [QueryProperty(nameof(Session), "Session")]
    public partial class WorkoutDetailViewModel(IWorkoutService workoutService, ISettingsService settingsService, ITemplateService templateService) : BaseViewModel
    {
        [ObservableProperty] private List<MuscleGroupChipViewModel> _muscleGroupChips = [];
        [ObservableProperty] private WorkoutSession _session = new();
        [ObservableProperty] private ObservableCollection<ExerciseGroup> _exerciseGroups = [];

        [ObservableProperty] private int _totalReps;
        [ObservableProperty] private int _totalSets;

        [ObservableProperty] private double _totalVolume;
        
        [ObservableProperty] private string _weightUnitLabel = "lbs";
        [ObservableProperty] private string _volumeComparison = string.Empty;
        [ObservableProperty] private string _volumeComparisonColor = "#4CAF50";

        [ObservableProperty] private bool _volumeIsUp;
        [ObservableProperty] private bool _hasVolumeComparison;

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading || Session?.Id == 0) return;

            try
            {
                IsLoading = true;
                WeightUnitLabel = settingsService.WeightUnit;

                var freshSessionTask = workoutService.GetSessionAsync(Session.Id);
                var setsTask = workoutService.GetSetsForSessionAsync(Session.Id);
                var exercisesTask = workoutService.GetAllExercisesAsync();

                await Task.WhenAll(freshSessionTask, setsTask, exercisesTask);

                if (freshSessionTask.Result is not null)
                    Session = freshSessionTask.Result;

                var currentSets = setsTask.Result;
                var exerciseDict = exercisesTask.Result.ToDictionary(e => e.Id);
                var exerciseIds = currentSets.Select(s => s.ExerciseId).Distinct().ToList();

                var prMapTask = workoutService.GetPersonalRecordsAsync(exerciseIds);
                var prevSessionTask = workoutService.GetPreviousSessionByDayAsync(Session.Id, Session.DayName, Session.Date);

                await Task.WhenAll(prMapTask, prevSessionTask);
                var prMap = prMapTask.Result;

                var groupsDict = new Dictionary<int, ExerciseGroup>();
                var muscleGroupNames = new HashSet<string>();
                int tSets = 0, tReps = 0;
                double tVol = 0;

                foreach (var set in currentSets)
                {
                    if (!exerciseDict.TryGetValue(set.ExerciseId, out var exercise)) continue;

                    tSets++;
                    tReps += set.Reps;
                    tVol += set.Weight * set.Reps;
                    muscleGroupNames.Add(exercise.MuscleGroup);

                    set.Exercise = exercise;

                    if (prMap.TryGetValue(set.ExerciseId, out var maxWeight))
                        set.IsPR = set.Weight >= maxWeight;

                    if (!groupsDict.TryGetValue(set.ExerciseId, out var group))
                    {
                        group = new ExerciseGroup(exercise) { MaxWeight = maxWeight };
                        groupsDict.Add(set.ExerciseId, group);
                    }

                    group.Sets.Add(set);
                    group.TotalReps += set.Reps;
                }

                ExerciseGroups = new ObservableCollection<ExerciseGroup>(groupsDict.Values);
                TotalSets = tSets;
                TotalReps = tReps;
                TotalVolume = tVol;

                MuscleGroupChips = muscleGroupNames
                    .OrderBy(m => m)
                    .Select(MuscleGroupChipViewModel.FromName)
                    .ToList();

                if (prevSessionTask.Result != null)
                    await LoadVolumeComparison(new List<WorkoutSession> { prevSessionTask.Result });
                else
                    HasVolumeComparison = false;
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

        [RelayCommand]
        private async Task DoWorkoutAgain()
        {
            try
            {
                var template = new WorkoutTemplate
                {
                    Id = -1,
                    Name = Session.DayName,
                    Notes = string.Empty
                };

                templateService.PendingTemplate = template;
                templateService.PendingTemplateSets = ExerciseGroups
                    .SelectMany(g => g.Sets.Select(s => new WorkoutTemplateSet
                    {
                        TemplateId = -1,
                        ExerciseId = g.Exercise.Id,
                        Reps = s.Reps,
                        Weight = s.Weight,
                        WeightUnit = s.WeightUnit
                    }))
                    .ToList();

                await Shell.Current.GoToAsync(Routes.Workout);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);

        [RelayCommand]
        private async Task EditWorkout()
        {
            await Shell.Current.GoToAsync(Routes.EditWorkout, new Dictionary<string, object>
            {
                { "Session", Session }
            });
        }

        [RelayCommand]
        private async Task DeleteWorkout()
        {
            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Delete Workout",
                $"Are you sure you want to delete '{Session.DayName}'?",
                "Yes", "No");
            if (!confirmed) return;

            bool doubleConfirmed = await Shell.Current.DisplayAlertAsync(
                "Are you sure?",
                "This cannot be undone.",
                "Yes, delete", "Cancel");
            if (!doubleConfirmed) return;

            try
            {
                IsLoading = true;
                // FIX 6: Single DELETE WHERE replaces N sequential DeleteSetAsync
                // calls — one DB round-trip instead of one per set.
                await workoutService.DeleteSetsForSessionAsync(Session.Id);
                await workoutService.DeleteSessionAsync(Session);
                await Shell.Current.GoToAsync(Routes.Back);
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

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================

        private async Task LoadVolumeComparison(List<WorkoutSession> allSessions)
        {
            try
            {
                var previousSession = allSessions
                    .Where(s => s.Id != Session.Id
                             && s.Date < Session.Date
                             && s.DayName == Session.DayName)
                    .OrderByDescending(s => s.Date)
                    .FirstOrDefault();

                if (previousSession is null)
                {
                    HasVolumeComparison = false;
                    return;
                }

                var previousSets = await workoutService.GetSetsForSessionAsync(previousSession.Id);
                var previousVolume = previousSets.Sum(s => s.Weight * s.Reps);

                if (previousVolume == 0)
                {
                    HasVolumeComparison = false;
                    return;
                }

                var diff = TotalVolume - previousVolume;
                var percent = (diff / previousVolume) * 100;
                VolumeIsUp = diff >= 0;
                var sign = diff >= 0 ? "+" : "";
                VolumeComparison = $"{sign}{diff:F0} {settingsService.WeightUnit} ({sign}{percent:F0}%) vs last {Session.DayName}";
                VolumeComparisonColor = diff >= 0 ? "#4CAF50" : "#FF6B6B";
                HasVolumeComparison = true;
            }
            catch
            {
                HasVolumeComparison = false;
            }
        }
    }
}
