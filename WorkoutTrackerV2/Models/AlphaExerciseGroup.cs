using System.Collections.ObjectModel;
namespace WorkoutTrackerV2.Models
{
    public class AlphaExerciseGroup : ObservableCollection<Exercise>
    {
        public string Letter { get; set; } = string.Empty;
        public AlphaExerciseGroup(string letter, IEnumerable<Exercise> exercises) : base(exercises) => Letter = letter;
    }
}