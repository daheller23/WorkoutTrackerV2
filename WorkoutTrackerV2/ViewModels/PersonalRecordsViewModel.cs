using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class PersonalRecordsViewModel(IAnalyticsService analyticsService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ObservableCollection<PersonalRecord> _records = [];
        [ObservableProperty] private int _selectedDays = 0;
        #endregion

        #region "PARTIAL METHODS"
        partial void OnSelectedDaysChanged(int value) => LoadRecordsCommand.Execute(null);
        #endregion

        #region "LOAD RECORDS"
        [RelayCommand]
        private async Task LoadRecords()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                var records = await analyticsService.GetPersonalRecordsAsync(SelectedDays);
                Records = new ObservableCollection<PersonalRecord>(records);
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region "TOGGLE EXPANDED"
        [RelayCommand]
        private void ToggleExpanded(PersonalRecord record)
        {
            record.IsExpanded = !record.IsExpanded;
            // Force UI refresh by replacing the item
            var index = Records.IndexOf(record);
            if (index >= 0)
            {
                Records.RemoveAt(index);
                Records.Insert(index, record);
            }
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

        #region "GO BACK"
        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
        #endregion
    }
}