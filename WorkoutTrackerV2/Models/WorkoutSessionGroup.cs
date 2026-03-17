using System.Collections.ObjectModel;

namespace WorkoutTrackerV2.Models
{
    public class WorkoutSessionGroup : ObservableCollection<WorkoutSessionDetail>
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public bool IsExpanded { get; set; } = true;

        public WorkoutSessionGroup(string title, string subtitle, IEnumerable<WorkoutSessionDetail> items) : base(items)
        {
            Title = title;
            Subtitle = subtitle;
        }
    }
}