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
        [ObservableProperty] private double _estimatedOneRepMax;
        [ObservableProperty] private double _strengthPercentage;
        [ObservableProperty] private double _totalVolume;

        [ObservableProperty] private string _coachAdvice = "";
        [ObservableProperty] private string _currentRatioText = "0.00x BW";
        [ObservableProperty] private string _muscleGroupColor = "#1F77F0";
        [ObservableProperty] private string _strengthLevel = "Beginner";
        [ObservableProperty] private string _weightToNextLevel = "";

        [ObservableProperty] private bool _hasOneRepMax;
        [ObservableProperty] private bool _isPlateaued;

        [ObservableProperty] private ExerciseProgress? _exercise;
        [ObservableProperty] private LineChart? _chart;
        [ObservableProperty] private ObservableCollection<WeightPercentage> _percentages = [];
        [ObservableProperty] private ObservableCollection<WorkoutHistoryGroup> _groupedSets = [];

        // ==============================================================================================================
        //
        //      PUBLIC METHODS
        //
        // ==============================================================================================================

        public string WeightUnitLabel => settingsService.WeightUnit;

        // ==============================================================================================================
        //
        //      PARTIAL METHODS
        //
        // ==============================================================================================================

        partial void OnExerciseChanged(ExerciseProgress? value)
        {
            if (value is null || value.Sets == null || value.Sets.Count == 0)
            {
                return;
            }

            MuscleGroupColor = ColorHelper.GetMuscleGroupColor(value.MuscleGroup);

            if (value.Points?.Count > 0)
            {
                Chart = ChartHelper.BuildProgressChart(value.Points);
            }
                
            TotalVolume = value.Sets.Sum(s => s.Weight * s.Reps);

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
                    current1RM = set.Weight / (1.0278 - (0.0278 * set.Reps));
                }

                if (current1RM > highestPotential)
                    highestPotential = current1RM;
            }

            EstimatedOneRepMax = Math.Round(highestPotential, 1);
            HasOneRepMax = EstimatedOneRepMax > 0;

            CalculateStrengthRank(EstimatedOneRepMax);
            CheckForPlateau(value);

            CalculatePercentageTable(EstimatedOneRepMax);

            var groups = value.Sets
                .GroupBy(s => s.CreatedDate.Date)
                .OrderByDescending(g => g.Key)
                .Select(g => new WorkoutHistoryGroup(g.Key, g.OrderBy(s => s.CreatedDate).ToList()));

            GroupedSets = new ObservableCollection<WorkoutHistoryGroup>(groups);
        }

        // ==============================================================================================================
        //
        //      PRIVATE RELAY METHODS
        //
        // ==============================================================================================================

        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================

        private void CheckForPlateau(ExerciseProgress exercise)
        {
            if (exercise.Points == null || exercise.Points.Count < 4) return;

            var lastFour = exercise.Points.TakeLast(4).ToList();
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
            double bodyWeight = 80;
            double ratio = oneRepMax / bodyWeight;
            CurrentRatioText = $"{ratio:F2}x BW";

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

            double[] goals = { 0.75, 1.25, 1.75, 2.5 };
            double nextGoal = goals.FirstOrDefault(g => g > ratio);
            WeightToNextLevel = nextGoal > 0
                ? $"+{Math.Round((nextGoal * bodyWeight) - oneRepMax, 1)} {WeightUnitLabel} to level up"
                : "Ultimate Rank Achieved!";
        }

        private void CalculatePercentageTable(double baseWeight)
        {
            Percentages.Clear();
            if (baseWeight <= 0)
            {
                return;
            }
            int[] targets = [100, 95, 90, 85, 80, 75, 70, 60, 50];
            foreach (var p in targets)
            {
                Percentages.Add(new WeightPercentage { Percent = p, Weight = Math.Round(baseWeight * (p / 100.0), 1) });
            }
        }
    }

    // ==============================================================================================================
    //
    //      CLASSES
    //
    // ==============================================================================================================

    public class WorkoutHistoryGroup(DateTime date, List<WorkoutSet> sets) : List<WorkoutSet>(sets)
    {
        public DateTime Date { get; set; } = date;
        public string DisplayDate => Date.ToString("MMMM d, yyyy");
    }
}