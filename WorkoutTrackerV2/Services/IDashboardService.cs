using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public interface IDashboardService
    {
        public Task<HomeDashboardSummary> GetHomeDashboardSummaryAsync();
    }
}
