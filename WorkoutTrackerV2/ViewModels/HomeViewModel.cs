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

        // PR message set via static field from AddWorkoutViewModel before navigation.
        // Static survives the singleton lifetime — read once in HomeView.OnAppearing
        // then cleared. No QueryProperty, no PropertyChanged, no timing races.
        public static string PendingPrMessage { get; set; } = string.Empty;

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;

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
                var exerciseDict = allExercisesTask.Result.ToDictionary(e => e.Id);

                LastWorkoutSession = allSessions.FirstOrDefault();

                var recent = allSessions.Skip(1).Take(3).ToList();
                RecentSessions = new ObservableCollection<WorkoutSession>(recent);

                var today = DateTime.Today;
                var calWeekStart = today.AddDays(-(int)today.DayOfWeek);
                var rollingWeekStart = today.AddDays(-7);

                var calWeekSessions = allSessions.Where(s => s.Date >= calWeekStart).ToList();
                var rollingWeekSessions = allSessions.Where(s => s.Date >= rollingWeekStart).ToList();

                var allRelevantIds = calWeekSessions
                    .Select(s => s.Id)
                    .Union(rollingWeekSessions.Select(s => s.Id))
                    .ToHashSet();

                var allRelevantSessions = allSessions
                    .Where(s => allRelevantIds.Contains(s.Id))
                    .ToList();

                var setTasks = allRelevantSessions.Select(s => workoutService.GetSetsForSessionAsync(s.Id)).ToList();
                var allSetsArrays = await Task.WhenAll(setTasks);

                var setsBySessionId = allRelevantSessions
                    .Zip(allSetsArrays, (session, sets) => (session.Id, sets))
                    .ToDictionary(x => x.Id, x => (IEnumerable<WorkoutSet>)x.sets);

                var calWeekSets = calWeekSessions
                    .Where(s => setsBySessionId.ContainsKey(s.Id))
                    .SelectMany(s => setsBySessionId[s.Id])
                    .ToList();

                WorkoutsThisWeek = calWeekSessions.Count;
                SetsThisWeek = calWeekSets.Count;
                VolumeThisWeek = calWeekSets.Sum(s => s.Weight * s.Reps);

                ComputeMostTrainedMuscleGroup(rollingWeekSessions, setsBySessionId, exerciseDict);
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
        private void ComputeMostTrainedMuscleGroup(
            List<WorkoutSession> sessions,
            Dictionary<int, IEnumerable<WorkoutSet>> setsBySessionId,
            Dictionary<int, Exercise> exerciseDict)
        {
            try
            {
                if (sessions.Count == 0) { MostTrainedMuscleGroup = string.Empty; return; }

                var flatSets = sessions
                    .Where(s => setsBySessionId.ContainsKey(s.Id))
                    .SelectMany(s => setsBySessionId[s.Id])
                    .ToList();

                if (flatSets.Count == 0) { MostTrainedMuscleGroup = string.Empty; return; }

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
            catch { MostTrainedMuscleGroup = string.Empty; }
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

            (MotivationalMessage, MotivationalSubMessage) = daysSinceLastWorkout switch
            {
                0 => ("Great work today! \U0001f525",
                         "You already crushed a session. Rest up or go again!"),
                1 => ($"{timeGreeting}! Ready to go? \U0001f4aa",
                         "Yesterday's session was great. Keep the momentum going!"),
                2 => ("Time to get back at it! \U0001f3cb\ufe0f",
                         "It's been 2 days. Your muscles are rested and ready."),
                <= 5 => ("Don't break the habit! \u26a1",
                         $"{daysSinceLastWorkout} days since your last session. Let's go!"),
                _ when TotalWorkouts == 0
                     => ("Welcome! Let's get started \U0001f680",
                         "Log your first workout to start tracking your progress."),
                _ => ("Welcome back! \U0001f44b",
                         "It's been a while. Every session counts \u2014 let's go!")
            };

            StreakSubtitle = CurrentStreak switch
            {
                0 => "Start your streak today",
                1 => "1 day \u2014 keep it going!",
                >= 7 => $"{CurrentStreak} days \u2014 you're on fire! \U0001f525",
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

        [RelayCommand]
        private static Task ViewBodyWeight() => Shell.Current.GoToAsync(Routes.BodyWeight);

        [RelayCommand]
        private static Task ViewPlateCalculator() => Shell.Current.GoToAsync(Routes.PlateCalculator);
        #endregion
    }
}
