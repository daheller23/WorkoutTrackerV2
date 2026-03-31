using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public class DashboardService(IWorkoutService workoutService, IAnalyticsService analyticsService) : IDashboardService
    {
        // ==============================================================================================================
        //
        //      PUBLIC METHODS
        //
        // ==============================================================================================================
        public async Task<HomeDashboardSummary> GetHomeDashboardSummaryAsync()
        {
            var totalWorkoutsTask = workoutService.GetTotalWorkoutCountAsync();
            var currentStreakTask = analyticsService.GetCurrentStreak();
            var lastWorkoutDateTask = workoutService.GetLastWorkoutDateAsync();
            var averageDurationTask = analyticsService.GetAverageWorkoutDurationAsync();
            var allSessionsTask = workoutService.GetAllSessionsAsync();
            var allExercisesTask = workoutService.GetAllExercisesAsync();
            await Task.WhenAll(totalWorkoutsTask, currentStreakTask, lastWorkoutDateTask, averageDurationTask, allSessionsTask, allExercisesTask);

            var allSessions = allSessionsTask.Result;
            var exerciseDict = allExercisesTask.Result.ToDictionary(e => e.Id);

            var today = DateTime.Today;
            var calWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var rollingWeekStart = today.AddDays(-7);
            var calWeekSessions = allSessions.Where(s => s.Date >= calWeekStart).ToList();
            var rollingWeekSessions = allSessions.Where(s => s.Date >= rollingWeekStart).ToList();
            var allRelevantIds = calWeekSessions.Select(s => s.Id).Union(rollingWeekSessions.Select(s => s.Id)).ToHashSet();
            var allRelevantSessions = allSessions.Where(s => allRelevantIds.Contains(s.Id)).ToList();
            var setTasks = allRelevantSessions.Select(s => workoutService.GetSetsForSessionAsync(s.Id)).ToList();
            var weekSetsArrays = await Task.WhenAll(setTasks);
            var weekSets = weekSetsArrays.SelectMany(s => s).ToList();
            var setsBySessionId = allRelevantSessions.Zip(weekSetsArrays, (session, sets) => (session.Id, sets)).ToDictionary(x => x.Id, x => (IEnumerable<WorkoutSet>)x.sets);
            var calWeekSets = calWeekSessions.Where(s => setsBySessionId.ContainsKey(s.Id)).SelectMany(s => setsBySessionId[s.Id]).ToList();
            var topMuscleGroup = GetMostTrainedMuscleGroup(rollingWeekSessions, weekSets, exerciseDict);

            return new HomeDashboardSummary()
            {
                TotalWorkouts = totalWorkoutsTask.Result,
                CurrentStreak = currentStreakTask.Result,
                LastWorkoutDate = lastWorkoutDateTask.Result,
                AverageDuration = averageDurationTask.Result,
                LastWorkoutSession = allSessions.FirstOrDefault(),
                RecentSessions = [.. allSessions.Skip(1).Take(3)],
                WorkoutsThisWeek = calWeekSessions.Count,
                SetsThisWeek = calWeekSets.Count,
                VolumeThisWeek = calWeekSets.Sum(s => s.Weight * s.Reps),
                TopMuscleGroup = topMuscleGroup
            };
        }

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================
        private string GetMostTrainedMuscleGroup(
                List<WorkoutSession> rollingSessions,
                List<WorkoutSet> weekSets,
                Dictionary<int, Exercise> exerciseDict)
        {
            if (rollingSessions.Count == 0 || weekSets.Count == 0) return string.Empty;

            var rollingIds = rollingSessions.Select(s => s.Id).ToHashSet();

            return weekSets
                .Where(set => rollingIds.Contains(set.WorkoutSessionId))
                .Where(set => exerciseDict.ContainsKey(set.ExerciseId))
                .GroupBy(set => exerciseDict[set.ExerciseId].MuscleGroup)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? string.Empty;
        }
    }
}
