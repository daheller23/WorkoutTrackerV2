using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class WorkoutHistoryViewModel(IWorkoutService workoutService) : BaseViewModel
    {
        private List<WorkoutSessionDetail> _allSessions = [];

        public List<TimePeriodPillViewModel> TimePeriodPills { get; } =
        [
            new() { Label = "All", Days = 0  },
            new() { Label = "7d",  Days = 7  },
            new() { Label = "14d", Days = 14 },
            new() { Label = "30d", Days = 30, IsSelected = true },
            new() { Label = "60d", Days = 60 },
            new() { Label = "90d", Days = 90 },
        ];

        [ObservableProperty] private int _selectedDays = 30;

        [ObservableProperty] private string _searchQuery = string.Empty;
        [ObservableProperty] private string _sessionCountLabel = string.Empty;

        [ObservableProperty] private bool _isRefreshing;

        public ObservableCollection<WorkoutSessionGroup> GroupedSessions { get; } = [];

        // ==============================================================================================================
        //
        //      PARTIAL METHODS
        //
        // ==============================================================================================================

        partial void OnSelectedDaysChanged(int value)
        {
            foreach (var pill in TimePeriodPills)
            {
                pill.IsSelected = pill.Days == value;
            }              
            _ = LoadSessionsAsync();
        }

        partial void OnSearchQueryChanged(string value)
        {
            RebuildGroups();
        }

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private void SetTimePeriod(string days)
        {
            if (int.TryParse(days, out int result))
            {
                SelectedDays = result;
            }               
        }

        [RelayCommand]
        private async Task LoadSessions() => await LoadSessionsAsync();

        [RelayCommand]
        private void ToggleGroup(WorkoutSessionGroup group)
        {
            group.IsExpanded = !group.IsExpanded;
            var index = GroupedSessions.IndexOf(group);
            if (index < 0)
            {
                return;
            }

            var allInGroup = _allSessions
                .Where(s => GetGroupKey(s.Session.Date.Date) == group.Title)
                .ToList();

            var updated = new WorkoutSessionGroup(
                group.Title, group.Subtitle,
                group.IsExpanded ? allInGroup : [])
                {
                    IsExpanded = group.IsExpanded
                };

            GroupedSessions[index] = updated;
        }

        [RelayCommand]
        private async Task DeleteSession(WorkoutSessionDetail detail)
        {
            if (detail?.Session is null)
            {
                return;
            }

            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Delete Workout",
                $"Are you sure you want to delete '{detail.Session.DayName}'?",
                "Yes", "No");
            if (!confirmed)
            {
                return;
            }

            try
            {
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

        [RelayCommand]
        private static async Task ViewWorkout(WorkoutSessionDetail detail)
        {
            await Shell.Current.GoToAsync(Routes.WorkoutDetail, new Dictionary<string, object>
            {
                { "Session", detail.Session }
            });
        }

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================

        private async Task LoadSessionsAsync()
        {
            if (IsLoading) 
            {
                return;
            }

            try
            {
                IsLoading = true;

                var startDate = SelectedDays == 0
                    ? DateTime.MinValue
                    : DateTime.Now.AddDays(-SelectedDays).Date;
                var endDate = DateTime.Now.Date.AddDays(1);

                var allSessions = await workoutService.GetSessionsAsync(startDate, endDate);
                var exercisesTask = workoutService.GetAllExercisesAsync();

                var sessionIds = allSessions.Select(s => s.Id).ToList();
                var allSets = await workoutService.GetSetsForSessionsAsync(sessionIds);

                var setsLookup = allSets.ToLookup(s => s.WorkoutSessionId);

                var exerciseDict = (await exercisesTask).ToDictionary(e => e.Id);

                var sessions = new List<WorkoutSessionDetail>(allSessions.Count);

                foreach (var session in allSessions)
                {
                    var sets = setsLookup[session.Id].ToList();

                    var topMuscleGroup = sets
                        .Where(s => exerciseDict.ContainsKey(s.ExerciseId))
                        .GroupBy(s => exerciseDict[s.ExerciseId].MuscleGroup)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? string.Empty;

                    int reps = 0;
                    double weight = 0;
                    foreach (var s in sets)
                    {
                        reps += s.Reps;
                        weight += s.Weight * s.Reps;
                    }

                    sessions.Add(new WorkoutSessionDetail
                    {
                        Session = session,
                        SetCount = sets.Count,
                        TotalReps = reps,
                        TotalWeight = weight,
                        Sets = sets,
                        MuscleGroup = topMuscleGroup
                    });
                }

                _allSessions = sessions;
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

        private void RebuildGroups()
        {
            var expandedState = GroupedSessions
                .ToDictionary(gs => gs.Title, gs => gs.IsExpanded);

            var filteredSessions = string.IsNullOrWhiteSpace(SearchQuery)
                ? _allSessions
                : _allSessions.Where(s =>
                    (s.Session.DayName?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Session.Notes?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.MuscleGroup?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();

            var grouped = filteredSessions
                .GroupBy(s => GetGroupKey(s.Session.Date.Date))
                .Select(g =>
                {
                    var count = g.Count();
                    var expanded = expandedState.GetValueOrDefault(g.Key, true);
                    var subtitle = count == 1 ? "1 workout" : $"{count} workouts";
                    return new WorkoutSessionGroup(g.Key, subtitle, expanded ? g.ToList() : [])
                    {
                        IsExpanded = expanded
                    };
                })
                .ToList();

            GroupedSessions.Clear(); 
            foreach (var group in grouped)
            {
                GroupedSessions.Add(group); 
            }
        }

        private static string GetGroupKey(DateTime date)
        {
            var today = DateTime.Today;
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var lastWeekStart = thisWeekStart.AddDays(-7);

            if (date >= thisWeekStart) return "This Week";
            if (date >= lastWeekStart) return "Last Week";
            return date.ToString("MMMM yyyy");
        }


    }
}