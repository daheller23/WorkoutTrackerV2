
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public interface IWorkoutService
    {
        Task InitializeAsync();
        Task<List<Exercise>> GetAllExercisesAsync();
        Task<Exercise> GetExerciseAsync(int id);
        Task<int> SaveSessionAsync(WorkoutSession session);
        Task<int> SaveSetAsync(WorkoutSet set);
        Task<int> GetTotalWorkoutCountAsync();
        Task<DateTime?> GetLastWorkoutDateAsync();
        Task<List<WorkoutSession>> GetAllSessionsAsync();
        Task<List<WorkoutSession>> GetSessionsAsync(DateTime startDate, DateTime endDate);
        Task<List<WorkoutSet>> GetSetsForSessionAsync(int sessionId);
        Task<List<WorkoutSet>> GetExerciseHistoryAsync(int exerciseId, int days = 30);
        Task<int> DeleteSetAsync(WorkoutSet set);
        Task<int> DeleteSessionAsync(WorkoutSession session);
    }
}
