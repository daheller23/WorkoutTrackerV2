using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class PersonalRecordsViewModel(IAnalyticsService analyticsService, ISettingsService settingsService) : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ObservableCollection<PersonalRecord> _records = [];
        [ObservableProperty] private int _selectedDays = 0;
        [ObservableProperty] private string _weightUnitLabel = "lbs";

        // FIX 4: Pill VMs with different day values from the Analytics/History pages
        // (0, 30, 60, 90, 180, 365 instead of 0, 7, 14, 30, 60, 90).
        // Constructed once; IsSelected toggled when SelectedDays changes.
        public List<TimePeriodPillViewModel> TimePeriodPills { get; } =
        [
            new() { Label = "All",  Days = 0,   IsSelected = true },
            new() { Label = "30d",  Days = 30  },
            new() { Label = "60d",  Days = 60  },
            new() { Label = "90d",  Days = 90  },
            new() { Label = "180d", Days = 180 },
            new() { Label = "1yr",  Days = 365 },
        ];
        #endregion

        #region "PARTIAL METHODS"
        partial void OnSelectedDaysChanged(int value)
        {
            foreach (var pill in TimePeriodPills)
                pill.IsSelected = pill.Days == value;
            // FIX 1: Call async method directly instead of LoadRecordsCommand.Execute().
            _ = LoadRecordsAsync();
        }
        #endregion

        #region "LOAD RECORDS"
        [RelayCommand]
        private async Task LoadRecords() => await LoadRecordsAsync();

        private async Task LoadRecordsAsync()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                WeightUnitLabel = settingsService.WeightUnit;
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
            // FIX 2: PersonalRecord.IsExpanded is now an [ObservableProperty] on
            // the model (via PersonalRecord.Display.cs). Toggling it fires
            // PropertyChanged automatically — no RemoveAt+Insert needed to force
            // a CollectionView refresh.
            record.IsExpanded = !record.IsExpanded;
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
