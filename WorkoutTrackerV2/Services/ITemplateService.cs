using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public interface ITemplateService
    {
        WorkoutTemplate? PendingTemplate { get; set; }
        List<WorkoutTemplateSet> PendingTemplateSets { get; set; }
    }
}