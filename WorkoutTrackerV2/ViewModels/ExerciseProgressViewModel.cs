using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Helpers;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(Exercise), "Exercise")]
    public partial class ExerciseProgressViewModel(ISettingsService settingsService) : BaseViewModel
    {
        [ObservableProperty] private ExerciseProgress? _exercise;
        [ObservableProperty] private LineChart? _chart;

        // NEW: Exceptional Features Data
        [ObservableProperty] private double _estimatedOneRepMax;
        [ObservableProperty] private bool _hasOneRepMax;
        [ObservableProperty] private ObservableCollection<WeightPercentage> _percentages = [];

        public string WeightUnitLabel => settingsService.WeightUnit;

        partial void OnExerciseChanged(ExerciseProgress? value)
        {
            if (value is null) return;

            if (value.Points?.Count > 0)
                Chart = ChartHelper.BuildProgressChart(value.Points);

            CalculatePercentageTable(value.MaxWeight);
        }

        private void CalculatePercentageTable(double maxWeight)
        {
            Percentages.Clear();
            if (maxWeight <= 0) return;

            int[] targets = [100, 95, 90, 85, 80, 75, 70, 60, 50];
            foreach (var p in targets)
            {
                Percentages.Add(new WeightPercentage
                {
                    Percent = p,
                    Weight = Math.Round(maxWeight * (p / 100.0), 1)
                });
            }
        }

        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
    }

    public class WeightPercentage
    {
        public int Percent { get; set; }
        public double Weight { get; set; }
        public string Label => $"{Percent}%";
    }
}