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
        [ObservableProperty] private double _averageDuration = 0; 
        [ObservableProperty] private int _currentStreak = 0;
        [ObservableProperty] private DateTime? _lastWorkoutDate = DateTime.Today;
        [ObservableProperty] private ObservableCollection<WorkoutSession> _recentSessions = [];
        [ObservableProperty] private int _totalWorkouts = 0;
        #endregion

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData()
        {
            try
            {
                IsLoading = true;
                TotalWorkouts = await workoutService.GetTotalWorkoutCountAsync();
                CurrentStreak = await analyticsService.GetCurrentStreak();
                LastWorkoutDate = await workoutService.GetLastWorkoutDateAsync();
                AverageDuration = await analyticsService.GetAverageWorkoutDurationAsync();

                var allSessions = await workoutService.GetAllSessionsAsync();
                var recent = allSessions.Take(5).ToList();

                RecentSessions.Clear();
                foreach (var session in recent)
                {
                    RecentSessions.Add(session);
                }
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

        #region "START WORKOUT"
        [RelayCommand]
        private static async Task StartWorkout()
        {
            await Shell.Current.GoToAsync(Routes.Workout);
        }
        #endregion

        #region "VIEW HISTORY"
        [RelayCommand]
        private static async Task ViewHistory()
        {
            await Shell.Current.GoToAsync(Routes.History);
        }
        #endregion

        #region "VIEW ANALYTICS"
        [RelayCommand]
        private static async Task ViewAnalytics()
        {
            await Shell.Current.GoToAsync(Routes.Analytics);
        }
        #endregion

        #region "VIEW WORKOUT
        [RelayCommand]
        private static async Task ViewWorkout(WorkoutSession session)
        {
            await Shell.Current.GoToAsync(Routes.WorkoutDetail, new Dictionary<string, object>
            {
                { "Session", session }
            });
        }
        #endregion

    }
}
