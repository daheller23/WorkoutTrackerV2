using System;
using System.Collections.Generic;
using System.Text;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public interface IAnalyticsService
    {
        Task<int> GetCurrentStreak();
        Task<double> GetAverageWorkoutDurationAsync(int days = 30);
        Task<Dictionary<string, double>> GetStrengthProgressAsync(int days = 30);
        Task<List<ExerciseProgress>> GetProgressForMuscleGroupAsync(string muscleGroup, int days = 30);
        Task<ExerciseProgress> GetExerciseProgressAsync(int exerciseId, int days = 30);
        Task<List<DailyStats>> GetDailyStatsAsync(int days = 30);
    }
}
