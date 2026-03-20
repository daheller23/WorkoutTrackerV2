using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Helpers;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(MuscleGroup), "MuscleGroup")]
    public partial class MuscleGroupProgressViewModel(
        IAnalyticsService analyticsService,
        ISettingsService settingsService) : BaseViewModel
    {
        #region "PRIVATE VARIABLES"
        private static readonly string[] ChartColors =
        [
            "#1F77F0", "#4CAF50", "#FF9800", "#FF6B6B",
            "#9C27B0", "#00BCD4", "#FF5722", "#607D8B"
        ];
        #endregion

        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private string _muscleGroup = string.Empty;
        [ObservableProperty] private ObservableCollection<ExerciseProgress> _exercises = [];
        [ObservableProperty] private LineChart? _combinedChart;
        [ObservableProperty] private int _selectedDays = 30;
        [ObservableProperty] private string _weightUnitLabel = "lbs";
        [ObservableProperty] private string _topExerciseName = string.Empty;
        [ObservableProperty] private int _totalSets;
        [ObservableProperty] private int _totalReps;
        [ObservableProperty] private double _maxWeight;

        // FIX 6: Pre-computed color string exposed as a property so the XAML header
        // can bind directly instead of running MuscleGroupColorConverter twice.
        [ObservableProperty] private string _muscleGroupColor = "#1F77F0";

        // Reuses TimePeriodPillViewModel from AnalyticsViewModel — constructed once,
        // IsSelected toggled when SelectedDays changes.
        public List<TimePeriodPillViewModel> TimePeriodPills { get; } =
        [
            new() { Label = "All", Days = 0  },
            new() { Label = "7d",  Days = 7  },
            new() { Label = "14d", Days = 14 },
            new() { Label = "30d", Days = 30, IsSelected = true },
            new() { Label = "60d", Days = 60 },
            new() { Label = "90d", Days = 90 },
        ];
        #endregion

        #region "PARTIAL METHODS"
        partial void OnMuscleGroupChanged(string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            // FIX 6: Compute the color once when MuscleGroup is set.
            MuscleGroupColor = value switch
            {
                "Chest" => "#4A90D9",
                "Back" => "#27AE60",
                "Legs" => "#E67E22",
                "Shoulders" => "#8E44AD",
                "Arms" => "#E74C3C",
                "Core" => "#5DADE2",
                _ => "#1F77F0"
            };

            // FIX 1: Call async method directly instead of LoadDataCommand.Execute().
            _ = LoadDataAsync();
        }

        partial void OnSelectedDaysChanged(int value)
        {
            // FIX 4: Guard replaces the _isInitialized flag — same intent, no extra field.
            if (string.IsNullOrEmpty(MuscleGroup)) return;

            // Update pill selection state.
            foreach (var pill in TimePeriodPills)
                pill.IsSelected = pill.Days == value;

            // FIX 1: Call async method directly.
            _ = LoadDataAsync();
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

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData() => await LoadDataAsync();

        private async Task LoadDataAsync()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                WeightUnitLabel = settingsService.WeightUnit;

                var progress = await analyticsService.GetProgressForMuscleGroupAsync(
                    MuscleGroup, SelectedDays);

                // Assign chart colors and improvement labels by index.
                for (int i = 0; i < progress.Count; i++)
                {
                    progress[i].ChartColor = ChartColors[i % ChartColors.Length];

                    var diff = progress[i].LatestMaxWeight - progress[i].EarliestMaxWeight;
                    if (diff != 0 && progress[i].EarliestMaxWeight > 0)
                    {
                        var sign = diff > 0 ? "↑" : "↓";
                        progress[i].ImprovementLabel = $"{sign} {Math.Abs(diff):F0} {settingsService.WeightUnit}";
                        progress[i].ImprovementColor = diff > 0 ? "#4CAF50" : "#FF6B6B";
                        progress[i].HasImprovement = true;
                    }
                }

                // FIX 3: Find top exercise once — reused for TopExerciseName and
                // BuildCombinedChart instead of calling OrderByDescending twice.
                var top = progress.Count > 0
                    ? progress.OrderByDescending(e => e.MaxWeight).First()
                    : null;
                TopExerciseName = top?.ExerciseName ?? string.Empty;

                Exercises = new ObservableCollection<ExerciseProgress>(progress);

                // FIX 2: Single loop computes all three summary stats instead of
                // three separate LINQ passes (Sum+Sum+Max) over the same list.
                int sets = 0, reps = 0;
                double maxWeight = 0;
                foreach (var e in progress)
                {
                    sets += e.Sets.Count;
                    reps += e.TotalReps;
                    if (e.MaxWeight > maxWeight) maxWeight = e.MaxWeight;
                }
                TotalSets = sets;
                TotalReps = reps;
                MaxWeight = maxWeight;

                // FIX 3: Pass the already-found top exercise — no second sort needed.
                BuildCombinedChart(top);
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

        #region "SELECT EXERCISE"
        [RelayCommand]
        private static async Task SelectExercise(ExerciseProgress exercise)
        {
            await Shell.Current.GoToAsync(Routes.ExerciseProgress, new Dictionary<string, object>
            {
                { "Exercise", exercise }
            });
        }
        #endregion

        #region "GO BACK"
        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
        #endregion

        #region "BUILD COMBINED CHART"
        // FIX 3: Accepts the pre-found top exercise instead of re-sorting Exercises.
        private void BuildCombinedChart(ExerciseProgress? topExercise)
        {
            if (topExercise?.Points.Count == 0 || topExercise is null) return;
            CombinedChart = ChartHelper.BuildProgressChart(topExercise.Points);
        }
        #endregion
    }
}
