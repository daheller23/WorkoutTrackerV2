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

        // Injected by AddWorkoutViewModel so sets can auto-trigger the rest timer
        // without holding a service reference. Set once after construction.
        public Action<string>? StartRestTimerAction { get; set; }

        public Exercise Exercise { get; set; } = exercise;

        // Private setter — Sets should never be replaced from outside the class
        // since that would silently break all CollectionView bindings.
        public ObservableCollection<WorkoutSet> Sets { get; } = [];

        public string SetCountLabel => Sets.Count == 1 ? "1 set" : $"{Sets.Count} sets";
        public bool HasSets => Sets.Count > 0;

        // Completion progress — drives the progress bar under each exercise header.
        public int CompletedSets => Sets.Count(s => s.IsCompleted);
        public int TotalSetCount => Sets.Count;
        public bool AllSetsCompleted => Sets.Count > 0 && Sets.All(s => s.IsCompleted);
        // 0.0–1.0 for ProgressBar.Progress binding — no converter needed.
        public double CompletionProgress => Sets.Count == 0 ? 0 : (double)CompletedSets / Sets.Count;

        // FIX 1+2: TotalReps and MaxWeight notify correctly.
        public int TotalReps { get; private set; }
        public double MaxWeight { get; private set; }

        // Previous session data — populated asynchronously after group is created.
        [ObservableProperty] private string _lastSessionSummary = string.Empty;
        [ObservableProperty] private bool _hasLastSession;

        // Each entry is "Reps × Weight Unit" — shown as chips in the XAML.
        public ObservableCollection<string> LastSessionChips { get; } = [];

        /// <summary>
        /// Called by AddWorkoutViewModel once the previous session fetch completes.
        /// Populates last-session chips and injects suggested weight placeholder
        /// into each set row so the user knows what they hit last time.
        /// </summary>
        public void SetLastSession(List<WorkoutSet> sets, string weightUnit)
        {
            LastSessionChips.Clear();

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

            for (int i = 0; i < lastSets.Count; i++)
            {
                var s = lastSets[i];
                double w = s.WeightUnit == weightUnit
                    ? s.Weight
                    : s.WeightUnit == "lbs" ? s.Weight * 0.453592 : s.Weight / 0.453592;
                LastSessionChips.Add($"{s.Reps} × {w:F0}");

                // Inject into the matching live set row.
                if (i < Sets.Count)
                    Sets[i].SuggestedWeightPlaceholder = $"Last: {w:F0}";
            }

            // Fill extra rows beyond what last session had.
            if (lastSets.Count > 0)
            {
                var last = lastSets[^1];
                double lastW = last.WeightUnit == weightUnit
                    ? last.Weight
                    : last.WeightUnit == "lbs" ? last.Weight * 0.453592 : last.Weight / 0.453592;
                for (int i = lastSets.Count; i < Sets.Count; i++)
                    Sets[i].SuggestedWeightPlaceholder = $"Last: {lastW:F0}";
            }

            HasLastSession = true;
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

            // ToggleCompletedCommand: flip IsCompleted + auto-start rest timer
            // when completing (not un-completing). CommandParameter = muscle group
            // so the rest timer service picks the right default duration.
            set.ToggleCompletedCommand = new RelayCommand<string>(muscleGroup =>
            {
                set.IsCompleted = !set.IsCompleted;
                NotifyCompletionStats();
                if (set.IsCompleted)
                    StartRestTimerAction?.Invoke(muscleGroup ?? Exercise.MuscleGroup);
            });

            Sets.Add(set);
            NotifySetStats();
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

        // Raised whenever a set is checked/unchecked or the set count changes.
        private void NotifyCompletionStats()
        {
            OnPropertyChanged(nameof(CompletedSets));
            OnPropertyChanged(nameof(TotalSetCount));
            OnPropertyChanged(nameof(AllSetsCompleted));
            OnPropertyChanged(nameof(CompletionProgress));
        }
    }
}
