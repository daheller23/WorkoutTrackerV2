
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public interface IWorkoutService
    {
        Task<int> DeleteSessionAsync(WorkoutSession session);
        Task<int> DeleteSetAsync(WorkoutSet set);
        Task DeleteSetAsync(int id);
        Task<List<WorkoutSet>> GetExerciseHistoryAsync(int exerciseId, int days = 30);
        Task<List<Exercise>> GetAllExercisesAsync();
        Task<List<WorkoutSession>> GetAllSessionsAsync();
        Task<Exercise> GetExerciseAsync(int id);
        Task<List<WorkoutSession>> GetSessionsAsync(DateTime startDate, DateTime endDate);
        Task<List<WorkoutSet>> GetSetsForSessionAsync(int sessionId);
        Task<int> GetTotalWorkoutCountAsync();
        Task<DateTime?> GetLastWorkoutDateAsync();
        Task InitializeAsync();
        Task<int> SaveSessionAsync(WorkoutSession session);
        Task<int> SaveSetAsync(WorkoutSet set);
        Task<List<WorkoutTemplate>> GetAllTemplatesAsync();
        Task<int> SaveTemplateAsync(WorkoutTemplate template);
        Task<List<WorkoutTemplateSet>> GetTemplateSetsAsync(int templateId);
        Task<int> SaveTemplateSetAsync(WorkoutTemplateSet set);
        Task DeleteTemplateAsync(int templateId);
        Task ClearAllDataAsync();
        Task<WorkoutSession?> GetSessionAsync(int id);
        Task<int> SaveExerciseAsync(Exercise exercise);
    }
}
