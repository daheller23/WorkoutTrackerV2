
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public interface IWorkoutService
    {
        Task InitializeAsync();
        Task<List<Exercise>> GetAllExercisesAsync();
        Task<Exercise> GetExerciseAsync(int id);
    }
}
