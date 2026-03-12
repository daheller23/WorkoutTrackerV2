
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

        #region "LOAD SESSION"
        [RelayCommand]
        private async Task LoadSessions()
        {
            try
            {
                IsLoading = true;
                Sessions.Clear();

                var startDate = DateTime.Now.AddDays(-SelectedDays).Date;
                var endDate = DateTime.Now.Date.AddDays(1);
                var allSessions = await workoutService.GetSessionsAsync(startDate, endDate);

                foreach (var session in allSessions)
                {
                    var sets = await workoutService.GetSetsForSessionAsync(session.Id);
                    var detail = new WorkoutSessionDetail
                    {
                        Session = session,
                        SetCount = sets.Count,
                        TotalReps = sets.Sum(s => s.Reps),
                        TotalWeight = sets.Sum(s => s.Weight * s.Reps),
                        Sets = sets
                    };
                    Sessions.Add(detail);
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




    }
}

