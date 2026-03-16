using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class HomeViewModel(IWorkoutService workoutService, IAnalyticsService analyticsService) : BaseViewModel
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

        #region "VIEW PERSONAL RECORDS"
        [RelayCommand]
        private static Task ViewPersonalRecords() => Shell.Current.GoToAsync(Routes.PersonalRecords);
        #endregion

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

                await Task.WhenAll(totalWorkoutsTask, currentStreakTask, lastWorkoutDateTask,
                    averageDurationTask, allSessionsTask);

                TotalWorkouts = totalWorkoutsTask.Result;
                CurrentStreak = currentStreakTask.Result;
                LastWorkoutDate = lastWorkoutDateTask.Result;
                AverageDuration = averageDurationTask.Result;

                var allSessions = allSessionsTask.Result;

                // Last workout shown in its own card
                LastWorkoutSession = allSessions.FirstOrDefault();

                // Skip first since it's shown in Last Workout card, take next 3
                var recent = allSessions.Skip(1).Take(3).ToList();
                RecentSessions.Clear();
                foreach (var session in recent)
                    RecentSessions.Add(session);

                // Most trained muscle group this week
                await LoadMostTrainedMuscleGroup(allSessions);

                // This week stats
                var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                var thisWeekSessions = allSessions.Where(s => s.Date >= weekStart).ToList();
                WorkoutsThisWeek = thisWeekSessions.Count;

                var thisWeekSetTasks = thisWeekSessions
                    .Select(s => workoutService.GetSetsForSessionAsync(s.Id))
                    .ToList();
                var thisWeekSets = await Task.WhenAll(thisWeekSetTasks);
                var flatSets = thisWeekSets.SelectMany(s => s).ToList();
                SetsThisWeek = flatSets.Count;
                VolumeThisWeek = flatSets.Sum(s => s.Weight * s.Reps);

                // Dynamic motivational message
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
        private async Task LoadMostTrainedMuscleGroup(List<WorkoutSession> allSessions)
        {
            try
            {
                var weekStart = DateTime.Today.AddDays(-7);
                var recentSessions = allSessions.Where(s => s.Date >= weekStart).ToList();
                if (recentSessions.Count == 0)
                {
                    MostTrainedMuscleGroup = string.Empty;
                    return;
                }

                var setTasks = recentSessions.Select(s => workoutService.GetSetsForSessionAsync(s.Id)).ToList();
                var allSets = await Task.WhenAll(setTasks);
                var flatSets = allSets.SelectMany(s => s).ToList();

                if (flatSets.Count == 0)
                {
                    MostTrainedMuscleGroup = string.Empty;
                    return;
                }

                var exercises = await workoutService.GetAllExercisesAsync();
                var exerciseDict = exercises.ToDictionary(e => e.Id);

                MostTrainedMuscleGroup = flatSets
                    .Where(s => exerciseDict.ContainsKey(s.ExerciseId))
                    .GroupBy(s => exerciseDict[s.ExerciseId].MuscleGroup)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key ?? string.Empty;

                // Set color based on muscle group
                MostTrainedMuscleGroupColor = MostTrainedMuscleGroup switch
                {
                    "Chest" => "#1F77F0",
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
            string timeGreeting = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";

            if (daysSinceLastWorkout == 0)
            {
                MotivationalMessage = "Great work today! 🔥";
                MotivationalSubMessage = "You already crushed a session. Rest up or go again!";
            }
            else if (daysSinceLastWorkout == 1)
            {
                MotivationalMessage = $"{timeGreeting}! Ready to go? 💪";
                MotivationalSubMessage = "Yesterday's session was great. Keep the momentum going!";
            }
            else if (daysSinceLastWorkout == 2)
            {
                MotivationalMessage = "Time to get back at it! 🏋️";
                MotivationalSubMessage = "It's been 2 days. Your muscles are rested and ready.";
            }
            else if (daysSinceLastWorkout <= 5)
            {
                MotivationalMessage = "Don't break the habit! ⚡";
                MotivationalSubMessage = $"{daysSinceLastWorkout} days since your last session. Let's go!";
            }
            else if (TotalWorkouts == 0)
            {
                MotivationalMessage = "Welcome! Let's get started 🚀";
                MotivationalSubMessage = "Log your first workout to start tracking your progress.";
            }
            else
            {
                MotivationalMessage = "Welcome back! 👋";
                MotivationalSubMessage = "It's been a while. Every session counts — let's go!";
            }

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