using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
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
        [ObservableProperty] private List<string> _muscleGroups = [];
        [ObservableProperty] private string _volumeComparison = string.Empty;
        [ObservableProperty] private bool _volumeIsUp;
        [ObservableProperty] private bool _hasVolumeComparison;
        #endregion

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading || Session?.Id == 0) return;
            try
            {
                IsLoading = true;

                // Reload session from DB to get latest fields
                var freshSession = await workoutService.GetSessionAsync(Session.Id);
                if (freshSession is not null)
                    Session = freshSession;

                var setsTask = workoutService.GetSetsForSessionAsync(Session.Id);
                var exercisesTask = workoutService.GetAllExercisesAsync();
                await Task.WhenAll(setsTask, exercisesTask);

                var sets = setsTask.Result;
                var exerciseDict = exercisesTask.Result.ToDictionary(e => e.Id);

                ExerciseGroups.Clear();
                foreach (var set in sets)
                {
                    if (!exerciseDict.TryGetValue(set.ExerciseId, out var exercise)) continue;
                    set.Exercise = exercise;
                    var existing = ExerciseGroups.FirstOrDefault(g => g.Exercise.Id == set.ExerciseId);
                    if (existing is not null)
                        existing.Sets.Add(set);
                    else
                    {
                        var group = new ExerciseGroup(set.Exercise);
                        group.Sets.Add(set);
                        ExerciseGroups.Add(group);
                    }
                }

                TotalSets = ExerciseGroups.Sum(g => g.Sets.Count);
                TotalVolume = ExerciseGroups.SelectMany(g => g.Sets).Sum(s => s.Weight * s.Reps);
                TotalReps = ExerciseGroups.SelectMany(g => g.Sets).Sum(s => s.Reps);
                WeightUnitLabel = settingsService.WeightUnit;

                // Muscle group breakdown
                MuscleGroups = ExerciseGroups
                    .Select(g => g.Exercise.MuscleGroup)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToList();

                // Volume comparison
                await LoadVolumeComparison();
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
        private async Task LoadVolumeComparison()
        {
            try
            {
                var allSessions = await workoutService.GetAllSessionsAsync();
                var previousSession = allSessions
                    .Where(s => s.Id != Session.Id
                        && s.Date < Session.Date
                        && s.DayName == Session.DayName) // 👈 only same workout type
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
                // Build an in-memory template without saving to DB
                var template = new WorkoutTemplate
                {
                    Id = -1, // sentinel value so template service knows it's temporary
                    Name = Session.DayName,
                    Notes = string.Empty
                };

                // Store sets directly on the template service
                // We need a different approach — store exercise groups directly
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
                var sets = await workoutService.GetSetsForSessionAsync(Session.Id);
                foreach (var set in sets)
                    await workoutService.DeleteSetAsync(set.Id);
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