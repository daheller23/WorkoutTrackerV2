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
        [ObservableProperty] private ObservableCollection<WeightPercentage> _percentages = [];
        [ObservableProperty] private ObservableCollection<WorkoutHistoryGroup> _groupedSets = [];

        // Color
        [ObservableProperty] private string _muscleGroupColor = "#1F77F0";

        // Coaching & Insights
        [ObservableProperty] private double _estimatedOneRepMax;
        [ObservableProperty] private bool _hasOneRepMax;
        [ObservableProperty] private double _totalVolume;
        [ObservableProperty] private bool _isPlateaued;
        [ObservableProperty] private string _coachAdvice = "";

        // Strength Rank
        [ObservableProperty] private string _strengthLevel = "Beginner";
        [ObservableProperty] private double _strengthPercentage;
        [ObservableProperty] private string _currentRatioText = "0.00x BW";
        [ObservableProperty] private string _weightToNextLevel = "";

        public string WeightUnitLabel => settingsService.WeightUnit;

        partial void OnExerciseChanged(ExerciseProgress? value)
        {
            if (value is null || value.Sets == null || value.Sets.Count == 0) return;

            MuscleGroupColor = value.MuscleGroup switch
            {
                "Chest" => "#4A90D9",
                "Back" => "#27AE60",
                "Legs" => "#E67E22",
                "Shoulders" => "#8E44AD",
                "Arms" => "#E74C3C",
                "Core" => "#5DADE2",
                _ => "#1F77F0"
            };

            // 1. PERFORMANCE GRAPH & VOLUME
            if (value.Points?.Count > 0)
                Chart = ChartHelper.BuildProgressChart(value.Points);

            TotalVolume = value.Sets.Sum(s => s.Weight * s.Reps);

            // 2. THE "BEST SET" 1RM CALCULATION (Brzycki Formula)
            // We find the highest 1RM potential across ALL sets, not just the heaviest one.
            double highestPotential = 0;

            foreach (var set in value.Sets)
            {
                double current1RM;
                if (set.Reps <= 1)
                {
                    current1RM = set.Weight;
                }
                else
                {
                    // Brzycki Formula: Weight / (1.0278 - (0.0278 * Reps))
                    current1RM = set.Weight / (1.0278 - (0.0278 * set.Reps));
                }

                if (current1RM > highestPotential)
                    highestPotential = current1RM;
            }

            EstimatedOneRepMax = Math.Round(highestPotential, 1);
            HasOneRepMax = EstimatedOneRepMax > 0;

            // 3. RE-CALCULATE RANK & COACHING
            CalculateStrengthRank(EstimatedOneRepMax);
            CheckForPlateau(value);

            // 4. PERCENTAGES & HISTORY
            CalculatePercentageTable(EstimatedOneRepMax);

            var groups = value.Sets
                .GroupBy(s => s.CreatedDate.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new WorkoutHistoryGroup(g.Key, g.OrderBy(s => s.CreatedDate).ToList()));

            GroupedSets = new ObservableCollection<WorkoutHistoryGroup>(groups);
        }

        private void CheckForPlateau(ExerciseProgress exercise)
        {
            if (exercise.Points == null || exercise.Points.Count < 4) return;

            var lastFour = exercise.Points.TakeLast(4).ToList();
            // Check if current max is less than or equal to the max from 4 sessions ago
            bool noProgress = lastFour.All(p => p.MaxWeight <= lastFour[0].MaxWeight);

            if (noProgress)
            {
                IsPlateaued = true;
                CoachAdvice = "You've hit a wall. Try a 'Deload Week' or switch to a higher rep range for 2 weeks to spark new growth.";
            }
            else { IsPlateaued = false; }
        }

        private void CalculateStrengthRank(double oneRepMax)
        {
            double bodyWeight = 80; // Placeholder: Connect to settings later!
            double ratio = oneRepMax / bodyWeight;
            CurrentRatioText = $"{ratio:F2}x BW";

            // Updated Tiers: Beginner(0.75), Novice(1.25), Intermediate(1.75), Advanced(2.5), Elite(>2.5)
            if (ratio < 0.75)
            {
                StrengthLevel = "Beginner";
                StrengthPercentage = Math.Clamp(ratio / 0.75, 0, 1);
            }
            else if (ratio < 1.25)
            {
                StrengthLevel = "Novice";
                StrengthPercentage = Math.Clamp((ratio - 0.75) / 0.5, 0, 1);
            }
            else if (ratio < 1.75)
            {
                StrengthLevel = "Intermediate";
                StrengthPercentage = Math.Clamp((ratio - 1.25) / 0.5, 0, 1);
            }
            else if (ratio < 2.5)
            {
                StrengthLevel = "Advanced";
                StrengthPercentage = Math.Clamp((ratio - 1.75) / 0.75, 0, 1);
            }
            else
            {
                StrengthLevel = "Elite / World Class";
                StrengthPercentage = 1.0;
            }

            // Calculate next goal nudge
            double[] goals = { 0.75, 1.25, 1.75, 2.5 };
            double nextGoal = goals.FirstOrDefault(g => g > ratio);
            WeightToNextLevel = nextGoal > 0
                ? $"+{Math.Round((nextGoal * bodyWeight) - oneRepMax, 1)} {WeightUnitLabel} to level up"
                : "Ultimate Rank Achieved!";
        }

        private void CalculatePercentageTable(double baseWeight)
        {
            Percentages.Clear();
            if (baseWeight <= 0) return;
            int[] targets = [100, 95, 90, 85, 80, 75, 70, 60, 50];
            foreach (var p in targets)
            {
                Percentages.Add(new WeightPercentage { Percent = p, Weight = Math.Round(baseWeight * (p / 100.0), 1) });
            }
        }

        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync("..");
    }

    public class WorkoutHistoryGroup(DateTime date, List<WorkoutSet> sets) : List<WorkoutSet>(sets)
    {
        public DateTime Date { get; set; } = date;
        public string DisplayDate => Date.ToString("MMMM d, yyyy");
    }
}