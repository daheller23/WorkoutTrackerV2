using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class HomeViewModel(
        IWorkoutService workoutService,
        IAnalyticsService analyticsService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private double _averageDuration;
        [ObservableProperty] private int _currentStreak;
        [ObservableProperty] private DateTime? _lastWorkoutDate;
        [ObservableProperty] private ObservableCollection<WorkoutSession> _recentSessions = [];
        [ObservableProperty] private int _totalWorkouts;
        [ObservableProperty] private string _motivationalMessage = string.Empty;
        [ObservableProperty] private string _motivationalSubMessage = string.Empty;
        [ObservableProperty] private WorkoutSession? _lastWorkoutSession;
        [ObservableProperty] private string _mostTrainedMuscleGroup = string.Empty;
        [ObservableProperty] private string _streakSubtitle = string.Empty;
        [ObservableProperty] private int _workoutsThisWeek;
        [ObservableProperty] private int _setsThisWeek;
        [ObservableProperty] private double _volumeThisWeek;
        [ObservableProperty] private string _mostTrainedMuscleGroupColor = "#1F77F0";
        #endregion

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;

                // Fire all independent DB queries concurrently.
                var totalWorkoutsTask = workoutService.GetTotalWorkoutCountAsync();
                var currentStreakTask = analyticsService.GetCurrentStreak();
                var lastWorkoutDateTask = workoutService.GetLastWorkoutDateAsync();
                var averageDurationTask = analyticsService.GetAverageWorkoutDurationAsync();
                var allSessionsTask = workoutService.GetAllSessionsAsync();
                var allExercisesTask = workoutService.GetAllExercisesAsync();

                await Task.WhenAll(
                    totalWorkoutsTask, currentStreakTask, lastWorkoutDateTask,
                    averageDurationTask, allSessionsTask, allExercisesTask);

                TotalWorkouts = totalWorkoutsTask.Result;
                CurrentStreak = currentStreakTask.Result;
                LastWorkoutDate = lastWorkoutDateTask.Result;
                AverageDuration = averageDurationTask.Result;

                var allSessions = allSessionsTask.Result;
                // FIX: Build exercise lookup once here and pass it down so
                // LoadMostTrainedMuscleGroup doesn't repeat the full table fetch.
                var exerciseDict = allExercisesTask.Result.ToDictionary(e => e.Id);

                // Last workout shown in its own card.
                LastWorkoutSession = allSessions.FirstOrDefault();

                // Next 3 sessions shown in the Recent Activity list.
                // FIX: Replace Clear()+foreach with a single collection swap to
                // fire one CollectionChanged notification instead of N+1.
                var recent = allSessions.Skip(1).Take(3).ToList();
                RecentSessions = new ObservableCollection<WorkoutSession>(recent);

                // Calculate week boundaries once and reuse across both stat blocks.
                var today = DateTime.Today;
                var calWeekStart = today.AddDays(-(int)today.DayOfWeek);   // Sun–Sat week
                var rollingWeekStart = today.AddDays(-7);                   // rolling 7 days

                // Identify sessions in each window.
                var calWeekSessions = allSessions.Where(s => s.Date >= calWeekStart).ToList();
                var rollingWeekSessions = allSessions.Where(s => s.Date >= rollingWeekStart).ToList();

                // FIX: Fetch sets for the UNION of both windows in one fan-out so
                // sessions that fall in both ranges are only fetched once.
                // Use a HashSet to deduplicate session Ids across both windows.
                var allRelevantIds = calWeekSessions
                    .Select(s => s.Id)
                    .Union(rollingWeekSessions.Select(s => s.Id))
                    .ToHashSet();

                var allRelevantSessions = allSessions
                    .Where(s => allRelevantIds.Contains(s.Id))
                    .ToList();

                var setTasks = allRelevantSessions
                    .Select(s => workoutService.GetSetsForSessionAsync(s.Id))
                    .ToList();
                var allSetsArrays = await Task.WhenAll(setTasks);

                // Build a per-session lookup so we can slice by window without re-fetching.
                // Explicitly typed to IEnumerable<WorkoutSet> so the dictionary is compatible
                // regardless of whether GetSetsForSessionAsync returns List, Array, or IEnumerable.
                var setsBySessionId = allRelevantSessions
                    .Zip(allSetsArrays, (session, sets) => (session.Id, sets))
                    .ToDictionary(x => x.Id, x => (IEnumerable<WorkoutSet>)x.sets);

                // ── This week stats (calendar week) ──────────────────────────
                var calWeekSets = calWeekSessions
                    .Where(s => setsBySessionId.ContainsKey(s.Id))
                    .SelectMany(s => setsBySessionId[s.Id])
                    .ToList();

                WorkoutsThisWeek = calWeekSessions.Count;
                SetsThisWeek = calWeekSets.Count;
                VolumeThisWeek = calWeekSets.Sum(s => s.Weight * s.Reps);

                // ── Most trained muscle group (rolling 7 days) ────────────────
                ComputeMostTrainedMuscleGroup(rollingWeekSessions, setsBySessionId, exerciseDict);

                // ── Motivational message (pure CPU, no await needed) ──────────
                SetMotivationalMessage();
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
        #endregion

        #region "MOST TRAINED MUSCLE GROUP"
        // FIX: Now synchronous — all data is pre-fetched in LoadData.
        // No longer triggers a second GetAllExercisesAsync call or its own
        // GetSetsForSessionAsync fan-out.
        private void ComputeMostTrainedMuscleGroup(
            List<WorkoutSession> sessions,
            Dictionary<int, IEnumerable<WorkoutSet>> setsBySessionId,
            Dictionary<int, Exercise> exerciseDict)
        {
            try
            {
                if (sessions.Count == 0)
                {
                    MostTrainedMuscleGroup = string.Empty;
                    return;
                }

                var flatSets = sessions
                    .Where(s => setsBySessionId.ContainsKey(s.Id))
                    .SelectMany(s => setsBySessionId[s.Id])
                    .ToList();

                if (flatSets.Count == 0)
                {
                    MostTrainedMuscleGroup = string.Empty;
                    return;
                }

                var topGroup = flatSets
                    .Where(s => exerciseDict.ContainsKey(s.ExerciseId))
                    .GroupBy(s => exerciseDict[s.ExerciseId].MuscleGroup)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? string.Empty;

                MostTrainedMuscleGroup = topGroup;

                MostTrainedMuscleGroupColor = topGroup switch
                {
                    "Chest" => "#4A90D9",
                    "Back" => "#4CAF50",
                    "Legs" => "#FF9800",
                    "Shoulders" => "#9C27B0",
                    "Arms" => "#FF6B6B",
                    "Core" => "#00BCD4",
                    _ => "#1F77F0"
                };
            }
            catch
            {
                MostTrainedMuscleGroup = string.Empty;
            }
        }
        #endregion

        #region "MOTIVATIONAL MESSAGE"
        private void SetMotivationalMessage()
        {
            var today = DateTime.Today;
            var daysSinceLastWorkout = LastWorkoutDate.HasValue
                ? (today - LastWorkoutDate.Value.Date).Days
                : 999;

            var hour = DateTime.Now.Hour;
            var timeGreeting = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";

            // FIX: Assign both message properties in one logical block.
            // Each assignment fires a property-changed notification; grouping them
            // here makes the intent clear and keeps the UI update atomic.
            (MotivationalMessage, MotivationalSubMessage) = daysSinceLastWorkout switch
            {
                0 => ("Great work today! 🔥",
                      "You already crushed a session. Rest up or go again!"),
                1 => ($"{timeGreeting}! Ready to go? 💪",
                      "Yesterday's session was great. Keep the momentum going!"),
                2 => ("Time to get back at it! 🏋️",
                      "It's been 2 days. Your muscles are rested and ready."),
                <= 5 => ("Don't break the habit! ⚡",
                         $"{daysSinceLastWorkout} days since your last session. Let's go!"),
                _ when TotalWorkouts == 0
                      => ("Welcome! Let's get started 🚀",
                          "Log your first workout to start tracking your progress."),
                _ => ("Welcome back! 👋",
                          "It's been a while. Every session counts — let's go!")
            };

            StreakSubtitle = CurrentStreak switch
            {
                0 => "Start your streak today",
                1 => "1 day — keep it going!",
                >= 7 => $"{CurrentStreak} days — you're on fire! 🔥",
                _ => $"{CurrentStreak} days in a row"
            };
        }
        #endregion

        #region "COMMANDS"
        [RelayCommand]
        private static Task ViewPersonalRecords() => Shell.Current.GoToAsync(Routes.PersonalRecords);

        [RelayCommand]
        private static Task StartWorkout() => Shell.Current.GoToAsync(Routes.Workout);

        [RelayCommand]
        private static Task ViewHistory() => Shell.Current.GoToAsync(Routes.History);

        [RelayCommand]
        private static Task ViewAnalytics() => Shell.Current.GoToAsync(Routes.Analytics);

        [RelayCommand]
        private static Task ViewWorkout(WorkoutSession session) =>
            Shell.Current.GoToAsync(Routes.WorkoutDetail, new Dictionary<string, object>
            {
                { "Session", session }
            });

        [RelayCommand]
        private static Task ViewSettings() => Shell.Current.GoToAsync(Routes.Settings);
        #endregion
    }
}
