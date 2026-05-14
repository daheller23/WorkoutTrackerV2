using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace WorkoutTrackerV2.Models
{
    public partial class ExerciseGroup(Exercise exercise, string defaultWeightUnit = "lbs") : ObservableObject
    {
        private readonly string _defaultWeightUnit = defaultWeightUnit;

        [ObservableProperty] private int _totalReps = 0;

        [ObservableProperty] private double _maxWeight = 0;

        [ObservableProperty] private string _currentWeightUnit = "lbs";
        [ObservableProperty] private string _lastSessionSummary = string.Empty;
        [ObservableProperty] private string _progressionSuggestion = string.Empty;

        [ObservableProperty] private bool _hasLastSession;
        [ObservableProperty] private bool _hasProgressionSuggestion;
        [ObservableProperty] private bool _isExpanded = true;
        [ObservableProperty] private bool _progressionIsIncrease;

        public int CompletedSets => Sets.Count(s => s.IsCompleted);
        public int TotalSetCount => Sets.Count;
        public double CompletionProgress => Sets.Count == 0 ? 0 : (double)CompletedSets / Sets.Count;
        public string SetCountLabel => Sets.Count == 1 ? "1 set" : $"{Sets.Count} sets";
        public bool AllSetsCompleted => Sets.Count > 0 && Sets.All(s => s.IsCompleted);
        public bool HasSets => Sets.Count > 0;
        public Exercise Exercise { get; set; } = exercise;
        public ObservableCollection<string> LastSessionChips { get; } = [];
        public ObservableCollection<WorkoutSet> Sets { get; } = [];

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private void ToggleExpanded()
        {
            IsExpanded = !IsExpanded;
        }

        // ==============================================================================================================
        //
        //      PUBLIC METHODS
        //
        // ==============================================================================================================

        public void NotifyCompletionStats()
        {
            OnPropertyChanged(nameof(CompletedSets));
            OnPropertyChanged(nameof(TotalSetCount));
            OnPropertyChanged(nameof(AllSetsCompleted));
            OnPropertyChanged(nameof(CompletionProgress));
        }

        public void NotifySetStatsPublic() => NotifySetStats();

        public void SetLastSession(List<WorkoutSet> sets, string weightUnit)
        {
            LastSessionChips.Clear();
            ProgressionSuggestion = string.Empty;
            HasProgressionSuggestion = false;

            if (sets.Count == 0)
            {
                HasLastSession = false;
                return;
            }

            var lastDate = sets.Max(s => s.CreatedDate.Date);
            var lastSets = sets
                .Where(s => s.CreatedDate.Date == lastDate)
                .OrderBy(s => s.SetNumber)
                .ToList();

            LastSessionSummary = $"Last session · {lastDate:MMM d}";

            double Convert(WorkoutSet s) =>
                s.WeightUnit == weightUnit ? s.Weight
                : s.WeightUnit == "lbs" ? s.Weight * 0.453592
                                           : s.Weight / 0.453592;

            var lastWeights = lastSets.Select(Convert).ToList();

            for (int i = 0; i < lastSets.Count; i++)
            {
                LastSessionChips.Add($"{lastSets[i].Reps} × {lastWeights[i]:F0}");
                if (i < Sets.Count)
                {
                    Sets[i].SuggestedWeightPlaceholder = $"Last: {lastWeights[i]:F0}";
                }              
            }

            if (lastWeights.Count > 0)
            {
                var fill = $"Last: {lastWeights[^1]:F0}";
                for (int i = lastWeights.Count; i < Sets.Count; i++)
                {
                    Sets[i].SuggestedWeightPlaceholder = fill;
                }             
            }

            HasLastSession = true;

            ComputeProgressionSuggestion(lastSets, lastWeights, weightUnit);
        }

        public void ApplyPlaceholderToLastSet()
        {
            if (Sets.Count == 0)
            {
                return;
            }
            var last = Sets[^1];
            if (!string.IsNullOrEmpty(ProgressionSuggestion) && ProgressionIsIncrease)
            {
                var first = Sets.Count > 1 ? Sets[0].SuggestedWeightPlaceholder : string.Empty;
                last.SuggestedWeightPlaceholder = first;
            }
            else if (Sets.Count > 1)
            {
                last.SuggestedWeightPlaceholder = Sets[^2].SuggestedWeightPlaceholder;
            }
        }

        public void AddSet(string? weightUnit = null, Action<WorkoutSet>? onDeleted = null)
        {
            var unit = weightUnit ?? _defaultWeightUnit;
            var set = new WorkoutSet
            {
                Exercise = Exercise,
                ExerciseId = Exercise.Id,
                SetNumber = Sets.Count + 1,
                WeightUnit = unit,
                ParentGroup = this
            };

            set.DeleteCommand = new RelayCommand(() =>
            {
                RemoveSet(set);
                onDeleted?.Invoke(set);
            });

            Sets.Add(set);
            NotifySetStats();

            if (HasLastSession)
            {
                ApplyPlaceholderToLastSet();
            }
        }

        public void RemoveSet(WorkoutSet set)
        {
            Sets.Remove(set);
            for (int i = 0; i < Sets.Count; i++)
            {
                Sets[i].SetNumber = i + 1;
            }            
            NotifySetStats();
        }

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================

        private void ComputeProgressionSuggestion(List<WorkoutSet> lastSets, List<double> lastWeights, string weightUnit)
        {
            if (lastSets.Count == 0)
            {
                return;
            }

            bool isCompound = Exercise.MuscleGroup is "Legs" or "Back";
            double increment = weightUnit == "kg"
                ? (isCompound ? 2.5 : 1.25)
                : (isCompound ? 5.0 : 2.5);

            int inferredTarget = lastSets
                .GroupBy(s => s.Reps)
                .OrderByDescending(g => g.Count())
                .First().Key;

            double topWeight = lastWeights.Max();

            int hitsAtTop = lastSets
                .Where((s, i) => s.Reps >= inferredTarget && lastWeights[i] >= topWeight)
                .Count();

            int totalSetsAtTop = lastSets.Count(s => s.Reps > 0);

            if (hitsAtTop >= totalSetsAtTop)
            {
                double suggested = topWeight + increment;
                string unit = weightUnit;
                ProgressionSuggestion = $"↑ Ready to progress — try {suggested:F1} {unit} today";
                HasProgressionSuggestion = true;
                ProgressionIsIncrease = true;

                var hint = $"→ {suggested:F1} {unit}";
                foreach (var s in Sets)
                {
                    s.SuggestedWeightPlaceholder = hint;
                }                  
            }
            else if (hitsAtTop > 0)
            {
                ProgressionSuggestion = $"→ Consolidate at {topWeight:F0} {weightUnit} — match all sets first";
                HasProgressionSuggestion = true;
                ProgressionIsIncrease = false;
            }
        }

        private void NotifySetStats()
        {
            int reps = 0; double max = 0;
            foreach (var s in Sets)
            {
                reps += s.Reps;
                if (s.Weight > max)
                {
                    max = s.Weight;
                }                   
            }
            TotalReps = reps;
            MaxWeight = max;

            OnPropertyChanged(nameof(TotalReps));
            OnPropertyChanged(nameof(MaxWeight));
            OnPropertyChanged(nameof(SetCountLabel));
            OnPropertyChanged(nameof(HasSets));
            NotifyCompletionStats();
        }
    }
}
