using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace WorkoutTrackerV2.Models
{
    public partial class ExerciseGroup(Exercise exercise) : ObservableObject
    {
        public Exercise Exercise { get; set; } = exercise;
        public ObservableCollection<WorkoutSet> Sets { get; set; } = [];

        #region "ADD SET"
        public void AddSet()
        {
            var set = new WorkoutSet
            {
                Exercise = Exercise,
                ExerciseId = Exercise.Id,
                SetNumber = Sets.Count + 1,
                Reps = 0,
                Weight = 0,
                WeightUnit = "lbs",
                ParentGroup = this
            };
            set.DeleteCommand = new RelayCommand(() => RemoveSet(set));
            Sets.Add(set);
        }
        #endregion

        #region "REMOVE SET"
        public void RemoveSet(WorkoutSet set)
        {
            Sets.Remove(set);
            for (int i = 0; i < Sets.Count; i++)
            {
                Sets[i].SetNumber = i + 1;
            }                
        }
        #endregion
    }
}