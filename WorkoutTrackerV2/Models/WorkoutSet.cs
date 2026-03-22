using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite;

namespace WorkoutTrackerV2.Models
{
    public partial class WorkoutSet : ObservableObject
    {
        // ── Persisted columns ────────────────────────────────────────────────
        // Reps, Weight, WeightUnit and SetNumber are [ObservableProperty] so
        // that any post-construction mutation (CopyLastSet, template loading)
        // fires PropertyChanged and the bound Entry/Label updates immediately.
        // The other persisted columns (Id, WorkoutSessionId, etc.) are plain
        // properties — they are set once before the set enters the UI and never
        // mutated while bound, so they don't need change notification.

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int WorkoutSessionId { get; set; }
        public int ExerciseId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Today;

        [ObservableProperty] private int _setNumber;
        [ObservableProperty] private int _reps;
        [ObservableProperty] private double _weight;
        [ObservableProperty] private string _weightUnit = "lbs";

        // ── In-memory only — not persisted ──────────────────────────────────
        [Ignore] public Exercise? Exercise { get; set; }
        [Ignore] public ExerciseGroup? ParentGroup { get; set; }

        [Ignore] public IRelayCommand? DeleteCommand { get; set; }
        [Ignore] public IRelayCommand<string>? ToggleCompletedCommand { get; set; }

        // ── Completion state ─────────────────────────────────────────────────
        [property: Ignore]
        [ObservableProperty]
        private bool _isCompleted;

        [Ignore] public string CheckmarkText => IsCompleted ? "✓" : "";
        [Ignore] public string RowBackground => IsCompleted ? "#F1FFF4" : "#FFFFFF";
        [Ignore] public string NumberBackground => IsCompleted ? "#4CAF50" : "#F0F7FF";
        [Ignore] public string NumberColor => IsCompleted ? "#FFFFFF" : "#1F77F0";

        partial void OnIsCompletedChanged(bool value)
        {
            OnPropertyChanged(nameof(CheckmarkText));
            OnPropertyChanged(nameof(RowBackground));
            OnPropertyChanged(nameof(NumberBackground));
            OnPropertyChanged(nameof(NumberColor));
        }

        // ── Personal record flag ─────────────────────────────────────────────
        [property: Ignore]
        [ObservableProperty]
        private bool _isPR;

        // ── Overload suggestion ──────────────────────────────────────────────
        [property: Ignore]
        [ObservableProperty]
        private string _suggestedWeightPlaceholder = "";
    }
}
