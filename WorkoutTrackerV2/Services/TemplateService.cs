using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public class TemplateService : ITemplateService
    {
        public WorkoutTemplate? PendingTemplate { get; set; }
    }
}