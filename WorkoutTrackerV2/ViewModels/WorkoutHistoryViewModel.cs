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

        // NEW: Search query property
        [ObservableProperty] private string _searchQuery = string.Empty;

        // FIX 9: Pill VMs — constructed once, IsSelected toggled on SelectedDays change.
        public List<TimePeriodPillViewModel> TimePeriodPills { get; } =
        [
            new() { Label = "All", Days = 0  },
            new() { Label = "7d",  Days = 7  },
            new() { Label = "14d", Days = 14 },
            new() { Label = "30d", Days = 30, IsSelected = true },
            new() { Label = "60d", Days = 60 },
            new() { Label = "90d", Days = 90 },
        ];
        #endregion

        #region "PARTIAL METHODS"
        partial void OnSelectedDaysChanged(int value)
        {
            foreach (var pill in TimePeriodPills)
                pill.IsSelected = pill.Days == value;
            // FIX 1: Call async method directly instead of LoadSessionsCommand.Execute().
            _ = LoadSessionsAsync();
        }

        // NEW: Automatically filters the list when the user types
        partial void OnSearchQueryChanged(string value)
        {
            RebuildGroups();
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

        #region "LOAD SESSIONS"
        [RelayCommand]
        private async Task LoadSessions() => await LoadSessionsAsync();

        private async Task LoadSessionsAsync()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;

                var startDate = SelectedDays == 0
                    ? DateTime.MinValue
                    : DateTime.Now.AddDays(-SelectedDays).Date;
                var endDate = DateTime.Now.Date.AddDays(1);

                var allSessions = await workoutService.GetSessionsAsync(startDate, endDate);
                var exercisesTask = workoutService.GetAllExercisesAsync();

                // Fetch all sets concurrently.
                var setTasks = allSessions
                    .Select(s => workoutService.GetSetsForSessionAsync(s.Id))
                    .ToList();
                var allSets = await Task.WhenAll(setTasks);

                var exerciseDict = (await exercisesTask).ToDictionary(e => e.Id);

                // FIX 2: Build new list directly instead of Clear() + loop Add().
                var sessions = new List<WorkoutSessionDetail>(allSessions.Count);
                for (int i = 0; i < allSessions.Count; i++)
                {
                    var sets = allSets[i];

                    var topMuscleGroup = sets
                        .Where(s => exerciseDict.ContainsKey(s.ExerciseId))
                        .GroupBy(s => exerciseDict[s.ExerciseId].MuscleGroup)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? string.Empty;

                    // FIX 3: Single loop computes TotalReps and TotalWeight instead
                    // of two separate .Sum() passes over the same set list.
                    int reps = 0;
                    double weight = 0;
                    foreach (var s in sets)
                    {
                        reps += s.Reps;
                        weight += s.Weight * s.Reps;
                    }

                    sessions.Add(new WorkoutSessionDetail
                    {
                        Session = allSessions[i],
                        SetCount = sets.Count,
                        TotalReps = reps,
                        TotalWeight = weight,
                        Sets = sets,
                        MuscleGroup = topMuscleGroup
                    });
                }

                _allSessions = sessions;
                SessionCountLabel = _allSessions.Count == 1
                    ? "1 workout"
                    : $"{_allSessions.Count} workouts";

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
            // FIX 8: Build an O(1) lookup for existing IsExpanded state instead
            // of calling FirstOrDefault (O(n)) per group inside the Select.
            var expandedState = GroupedSessions
                .ToDictionary(gs => gs.Title, gs => gs.IsExpanded);

            // UPDATED: Filter the sessions based on the search query
            var filteredSessions = string.IsNullOrWhiteSpace(SearchQuery)
                ? _allSessions
                : _allSessions.Where(s =>
                    (!string.IsNullOrEmpty(s.Session.DayName) && s.Session.DayName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.Session.Notes) && s.Session.Notes.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.MuscleGroup) && s.MuscleGroup.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                ).ToList();

            // Use the filteredSessions instead of _allSessions
            var grouped = filteredSessions
                .GroupBy(s => GetGroupKey(s.Session.Date.Date))
                .Select(g =>
                {
                    // FIX 7: Count() called once, stored in variable — was called
                    // twice per group (once for Count check, once for subtitle).
                    var count = g.Count();
                    var expanded = expandedState.GetValueOrDefault(g.Key, true);
                    var subtitle = count == 1 ? "1 workout" : $"{count} workouts";
                    return new WorkoutSessionGroup(g.Key, subtitle, expanded ? g.ToList() : [])
                    {
                        IsExpanded = expanded
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

            // FIX 4: GetGroupKey shared method replaces duplicated date-bucketing
            // logic that was inlined here and in RebuildGroups separately.
            var allInGroup = _allSessions
                .Where(s => GetGroupKey(s.Session.Date.Date) == group.Title)
                .ToList();

            var updated = new WorkoutSessionGroup(
                group.Title, group.Subtitle,
                group.IsExpanded ? allInGroup : [])
            {
                IsExpanded = group.IsExpanded
            };

            // FIX 5: Use indexer assignment — one CollectionChanged notification
            // instead of RemoveAt + Insert (two notifications).
            GroupedSessions[index] = updated;
        }
        #endregion

        #region "DELETE SESSION"
        [RelayCommand]
        private async Task DeleteSession(WorkoutSessionDetail detail)
        {
            if (detail?.Session is null) return;

            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Delete Workout",
                $"Are you sure you want to delete '{detail.Session.DayName}'?",
                "Yes", "No");
            if (!confirmed) return;

            try
            {
                // FIX 6: DeleteSetsForSessionAsync replaces foreach loop of
                // individual DeleteSetAsync calls — one DB query instead of N.
                await workoutService.DeleteSetsForSessionAsync(detail.Session.Id);
                await workoutService.DeleteSessionAsync(detail.Session);

                _allSessions.RemoveAll(s => s.Session.Id == detail.Session.Id);

                SessionCountLabel = _allSessions.Count == 1
                    ? "1 workout"
                    : $"{_allSessions.Count} workouts";

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

        #region "PRIVATE HELPERS"

        // FIX 4: Single shared method for date-to-group-key logic — eliminates the
        // duplicate implementation that previously existed in RebuildGroups and
        // ToggleGroup independently.
        private static string GetGroupKey(DateTime date)
        {
            var today = DateTime.Today;
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var lastWeekStart = thisWeekStart.AddDays(-7);

            if (date >= thisWeekStart) return "This Week";
            if (date >= lastWeekStart) return "Last Week";
            return date.ToString("MMMM yyyy");
        }

        #endregion
    }
}