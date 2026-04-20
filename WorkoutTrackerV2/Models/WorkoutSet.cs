using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite;

namespace WorkoutTrackerV2.Models
{
    public partial class WorkoutSet : ObservableObject
    {
        [ObservableProperty] private int _reps;
        [ObservableProperty] private int _setNumber;
        [ObservableProperty] private double _weight;
        [ObservableProperty] private string _weightUnit = "lbs";

        [property: Ignore]
        [ObservableProperty]
        private bool _isCompleted;

        [property: Ignore]
        [ObservableProperty]
        private bool _isPR;

        [property: Ignore]
        [ObservableProperty]
        private string _suggestedWeightPlaceholder = string.Empty;

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int WorkoutSessionId { get; set; }
        public int ExerciseId { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Today;

        [Ignore] public string CheckmarkText => IsCompleted ? "✓" : string.Empty;
        [Ignore] public string NumberBackground => IsCompleted ? "#4CAF50" : "#F0F7FF";
        [Ignore] public string NumberColor => IsCompleted ? "#FFFFFF" : "#1F77F0";
        [Ignore] public string RowBackground => IsCompleted ? "#F1FFF4" : "#FFFFFF";
        [Ignore] public Exercise? Exercise { get; set; }
        [Ignore] public IRelayCommand? DeleteCommand { get; set; }
        [Ignore] public ExerciseGroup? ParentGroup { get; set; }     
        [Ignore] public IRelayCommand<string>? ToggleCompletedCommand { get; set; }

        // ==============================================================================================================
        //
        //      PARTIAL METHODS
        //
        // ==============================================================================================================

        partial void OnIsCompletedChanged(bool value)
        {
            OnPropertyChanged(nameof(CheckmarkText));
            OnPropertyChanged(nameof(RowBackground));
            OnPropertyChanged(nameof(NumberBackground));
            OnPropertyChanged(nameof(NumberColor));
        }
    }
}
