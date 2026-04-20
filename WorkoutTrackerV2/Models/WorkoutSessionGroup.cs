using System.Collections.ObjectModel;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutSessionGroup(string title, string subtitle, IEnumerable<WorkoutSessionDetail> items) : ObservableCollection<WorkoutSessionDetail>(items)
    {
        public string Subtitle { get; set; } = subtitle;
        public string Title { get; set; } = title;   
        public bool IsExpanded { get; set; } = true;
    }
}