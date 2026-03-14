using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public class AnalyticsService(IWorkoutService workoutService) : IAnalyticsService
    {
        #region "GET AVERAGE WORKOUT DURATION ASYNC"
        public async Task<double> GetAverageWorkoutDurationAsync(int days = 30)
        {
            var startDate = DateTime.Now.AddDays(-days).Date;
            var endDate = DateTime.Now.Date;
            var sessions = await workoutService.GetSessionsAsync(startDate, endDate);

            if (sessions.Count == 0)
            {
                return 0;
            }
            return sessions.Average(s => s.Duration.TotalMinutes);
        }
        #endregion

        #region "GET CURRENT STREAK"
        public async Task<int> GetCurrentStreak()
        {
            var allSessions = await workoutService.GetAllSessionsAsync();
            if (allSessions.Count == 0)
            {
                return 0;
            }

            int streak = 0;
            var today = DateTime.Now.Date;

            for (int i = 0; i < 365; i++)
            {
                var checkDate = today.AddDays(-i);
                var hasSessionOnDate = allSessions.Any(s => s.Date.Date == checkDate);

                if (hasSessionOnDate)
                {
                    streak++;
                }
                else if (i > 0)
                {
                    break;
                }
            }
            return streak;
        }
        #endregion

        #region "GET DAILY STATS ASYNC"
        public async Task<List<DailyStats>> GetDailyStatsAsync(int days = 30)
        {
            var dailyStatsDict = new Dictionary<DateTime, DailyStats>();
            var startDate = DateTime.Now.AddDays(-days).Date;
            var endDate = DateTime.Now.Date;
            var sessions = await workoutService.GetSessionsAsync(startDate, endDate);
            foreach (var session in sessions)
            {
                var sessionDate = session.Date.Date;
                var sets = await workoutService.GetSetsForSessionAsync(session.Id);

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
        #endregion

        #region "GET EXERCISE PROGRESS ASYNC"
        public async Task<ExerciseProgress> GetExerciseProgressAsync(int exerciseId, int days = 30)
        {
            var sets = await workoutService.GetExerciseHistoryAsync(exerciseId, days);
            var exercise = await workoutService.GetExerciseAsync(exerciseId);
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
        #endregion

        #region "GET PROGRESS FOR MUSCLE GROUP ASYNC"
        public async Task<List<ExerciseProgress>> GetProgressForMuscleGroupAsync(string muscleGroup, int days = 30)
        {
            var progressList = new List<ExerciseProgress>();
            var exercises = await workoutService.GetAllExercisesAsync();
            var muscleExercises = exercises.Where(e => e.MuscleGroup == muscleGroup).ToList();           
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
        #endregion

        #region "GET STRENGTH PROGRESS ASYNC"
        public async Task<Dictionary<string, double>> GetStrengthProgressAsync(int days = 30)
        {  
            var strengthDict = new Dictionary<string, double>();
            var exercises = await workoutService.GetAllExercisesAsync();
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
        #endregion

    }
}
