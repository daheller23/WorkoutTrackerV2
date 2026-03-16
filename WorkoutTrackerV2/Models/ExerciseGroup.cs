using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace WorkoutTrackerV2.Models
{
    public partial class ExerciseGroup(Exercise exercise, string defaultWeightUnit = "lbs") : ObservableObject
    {
        private readonly string _defaultWeightUnit = defaultWeightUnit;
        public Exercise Exercise { get; set; } = exercise;
        public ObservableCollection<WorkoutSet> Sets { get; set; } = [];

        public void AddSet(string? weightUnit = null)
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
            set.DeleteCommand = new RelayCommand(() => RemoveSet(set));
            Sets.Add(set);
        }

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