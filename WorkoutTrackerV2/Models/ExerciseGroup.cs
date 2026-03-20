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

        // FIX 5: Private setter — Sets should never be replaced from outside
        // the class since that would silently break all CollectionView bindings
        // on the existing ObservableCollection instance.
        public ObservableCollection<WorkoutSet> Sets { get; } = [];

        public string SetCountLabel => Sets.Count == 1 ? "1 set" : $"{Sets.Count} sets";
        public bool HasSets => Sets.Count > 0;

        // FIX 1+2: TotalReps and MaxWeight now notify correctly — they are
        // computed from Sets but were not raising PropertyChanged after AddSet
        // or RemoveSet, causing bound labels to show stale values. Both are
        // computed together in one loop inside NotifySetStats() to avoid two
        // separate LINQ passes over Sets on every set operation.
        public int TotalReps { get; private set; }
        public double MaxWeight { get; private set; }

        // onDeleted is an optional callback injected by the ViewModel so that
        // when a set's DeleteCommand fires it can call both group.RemoveSet (which
        // updates SetCountLabel) AND ViewModel.UpdateTotals (which updates TotalSets).
        // Without this, DeleteCommand only reached RemoveSet and TotalSets never
        // decremented. ExerciseGroup intentionally has no direct ViewModel reference.
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
        }

        public void RemoveSet(WorkoutSet set)
        {
            Sets.Remove(set);
            // Renumber remaining sets to keep SetNumber sequential.
            for (int i = 0; i < Sets.Count; i++)
                Sets[i].SetNumber = i + 1;
            NotifySetStats();
        }

        // FIX 1+2: Single loop computes TotalReps and MaxWeight together, then
        // raises all four PropertyChanged notifications in one call site instead
        // of duplicating the notification calls in AddSet and RemoveSet.
        // Public overload used by AddWorkoutViewModel after template loading
        // where sets are added via Sets.Add() directly rather than AddSet().
        public void NotifySetStatsPublic() => NotifySetStats();

        private void NotifySetStats()
        {
            int reps = 0;
            double max = 0;
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
        }
    }
}
