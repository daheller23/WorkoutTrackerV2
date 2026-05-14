using System;
using System.Collections.Generic;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutDraft
    {
        public string WorkoutName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime SelectedDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public List<DraftExerciseGroup> Groups { get; set; } = [];
    }

    public class DraftExerciseGroup
    {
        public int ExerciseId { get; set; }
        public List<DraftSet> Sets { get; set; } = [];
    }

    public class DraftSet
    {
        public int Reps { get; set; }
        public double Weight { get; set; }
        public string WeightUnit { get; set; } = "lbs";
        public bool IsCompleted { get; set; }
    }
}