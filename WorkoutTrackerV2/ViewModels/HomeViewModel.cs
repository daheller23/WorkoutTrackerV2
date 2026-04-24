using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Helpers;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class HomeViewModel(IDashboardService dashboardService) : BaseViewModel
    {
        public static string PendingPrMessage { get; set; } = string.Empty;

        [ObservableProperty] private string _mostTrainedMuscleGroupColor = ColorHelper.GetDefaultColor();

        [ObservableProperty] private int _currentStreak;
        [ObservableProperty] private int _setsThisWeek;
        [ObservableProperty] private int _totalWorkouts;
        [ObservableProperty] private int _workoutsThisWeek;

        [ObservableProperty] private double _averageDuration;
        [ObservableProperty] private double _volumeThisWeek;

        [ObservableProperty] private string _mostTrainedMuscleGroup = string.Empty;
        [ObservableProperty] private string _motivationalMessage = string.Empty;
        [ObservableProperty] private string _motivationalSubMessage = string.Empty;
        [ObservableProperty] private string _streakSubtitle = string.Empty;

        [ObservableProperty] private DateTime?                              _lastWorkoutDate;
        [ObservableProperty] private ObservableCollection<WorkoutSession>   _recentSessions = [];
        [ObservableProperty] private WorkoutSession?                        _lastWorkoutSession;

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private async Task LoadData()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;

                var summary = await dashboardService.GetHomeDashboardSummaryAsync();
                TotalWorkouts = summary.TotalWorkouts;
                CurrentStreak = summary.CurrentStreak;
                LastWorkoutDate = summary.LastWorkoutDate;
                AverageDuration = summary.AverageDuration;
                LastWorkoutSession = summary.LastWorkoutSession;
                RecentSessions = new ObservableCollection<WorkoutSession>(summary.RecentSessions);
                WorkoutsThisWeek = summary.WorkoutsThisWeek;
                SetsThisWeek = summary.SetsThisWeek;
                VolumeThisWeek = summary.VolumeThisWeek;
                MostTrainedMuscleGroup = summary.TopMuscleGroup;

                SetMostTrainedMuscleGroupColor();
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

        [RelayCommand]
        private static Task StartWorkout() => Shell.Current.GoToAsync(Routes.Workout);

        [RelayCommand]
        private static Task ViewHistory() => Shell.Current.GoToAsync(Routes.History);

        [RelayCommand]
        private static Task ViewAnalytics() => Shell.Current.GoToAsync(Routes.Analytics);

        [RelayCommand]
        private static Task ViewPersonalRecords() => Shell.Current.GoToAsync(Routes.PersonalRecords);

        [RelayCommand]
        private static Task ViewPlateCalculator() => Shell.Current.GoToAsync(Routes.PlateCalculator);

        [RelayCommand]
        private static Task ViewOneRepMaxCalculator() => Shell.Current.GoToAsync(Routes.OneRmCalculator);

        [RelayCommand]
        private static Task ViewWeightConverter() => Shell.Current.GoToAsync(Routes.WeightConverterCalculator);

        [RelayCommand]
        private static Task ViewWorkout(WorkoutSession session) =>
            Shell.Current.GoToAsync(Routes.WorkoutDetail, new Dictionary<string, object>
            {
                { "Session", session }
            });

        [RelayCommand]
        private static Task ViewSettings() => Shell.Current.GoToAsync(Routes.Settings);

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================

        private void SetMostTrainedMuscleGroupColor()
        {
            MostTrainedMuscleGroupColor = ColorHelper.GetMuscleGroupColor(MostTrainedMuscleGroup);
        }

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
    }
}
