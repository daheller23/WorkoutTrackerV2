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

        // Volume Distribution Properties
        [ObservableProperty] private DonutChart? _volumeChart;
        [ObservableProperty] private ObservableCollection<VolumeDistributionItem> _volumeDistribution = [];

        // FIX: Added explicit boolean for UI visibility toggling
        [ObservableProperty] private bool _hasVolumeData;

        [ObservableProperty] private string _muscleGroupColor = "#1F77F0";

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

            _ = LoadDataAsync();
        }

        partial void OnSelectedDaysChanged(int value)
        {
            if (string.IsNullOrEmpty(MuscleGroup)) return;

            foreach (var pill in TimePeriodPills)
                pill.IsSelected = pill.Days == value;

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

                var progress = await analyticsService.GetProgressForMuscleGroupAsync(MuscleGroup, SelectedDays);

                // 1. Process Improvements and Colors
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

                Exercises = new ObservableCollection<ExerciseProgress>(progress);

                // 2. Calculate Aggregates
                int sets = 0, reps = 0;
                double maxWeight = 0;
                foreach (var e in progress)
                {
                    sets += e.Sets?.Count ?? 0;
                    reps += e.TotalReps;
                    if (e.MaxWeight > maxWeight) maxWeight = e.MaxWeight;
                }

                TotalSets = sets;
                TotalReps = reps;
                MaxWeight = maxWeight;

                // 3. Calculate Volume Distribution (The Fix is here)
                if (TotalSets > 0 && progress.Count > 0)
                {
                    var groupedVolume = progress
                        .GroupBy(e => {
                            // Check the exercise model directly if the progress model is empty
                            if (!string.IsNullOrWhiteSpace(e.SubMuscleGroup) && e.SubMuscleGroup != "General")
                                return e.SubMuscleGroup;

                            return "General";
                        })
                        .Select((g, index) => new VolumeDistributionItem
                        {
                            Name = g.Key,
                            Sets = g.Sum(e => e.Sets?.Count ?? 0),
                            ColorHex = ChartColors[index % ChartColors.Length],
                            Percentage = (double)g.Sum(e => e.Sets?.Count ?? 0) / TotalSets
                        })
                        .OrderByDescending(v => v.Sets)
                        .ToList();

                    var chartEntries = groupedVolume.Select(item => new ChartEntry(item.Sets)
                    {
                        Color = SkiaSharp.SKColor.Parse(item.ColorHex)
                    }).ToList();

                    VolumeDistribution = new ObservableCollection<VolumeDistributionItem>(groupedVolume);
                    VolumeChart = new DonutChart
                    {
                        Entries = chartEntries,
                        BackgroundColor = SkiaSharp.SKColors.Transparent,
                        HoleRadius = 0.65f,
                        LabelTextSize = 0
                    };

                    HasVolumeData = true;
                }
                else
                {
                    HasVolumeData = false;
                }

                var top = progress.OrderByDescending(e => e.MaxWeight).FirstOrDefault();
                TopExerciseName = top?.ExerciseName ?? string.Empty;
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
        private void BuildCombinedChart(ExerciseProgress? topExercise)
        {
            if (topExercise?.Points.Count == 0 || topExercise is null) return;
            CombinedChart = ChartHelper.BuildProgressChart(topExercise.Points);
        }
        #endregion
    }

    // Helper Class for the Breakdown List
    public class VolumeDistributionItem
    {
        public string Name { get; set; } = string.Empty;
        public int Sets { get; set; }
        public double Percentage { get; set; }
        public string DisplayPercentage => $"{Percentage:P0}";
        public string ColorHex { get; set; } = "#1F77F0";
    }
}