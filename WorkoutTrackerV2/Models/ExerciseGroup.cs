using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace WorkoutTrackerV2.Models
{
    public partial class ExerciseGroup(
        Exercise exercise,
        string defaultWeightUnit = "lbs") : ObservableObject
    {
        private readonly string _defaultWeightUnit = defaultWeightUnit;

        public Exercise Exercise { get; set; } = exercise;

        public ObservableCollection<WorkoutSet> Sets { get; } = [];

        public string SetCountLabel => Sets.Count == 1 ? "1 set" : $"{Sets.Count} sets";
        public bool HasSets => Sets.Count > 0;

        // ── Completion progress ───────────────────────────────────────────────
        public int CompletedSets => Sets.Count(s => s.IsCompleted);
        public int TotalSetCount => Sets.Count;
        public bool AllSetsCompleted => Sets.Count > 0 && Sets.All(s => s.IsCompleted);
        public double CompletionProgress => Sets.Count == 0 ? 0 : (double)CompletedSets / Sets.Count;

        [ObservableProperty] private int _totalReps = 0;
        [ObservableProperty] private double _maxWeight = 0;

        // ── Last session display ──────────────────────────────────────────────
        [ObservableProperty] private string _lastSessionSummary = string.Empty;
        [ObservableProperty] private bool _hasLastSession;

        public ObservableCollection<string> LastSessionChips { get; } = [];

        // ── Progressive overload suggestion ──────────────────────────────────
        // Shown as a banner above the set table when we have enough history to
        // make a meaningful recommendation. Empty string = hide the banner.
        [ObservableProperty] private string _progressionSuggestion = string.Empty;
        [ObservableProperty] private bool _hasProgressionSuggestion;
        [ObservableProperty] private bool _progressionIsIncrease; // drives colour (green vs amber)

        /// <summary>
        /// Called by AddWorkoutViewModel once the previous session fetch completes.
        /// Populates last-session chips, injects suggested weight placeholders,
        /// and computes the progressive overload recommendation.
        /// </summary>
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

            // ── Convert all last-session weights to current display unit ──────
            double Convert(WorkoutSet s) =>
                s.WeightUnit == weightUnit ? s.Weight
                : s.WeightUnit == "lbs" ? s.Weight * 0.453592
                                           : s.Weight / 0.453592;

            var lastWeights = lastSets.Select(Convert).ToList();

            // ── Populate chips and per-set placeholders ───────────────────────
            for (int i = 0; i < lastSets.Count; i++)
            {
                LastSessionChips.Add($"{lastSets[i].Reps} × {lastWeights[i]:F0}");
                if (i < Sets.Count)
                    Sets[i].SuggestedWeightPlaceholder = $"Last: {lastWeights[i]:F0}";
            }

            // Fill any extra rows beyond what last session had.
            if (lastWeights.Count > 0)
            {
                var fill = $"Last: {lastWeights[^1]:F0}";
                for (int i = lastWeights.Count; i < Sets.Count; i++)
                    Sets[i].SuggestedWeightPlaceholder = fill;
            }

            HasLastSession = true;

            // ── Progressive overload logic ────────────────────────────────────
            ComputeProgressionSuggestion(lastSets, lastWeights, weightUnit);
        }

        private void ComputeProgressionSuggestion(
            List<WorkoutSet> lastSets,
            List<double> lastWeights,
            string weightUnit)
        {
            if (lastSets.Count == 0) return;

            // Determine increment based on muscle group and unit.
            // Compound / leg movements: larger increment.
            // Isolation / upper accessory: smaller increment.
            bool isCompound = Exercise.MuscleGroup is "Legs" or "Back";
            double increment = weightUnit == "kg"
                ? (isCompound ? 2.5 : 1.25)
                : (isCompound ? 5.0 : 2.5);

            // Use the most common rep count as the inferred target.
            int inferredTarget = lastSets
                .GroupBy(s => s.Reps)
                .OrderByDescending(g => g.Count())
                .First().Key;

            // Highest weight lifted last session (already converted to display unit).
            double topWeight = lastWeights.Max();

            // Count how many sets hit the inferred target at top weight.
            int hitsAtTop = lastSets
                .Where((s, i) => s.Reps >= inferredTarget && lastWeights[i] >= topWeight)
                .Count();

            int totalSetsAtTop = lastSets.Count(s => s.Reps > 0);

            if (hitsAtTop >= totalSetsAtTop)
            {
                // All sets hit target — ready to progress.
                double suggested = topWeight + increment;
                string unit = weightUnit;
                ProgressionSuggestion = $"↑ Ready to progress — try {suggested:F1} {unit} today";
                HasProgressionSuggestion = true;
                ProgressionIsIncrease = true;

                // Update weight placeholders to the suggested target.
                var hint = $"→ {suggested:F1} {unit}";
                foreach (var s in Sets)
                    s.SuggestedWeightPlaceholder = hint;
            }
            else if (hitsAtTop > 0)
            {
                // Partial — consolidate at current weight.
                ProgressionSuggestion = $"→ Consolidate at {topWeight:F0} {weightUnit} — match all sets first";
                HasProgressionSuggestion = true;
                ProgressionIsIncrease = false;
            }
            // If hitsAtTop == 0 (missed target on all sets), no suggestion shown —
            // the last-session chips already tell the story.
        }

        // Called by AddSet when a new set is added after history already loaded,
        // so the new row also gets the correct placeholder.
        public void ApplyPlaceholderToLastSet()
        {
            if (Sets.Count == 0) return;
            var last = Sets[^1];
            if (!string.IsNullOrEmpty(ProgressionSuggestion) && ProgressionIsIncrease)
            {
                // Carry the suggested-weight hint to the new row.
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

            set.ToggleCompletedCommand = new RelayCommand<string>(_ =>
            {
                set.IsCompleted = !set.IsCompleted;
                NotifyCompletionStats();
            });

            Sets.Add(set);
            NotifySetStats();

            // If history is already loaded, propagate the placeholder to the new row.
            if (HasLastSession)
                ApplyPlaceholderToLastSet();
        }

        public void RemoveSet(WorkoutSet set)
        {
            Sets.Remove(set);
            for (int i = 0; i < Sets.Count; i++)
                Sets[i].SetNumber = i + 1;
            NotifySetStats();
        }

        public void NotifySetStatsPublic() => NotifySetStats();

        private void NotifySetStats()
        {
            int reps = 0; double max = 0;
            foreach (var s in Sets)
            {
                reps += s.Reps;
                if (s.Weight > max) max = s.Weight;
            }
            TotalReps = reps;
            MaxWeight = max;

            OnPropertyChanged(nameof(TotalReps));
            OnPropertyChanged(nameof(MaxWeight));
            OnPropertyChanged(nameof(SetCountLabel));
            OnPropertyChanged(nameof(HasSets));
            NotifyCompletionStats();
        }

        private void NotifyCompletionStats()
        {
            OnPropertyChanged(nameof(CompletedSets));
            OnPropertyChanged(nameof(TotalSetCount));
            OnPropertyChanged(nameof(AllSetsCompleted));
            OnPropertyChanged(nameof(CompletionProgress));
        }
    }
}
