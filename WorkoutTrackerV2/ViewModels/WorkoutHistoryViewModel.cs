
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
        private ObservableCollection<WorkoutSessionDetail> sessions = [];
        #endregion

        #region "PUBLIC PROPERTIES"
        public ObservableCollection<WorkoutSessionDetail> Sessions
        {
            get => sessions;
            set => SetProperty(ref sessions, value);
        }
        #endregion

        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Now;

        [ObservableProperty]
        private string _filterText = string.Empty;

        [ObservableProperty]
        private bool _isRefreshing = false;

        [ObservableProperty]
        private int _selectedDays = 0;
        #endregion

        #region "SET TIME PERIOD"
        [RelayCommand]
        private void SetTimePeriod(string days)
        {
            if (int.TryParse(days, out int result))
                SelectedDays = result;
        }
        #endregion

        #region "ON SELECTED DAYS CHANGED"
        partial void OnSelectedDaysChanged(int value) => LoadSessionsCommand.Execute(null);
        #endregion

        #region "LOAD SESSION"
        [RelayCommand]
        private async Task LoadSessions()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                Sessions.Clear();

                var startDate = SelectedDays == 0 ? DateTime.MinValue : DateTime.Now.AddDays(-SelectedDays).Date;
                var endDate = DateTime.Now.Date.AddDays(1);
                var allSessions = await workoutService.GetSessionsAsync(startDate, endDate);

                // Fetch all sets in parallel instead of one by one
                var setTasks = allSessions.Select(s => workoutService.GetSetsForSessionAsync(s.Id)).ToList();
                var allSets = await Task.WhenAll(setTasks);

                for (int i = 0; i < allSessions.Count; i++)
                {
                    var sets = allSets[i];
                    Sessions.Add(new WorkoutSessionDetail
                    {
                        Session = allSessions[i],
                        SetCount = sets.Count,
                        TotalReps = sets.Sum(s => s.Reps),
                        TotalWeight = sets.Sum(s => s.Weight * s.Reps),
                        Sets = sets
                    });
                }
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

        #region "DELETE SESSION"
        [RelayCommand]
        private async Task DeleteSession(WorkoutSessionDetail detail)
        {
            if (detail?.Session == null)
            {
                return;
            }
                
            bool confirmed = await Shell.Current.DisplayAlertAsync("Delete Workout", $"Are you sure you want to delete the {detail.Session.DayName} workout?", "Yes", "No");
            if (confirmed)
            {
                try
                {
                    foreach (var set in detail.Sets)
                    {
                        await workoutService.DeleteSetAsync(set);
                    }
                    await workoutService.DeleteSessionAsync(detail.Session);
                    await LoadSessions();
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlertAsync("DeleteSession Error", ex.Message, "OK");
                }
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


