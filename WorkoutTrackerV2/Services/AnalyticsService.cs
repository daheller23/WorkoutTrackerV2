using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IWorkoutService _workoutService;

        public AnalyticsService(IWorkoutService workoutService)
        {
            _workoutService = workoutService;
        }

        public async Task<List<DailyStats>> GetDailyStatsAsync(int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days).Date;
            var endDate = DateTime.Now.Date;
            var sessions = await _workoutService.GetSessionsAsync(startDate, endDate);

            var dailyStatsDict = new Dictionary<DateTime, DailyStats>();

            foreach (var session in sessions)
            {
                var sessionDate = session.Date.Date;
                var sets = await _workoutService.GetSetsForSessionAsync(session.Id);

                if (!dailyStatsDict.ContainsKey(sessionDate))
                {
                    dailyStatsDict[sessionDate] = new DailyStats { Date = sessionDate };
                }

                var stats = dailyStatsDict[sessionDate];
                stats.ExercisesCompleted = session.TotalExercises;
                stats.SetsCompleted += sets.Count;
                stats.TotalRepsCompleted += sets.Sum(s => s.Reps);
                stats.TotalWeightLifted += sets.Sum(s => s.Weight * s.Reps);
            }

            return dailyStatsDict.Values.OrderBy(x => x.Date).ToList();
        }

        public async Task<ExerciseProgress> GetExerciseProgressAsync(int exerciseId, int days = 30)
        {
            var sets = await _workoutService.GetExerciseHistoryAsync(exerciseId, days);
            var exercise = await _workoutService.GetExerciseAsync(exerciseId);

            var progress = new ExerciseProgress
            {
                ExerciseName = exercise.Name,
                Sets = sets,
                MaxWeight = sets.Count > 0 ? sets.Max(s => s.Weight) : 0,
                AverageWeight = sets.Count > 0 ? sets.Average(s => s.Weight) : 0,
                TotalReps = sets.Sum(s => s.Reps)
            };

            return progress;
        }

        public async Task<List<ExerciseProgress>> GetProgressForMuscleGroupAsync(string muscleGroup, int days = 30)
        {
            var exercises = await _workoutService.GetAllExercisesAsync();
            var muscleExercises = exercises.Where(e => e.MuscleGroup == muscleGroup).ToList();

            var progressList = new List<ExerciseProgress>();
            foreach (var exercise in muscleExercises)
            {
                var progress = await GetExerciseProgressAsync(exercise.Id, days);
                if (progress.Sets.Count > 0)
                {
                    progressList.Add(progress);
                }
            }

            return progressList.OrderByDescending(x => x.MaxWeight).ToList();
        }

        public async Task<Dictionary<string, double>> GetStrengthProgressAsync(int days = 30)
        {
            var exercises = await _workoutService.GetAllExercisesAsync();
            var strengthDict = new Dictionary<string, double>();

            foreach (var exercise in exercises)
            {
                var progress = await GetExerciseProgressAsync(exercise.Id, days);
                if (progress.MaxWeight > 0)
                {
                    strengthDict[exercise.Name] = progress.MaxWeight;
                }
            }

            return strengthDict.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        public async Task<int> GetCurrentStreak()
        {
            var allSessions = await _workoutService.GetAllSessionsAsync();
            if (allSessions.Count == 0)
                return 0;

            int streak = 0;
            var today = DateTime.Now.Date;

            for (int i = 0; i < 365; i++)
            {
                var checkDate = today.AddDays(-i);
                var hasSessionOnDate = allSessions.Any(s => s.Date.Date == checkDate);

                if (hasSessionOnDate)
                    streak++;
                else if (i > 0)
                    break;
            }

            return streak;
        }

        public async Task<double> GetAverageWorkoutDurationAsync(int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days).Date;
            var endDate = DateTime.Now.Date;
            var sessions = await _workoutService.GetSessionsAsync(startDate, endDate);

            if (sessions.Count == 0)
                return 0;

            return sessions.Average(s => s.Duration.TotalMinutes);
        }



    }
}
