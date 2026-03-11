using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class HomeViewModel : BaseViewModel
    {
        private readonly IWorkoutService _workoutService;
        private readonly IAnalyticsService _analyticsService;

        [ObservableProperty]
        private double _averageDuration;

        [ObservableProperty]
        private int _currentStreak;

        [ObservableProperty]
        private DateTime? _lastWorkoutDate;

        [ObservableProperty]
        private ObservableCollection<WorkoutSession> _recentSessions;

        [ObservableProperty]
        private int _totalWorkouts;

        public HomeViewModel(IWorkoutService workoutService, IAnalyticsService analyticsService)
        {
            _workoutService = workoutService;
            _analyticsService = analyticsService;
            RecentSessions = new();
        }

        [RelayCommand]
        private async Task LoadData()
        {
            try
            {
                IsLoading = true;
                TotalWorkouts = await _workoutService.GetTotalWorkoutCountAsync();
                CurrentStreak = await _analyticsService.GetCurrentStreak();
                LastWorkoutDate = await _workoutService.GetLastWorkoutDateAsync();
                AverageDuration = await _analyticsService.GetAverageWorkoutDurationAsync();

                var allSessions = await _workoutService.GetAllSessionsAsync();
                var recent = allSessions.Take(5).ToList();

                RecentSessions.Clear();
                foreach (var session in recent)
                {
                    RecentSessions.Add(session);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task StartWorkout()
        {
            await Shell.Current.GoToAsync(Routes.Workout);
        }

        [RelayCommand]
        private async Task ViewHistory()
        {
            await Shell.Current.GoToAsync(Routes.History);
        }

        [RelayCommand]
        private async void ViewAnalytics()
        {
            await Shell.Current.GoToAsync(Routes.Analytics);
        }

        [RelayCommand]
        private async Task ViewWorkout(WorkoutSession session)
        {
            await Shell.Current.GoToAsync(Routes.WorkoutDetail, new Dictionary<string, object>
            {
                { "Session", session }
            });
        }

    }
}
