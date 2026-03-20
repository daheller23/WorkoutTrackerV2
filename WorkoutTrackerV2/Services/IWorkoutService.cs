using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public interface IWorkoutService
    {
        Task InitializeAsync();

        // Sessions
        Task<List<WorkoutSession>> GetAllSessionsAsync();
        Task<WorkoutSession?> GetSessionAsync(int id);
        Task<DateTime?> GetLastWorkoutDateAsync();
        Task<int> GetTotalWorkoutCountAsync();
        Task<List<WorkoutSession>> GetSessionsAsync(DateTime startDate, DateTime endDate);
        Task<int> SaveSessionAsync(WorkoutSession session);
        Task<int> DeleteSessionAsync(WorkoutSession session);

        // Sets
        Task<List<WorkoutSet>> GetSetsForSessionAsync(int sessionId);
        Task<List<WorkoutSet>> GetAllSetsAsync(int days = 0);
        Task<List<WorkoutSet>> GetExerciseHistoryAsync(int exerciseId, int days = 30);
        Task<int> SaveSetAsync(WorkoutSet set);
        Task SaveAllSetsAsync(List<WorkoutSet> sets);
        Task<int> DeleteSetAsync(WorkoutSet set);
        Task DeleteSetAsync(int id);

        // Exercises
        // FIX 5: Return type changed to IReadOnlyList<Exercise> to prevent callers
        // from mutating the shared cache instance (sort, remove, etc.).
        Task<IReadOnlyList<Exercise>> GetAllExercisesAsync();
        Task<Exercise?> GetExerciseAsync(int id);
        Task<List<int>> GetRecentExerciseIdsAsync(int days);
        Task<int> SaveExerciseAsync(Exercise exercise);
        Task DeleteExerciseAsync(int id);

        // Templates
        Task<List<WorkoutTemplate>> GetAllTemplatesAsync();
        Task<int> SaveTemplateAsync(WorkoutTemplate template);
        Task<List<WorkoutTemplateSet>> GetTemplateSetsAsync(int templateId);
        Task<int> SaveTemplateSetAsync(WorkoutTemplateSet set);
        Task SaveAllTemplateSetsAsync(List<WorkoutTemplateSet> sets);
        Task DeleteTemplateAsync(int templateId);

        // Admin
        Task ClearAllDataAsync();
    }
}
