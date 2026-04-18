using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class PersonalRecordsViewModel(IAnalyticsService analyticsService, ISettingsService settingsService) : BaseViewModel
    {
        [ObservableProperty] private int _selectedDays = 0;
        [ObservableProperty] private string _weightUnitLabel = "lbs";
        [ObservableProperty] private ObservableCollection<PersonalRecord> _records = [];

        public List<TimePeriodPillViewModel> TimePeriodPills { get; } =
        [
            new() { Label = "All",  Days = 0,   IsSelected = true },
            new() { Label = "30d",  Days = 30  },
            new() { Label = "60d",  Days = 60  },
            new() { Label = "90d",  Days = 90  },
            new() { Label = "180d", Days = 180 },
            new() { Label = "1yr",  Days = 365 },
        ];

        // ==============================================================================================================
        //
        //      PARTIAL METHODS
        //
        // ==============================================================================================================

        partial void OnSelectedDaysChanged(int value)
        {
            foreach (var pill in TimePeriodPills)
            {
                pill.IsSelected = pill.Days == value;
            }               
            _ = LoadRecordsAsync();
        }

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private async Task LoadRecords() => await LoadRecordsAsync();

        [RelayCommand]
        private void ToggleExpanded(PersonalRecord record)
        {
            record.IsExpanded = !record.IsExpanded;
        }

        [RelayCommand]
        private void SetTimePeriod(string days)
        {
            if (int.TryParse(days, out int result))
            {
                SelectedDays = result;
            }            
        }

        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================
        private async Task LoadRecordsAsync()
        {
            if (IsLoading)
            {
                return;
            }
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
    }
}
