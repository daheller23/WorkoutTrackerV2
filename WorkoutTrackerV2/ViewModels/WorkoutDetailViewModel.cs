using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    // FIX 8: Thin wrapper for a muscle group string that carries pre-computed
    // color and emoji — eliminates MuscleGroupColorConverter and
    // MuscleGroupEmojiConverter on the muscle group pill BindableLayout.
    public sealed class MuscleGroupChipViewModel
    {
        public string Name { get; init; } = string.Empty;
        public string Emoji { get; init; } = string.Empty;
        public string Color { get; init; } = "#1F77F0";
    }

    [QueryProperty(nameof(Session), "Session")]
    public partial class WorkoutDetailViewModel(
        IWorkoutService workoutService,
        ISettingsService settingsService,
        ITemplateService templateService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private WorkoutSession _session = new();
        [ObservableProperty] private ObservableCollection<ExerciseGroup> _exerciseGroups = [];
        [ObservableProperty] private int _totalSets;
        [ObservableProperty] private double _totalVolume;
        [ObservableProperty] private int _totalReps;
        [ObservableProperty] private string _weightUnitLabel = "lbs";
        // FIX 8: Chip VMs replace List<string> so XAML needs no converters.
        [ObservableProperty] private List<MuscleGroupChipViewModel> _muscleGroupChips = [];
        [ObservableProperty] private string _volumeComparison = string.Empty;
        [ObservableProperty] private bool _volumeIsUp;
        [ObservableProperty] private bool _hasVolumeComparison;
        // FIX 10: Pre-computed color string for VolumeComparison label —
        // replaces VolumeComparisonColorConverter.
        [ObservableProperty] private string _volumeComparisonColor = "#4CAF50";
        #endregion

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading || Session?.Id == 0) return;
            try
            {
                IsLoading = true;
                WeightUnitLabel = settingsService.WeightUnit;

                // Reload session from DB to get latest fields.
                var freshSession = await workoutService.GetSessionAsync(Session.Id);
                if (freshSession is not null)
                    Session = freshSession;

                // Fetch sets, exercises, and all set history concurrently.
                // FIX 4: GetAllSetsAsync(0) replaces N per-exercise
                // GetExerciseHistoryAsync calls for PR marking.
                var setsTask = workoutService.GetSetsForSessionAsync(Session.Id);
                var exercisesTask = workoutService.GetAllExercisesAsync();
                var allSetsTask = workoutService.GetAllSetsAsync(0);

                await Task.WhenAll(setsTask, exercisesTask, allSetsTask);

                var sets = setsTask.Result;
                var exerciseDict = exercisesTask.Result.ToDictionary(e => e.Id);

                // FIX 4: Build per-exercise all-time max weight map from the
                // bulk fetch — no per-exercise DB queries.
                var allTimeMaxByExercise = allSetsTask.Result
                    .GroupBy(s => s.ExerciseId)
                    .ToDictionary(g => g.Key, g => g.Max(s => s.Weight));

                // Build exercise groups and mark PRs in a single pass.
                var groups = new List<ExerciseGroup>();
                foreach (var set in sets)
                {
                    if (!exerciseDict.TryGetValue(set.ExerciseId, out var exercise)) continue;
                    set.Exercise = exercise;

                    // Mark PR inline while building groups — no second loop needed.
                    if (allTimeMaxByExercise.TryGetValue(set.ExerciseId, out var maxW) && maxW > 0)
                        set.IsPR = set.Weight >= maxW;

                    var existing = groups.FirstOrDefault(g => g.Exercise.Id == set.ExerciseId);
                    if (existing is not null)
                        existing.Sets.Add(set);
                    else
                    {
                        var group = new ExerciseGroup(exercise);
                        if (allTimeMaxByExercise.TryGetValue(set.ExerciseId, out var maxWght))
                        {
                            group.MaxWeight = maxWght;
                        }
                        group.Sets.Add(set);
                        groups.Add(group);
                    }
                }

                // FIX 1: Assign the whole collection at once — one CollectionChanged
                // notification instead of Clear() + N individual Add() calls.
                ExerciseGroups = new ObservableCollection<ExerciseGroup>(groups);

                // FIX 2+3: Single loop computes TotalSets, TotalVolume, TotalReps,
                // and collects distinct muscle groups — replaces four separate LINQ
                // passes (Sum, SelectMany+Sum, SelectMany+Sum, Select+Distinct+OrderBy).
                int totalSets = 0;
                double totalVolume = 0;
                int totalReps = 0;
                var muscleGroupSet = new HashSet<string>();

                foreach (var g in groups)
                {
                    totalSets += g.Sets.Count;
                    muscleGroupSet.Add(g.Exercise.MuscleGroup);
                    foreach (var s in g.Sets)
                    {
                        totalVolume += s.Weight * s.Reps;
                        totalReps += s.Reps;
                    }
                }

                TotalSets = totalSets;
                TotalVolume = totalVolume;
                TotalReps = totalReps;

                await Shell.Current.DisplayAlertAsync("TEST", $"TotalReps: {TotalReps}\n" +
                                                              $"TotalSets: {TotalSets}\n" +
                                                              $"TotalVolume: {TotalVolume}\n" +
                                                              $"Group Count: {groups.Count}\n" +
                                                              $"MuscleGroupSet: {muscleGroupSet.Count}\n", "OK");

                // FIX 8: Build chip VMs with pre-computed color and emoji.
                MuscleGroupChips = muscleGroupSet
                    .OrderBy(m => m)
                    .Select(m => new MuscleGroupChipViewModel
                    {
                        Name = m,
                        Color = m switch
                        {
                            "Chest" => "#4A90D9",
                            "Back" => "#27AE60",
                            "Legs" => "#E67E22",
                            "Shoulders" => "#8E44AD",
                            "Arms" => "#E74C3C",
                            "Core" => "#5DADE2",
                            _ => "#1F77F0"
                        },
                        Emoji = m switch
                        {
                            "Chest" => "🔵",
                            "Back" => "🟢",
                            "Legs" => "🟠",
                            "Shoulders" => "🟣",
                            "Arms" => "🔴",
                            "Core" => "🩵",
                            _ => "⭐"
                        }
                    })
                    .ToList();

                // FIX 5: Pass already-loaded sessions to avoid a second full table
                // load inside LoadVolumeComparison.
                var allSessions = await workoutService.GetAllSessionsAsync();
                await LoadVolumeComparison(allSessions);
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
        #endregion

        #region "LOAD VOLUME COMPARISON"
        // FIX 5: Accepts pre-loaded sessions — no GetAllSessionsAsync call inside.
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
                // FIX 10: Set color directly — no VolumeComparisonColorConverter needed.
                VolumeComparisonColor = diff >= 0 ? "#4CAF50" : "#FF6B6B";
                HasVolumeComparison = true;
            }
            catch
            {
                HasVolumeComparison = false;
            }
        }
        #endregion

        #region "DO WORKOUT AGAIN"
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
        #endregion

        #region "GO BACK"
        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
        #endregion

        #region "EDIT WORKOUT"
        [RelayCommand]
        private async Task EditWorkout()
        {
            await Shell.Current.GoToAsync(Routes.EditWorkout, new Dictionary<string, object>
            {
                { "Session", Session }
            });
        }
        #endregion

        #region "DELETE WORKOUT"
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
        #endregion
    }
}
