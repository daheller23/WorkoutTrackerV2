using WorkoutTrackerV2.Models;
namespace WorkoutTrackerV2.Services
{
    public interface IAnalyticsService
    {
        Task<double> GetAverageWorkoutDurationAsync(int days = 30);
        Task<int> GetCurrentStreak();
        Task<List<DailyStats>> GetDailyStatsAsync(int days = 30);
        Task<ExerciseProgress> GetExerciseProgressAsync(int exerciseId, int days = 30);
        Task<List<MuscleGroupProgress>> GetMuscleGroupProgressAsync(int days = 30);
        Task<List<ExerciseProgress>> GetProgressForMuscleGroupAsync(string muscleGroup, int days = 30);
        Task<Dictionary<string, double>> GetStrengthProgressAsync(int days = 30);
    }
}