using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class WorkoutHistoryViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        #region "PRIVATE VARIABLES"
        private List<WorkoutSessionDetail> _allSessions = [];
        #endregion

        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ObservableCollection<WorkoutSessionGroup> _groupedSessions = [];
        [ObservableProperty] private bool _isRefreshing;
        [ObservableProperty] private int _selectedDays = 30;
        [ObservableProperty] private string _sessionCountLabel = string.Empty;
        #endregion

        #region "PARTIAL METHODS"
        partial void OnSelectedDaysChanged(int value) => LoadSessionsCommand.Execute(null);
        #endregion

        #region "LOAD SESSIONS"
        [RelayCommand]
        private async Task LoadSessions()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                _allSessions.Clear();

                var startDate = SelectedDays == 0 ? DateTime.MinValue : DateTime.Now.AddDays(-SelectedDays).Date;
                var endDate = DateTime.Now.Date.AddDays(1);
                var allSessions = await workoutService.GetSessionsAsync(startDate, endDate);

                var setTasks = allSessions.Select(s => workoutService.GetSetsForSessionAsync(s.Id)).ToList();
                var allSets = await Task.WhenAll(setTasks);

                var exercises = await workoutService.GetAllExercisesAsync();
                var exerciseDict = exercises.ToDictionary(e => e.Id);

                for (int i = 0; i < allSessions.Count; i++)
                {
                    var sets = allSets[i];
                    var topMuscleGroup = sets
                        .Where(s => exerciseDict.ContainsKey(s.ExerciseId))
                        .GroupBy(s => exerciseDict[s.ExerciseId].MuscleGroup)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? string.Empty;

                    _allSessions.Add(new WorkoutSessionDetail
                    {
                        Session = allSessions[i],
                        SetCount = sets.Count,
                        TotalReps = sets.Sum(s => s.Reps),
                        TotalWeight = sets.Sum(s => s.Weight * s.Reps),
                        Sets = sets,
                        MuscleGroup = topMuscleGroup
                    });
                }

                SessionCountLabel = _allSessions.Count == 1 ? "1 workout" : $"{_allSessions.Count} workouts";
                RebuildGroups();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }
        #endregion

        #region "REBUILD GROUPS"
        private void RebuildGroups()
        {
            var today = DateTime.Today;
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var lastWeekStart = thisWeekStart.AddDays(-7);

            var grouped = _allSessions
                .GroupBy(s =>
                {
                    var date = s.Session.Date.Date;
                    if (date >= thisWeekStart) return "This Week";
                    if (date >= lastWeekStart) return "Last Week";
                    return date.ToString("MMMM yyyy");
                })
                .Select(g =>
                {
                    var existing = GroupedSessions.FirstOrDefault(gs => gs.Title == g.Key);
                    var isExpanded = existing?.IsExpanded ?? true;
                    var subtitle = g.Count() == 1 ? "1 workout" : $"{g.Count()} workouts";
                    return new WorkoutSessionGroup(g.Key, subtitle, isExpanded ? g.ToList() : [])
                    {
                        IsExpanded = isExpanded
                    };
                })
                .ToList();

            GroupedSessions = new ObservableCollection<WorkoutSessionGroup>(grouped);
        }
        #endregion

        #region "TOGGLE GROUP"
        [RelayCommand]
        private void ToggleGroup(WorkoutSessionGroup group)
        {
            group.IsExpanded = !group.IsExpanded;
            var index = GroupedSessions.IndexOf(group);
            if (index < 0) return;

            var allInGroup = _allSessions.Where(s =>
            {
                var date = s.Session.Date.Date;
                var today = DateTime.Today;
                var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
                var lastWeekStart = thisWeekStart.AddDays(-7);
                string key = date >= thisWeekStart ? "This Week"
                    : date >= lastWeekStart ? "Last Week"
                    : date.ToString("MMMM yyyy");
                return key == group.Title;
            }).ToList();

            var updated = new WorkoutSessionGroup(
                group.Title, group.Subtitle,
                group.IsExpanded ? allInGroup : [])
            {
                IsExpanded = group.IsExpanded
            };

            GroupedSessions.RemoveAt(index);
            GroupedSessions.Insert(index, updated);
        }
        #endregion

        #region "SET TIME PERIOD"
        [RelayCommand]
        private void SetTimePeriod(string days)
        {
            if (int.TryParse(days, out int result))
                SelectedDays = result;
        }
        #endregion

        #region "DELETE SESSION"
        [RelayCommand]
        private async Task DeleteSession(WorkoutSessionDetail detail)
        {
            if (detail?.Session == null) return;

            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Delete Workout",
                $"Are you sure you want to delete '{detail.Session.DayName}'?",
                "Yes", "No");

            if (!confirmed) return;

            try
            {
                foreach (var set in detail.Sets)
                    await workoutService.DeleteSetAsync(set);
                await workoutService.DeleteSessionAsync(detail.Session);

                var toRemove = _allSessions.FirstOrDefault(s => s.Session.Id == detail.Session.Id);
                if (toRemove is not null)
                    _allSessions.Remove(toRemove);

                SessionCountLabel = _allSessions.Count == 1 ? "1 workout" : $"{_allSessions.Count} workouts";
                RebuildGroups();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
        #endregion

        #region "VIEW WORKOUT"
        [RelayCommand]
        private static async Task ViewWorkout(WorkoutSessionDetail detail)
        {
            await Shell.Current.GoToAsync(Routes.WorkoutDetail, new Dictionary<string, object>
            {
                { "Session", detail.Session }
            });
        }
        #endregion
    }
}