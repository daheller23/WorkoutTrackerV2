using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLite;

namespace WorkoutTrackerV2.Models
{
    public partial class WorkoutSet : ObservableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int ExerciseId { get; set; }

        [Indexed]
        public int WorkoutSessionId { get; set; }

        [Ignore]
        public Exercise Exercise { get; set; } = new();

        [ObservableProperty]
        private int _setNumber;

        [ObservableProperty]
        private int _reps;

        [ObservableProperty]
        private double _weight;

        [ObservableProperty]
        private string _weightUnit = "lbs";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Ignore]
        public ExerciseGroup? ParentGroup { get; set; }

        [Ignore]
        public IRelayCommand? DeleteCommand { get; set; }
    }
}