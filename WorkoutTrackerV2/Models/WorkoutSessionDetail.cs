using System;
using System.Collections.Generic;
using System.Text;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutSessionDetail
    {
        public WorkoutSession Session { get; set; }
        public int SetCount { get; set; }
        public int TotalReps { get; set; }
        public double TotalWeight { get; set; }
        public List<WorkoutSet> Sets { get; set; }
    }
}
