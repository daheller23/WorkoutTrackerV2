using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public class AnalyticsService(IWorkoutService workoutService) : IAnalyticsService
    {
        #region "GET AVERAGE WORKOUT DURATION ASYNC"
        public async Task<double> GetAverageWorkoutDurationAsync(int days = 30)
        {
            var startDate = days == 0 ? DateTime.MinValue : DateTime.Now.AddDays(-days).Date;
            var sessions = await workoutService.GetSessionsAsync(startDate, DateTime.Now.Date);
            return sessions.Count == 0 ? 0 : sessions.Average(s => s.Duration.TotalMinutes);
        }
        #endregion

        #region "GET CURRENT STREAK"
        public async Task<int> GetCurrentStreak()
        {
            var allSessions = await workoutService.GetAllSessionsAsync();
            if (allSessions.Count == 0) return 0;

            // HashSet gives O(1) date lookup instead of O(n) Any() scan.
            var sessionDates = allSessions.Select(s => s.Date.Date).ToHashSet();

            int streak = 0;
            var today = DateTime.Now.Date;

            for (int i = 0; i < 365; i++)
            {
                if (sessionDates.Contains(today.AddDays(-i)))
                    streak++;
                else if (i > 0)
                    break;
            }
            return streak;
        }
        #endregion

        #region "GET DAILY STATS ASYNC"
        public async Task<List<DailyStats>> GetDailyStatsAsync(int days = 30)
        {
            var startDate = days == 0 ? DateTime.MinValue : DateTime.Now.AddDays(-days).Date;
            var sessions = await workoutService.GetSessionsAsync(startDate, DateTime.Now.Date);
            if (sessions.Count == 0) return [];

            // Fetch all sets for the date range concurrently.
            var setTasks = sessions.Select(s => workoutService.GetSetsForSessionAsync(s.Id)).ToList();
            var allSets = await Task.WhenAll(setTasks);

            var dailyStatsDict = new Dictionary<DateTime, DailyStats>();
            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                var sets = allSets[i];
                var sessionDate = session.Date.Date;

                if (!dailyStatsDict.TryGetValue(sessionDate, out var stats))
                {
                    stats = new DailyStats { Date = sessionDate };
                    dailyStatsDict[sessionDate] = stats;
                }

                stats.ExercisesCompleted = session.TotalExercises;
                stats.SetsCompleted += sets.Count;

                // FIX 5: Single loop instead of two separate .Sum() passes
                // over the same list.
                foreach (var s in sets)
                {
                    stats.TotalRepsCompleted += s.Reps;
                    stats.TotalWeightLifted += s.Weight * s.Reps;
                }
            }

            return dailyStatsDict.Values.OrderBy(x => x.Date).ToList();
        }
        #endregion

        #region "GET EXERCISE PROGRESS ASYNC"
        public async Task<ExerciseProgress> GetExerciseProgressAsync(int exerciseId, int days = 30)
        {
            // Fetch sets and exercise concurrently.
            var setsTask = workoutService.GetExerciseHistoryAsync(exerciseId, days);
            var exerciseTask = workoutService.GetExerciseAsync(exerciseId);
            await Task.WhenAll(setsTask, exerciseTask);

            var sets = setsTask.Result;
            var exercise = exerciseTask.Result;

            var points = sets
                .GroupBy(s => s.CreatedDate.Date)
                .Select(g => new ProgressPoint
                {
                    Date = g.Key,
                    MaxWeight = g.Max(s => s.Weight)
                })
                .OrderBy(p => p.Date)
                .ToList();

            // FIX 6: Single pass computes max, total weight, and total reps
            // instead of three separate LINQ iterations over the same list.
            double maxWeight = 0, totalWeight = 0;
            int totalReps = 0;
            foreach (var s in sets)
            {
                if (s.Weight > maxWeight) maxWeight = s.Weight;
                totalWeight += s.Weight;
                totalReps += s.Reps;
            }

            return new ExerciseProgress
            {
                ExerciseId = exerciseId,
                ExerciseName = exercise?.Name ?? string.Empty,
                MuscleGroup = exercise?.MuscleGroup ?? string.Empty,
                Sets = sets,
                MaxWeight = maxWeight,
                AverageWeight = sets.Count > 0 ? totalWeight / sets.Count : 0,
                TotalReps = totalReps,
                Points = points,
                EarliestMaxWeight = points.FirstOrDefault()?.MaxWeight ?? 0,
                LatestMaxWeight = points.LastOrDefault()?.MaxWeight ?? 0
            };
        }
        #endregion

        #region "GET MUSCLE GROUP PROGRESS ASYNC"
        public async Task<List<MuscleGroupProgress>> GetMuscleGroupProgressAsync(int days = 30)
        {
            // FIX 1+2: Fetch ALL sets for the date range in one query, then build
            // the exercise lookup from the cached exercise list. No per-exercise or
            // per-muscle-group DB calls — all grouping is done in-memory.
            var allSetsTask = workoutService.GetAllSetsAsync(days);
            var allExercisesTask = workoutService.GetAllExercisesAsync();
            await Task.WhenAll(allSetsTask, allExercisesTask);

            var allSets = allSetsTask.Result;
            var exerciseDict = allExercisesTask.Result.ToDictionary(e => e.Id);

            // Group sets by muscle group, then by exercise — entirely in-memory.
            var byMuscleGroup = allSets
                .Where(s => exerciseDict.ContainsKey(s.ExerciseId))
                .GroupBy(s => exerciseDict[s.ExerciseId].MuscleGroup);

            var result = new List<MuscleGroupProgress>();

            foreach (var mgGroup in byMuscleGroup)
            {
                var exerciseProgresses = mgGroup
                    .GroupBy(s => s.ExerciseId)
                    .Select(exGroup =>
                    {
                        var exercise = exerciseDict[exGroup.Key];
                        return BuildExerciseProgress(exercise, exGroup.ToList());
                    })
                    .Where(p => p.Sets.Count > 0)
                    .OrderByDescending(p => p.MaxWeight)
                    .ToList();

                if (exerciseProgresses.Count == 0) continue;

                result.Add(new MuscleGroupProgress
                {
                    MuscleGroup = mgGroup.Key,
                    Exercises = exerciseProgresses,
                    EarliestMaxWeight = exerciseProgresses.Min(e => e.EarliestMaxWeight),
                    LatestMaxWeight = exerciseProgresses.Max(e => e.LatestMaxWeight)
                });
            }

            return result;
        }
        #endregion

        #region "GET PROGRESS FOR MUSCLE GROUP ASYNC"
        public async Task<List<ExerciseProgress>> GetProgressForMuscleGroupAsync(
            string muscleGroup, int days = 30)
        {
            // FIX 1+2: One bulk set fetch + cached exercise list instead of
            // N per-exercise GetExerciseProgressAsync calls.
            var allSetsTask = workoutService.GetAllSetsAsync(days);
            var allExercisesTask = workoutService.GetAllExercisesAsync();
            await Task.WhenAll(allSetsTask, allExercisesTask);

            var exerciseDict = allExercisesTask.Result
                .Where(e => e.MuscleGroup == muscleGroup)
                .ToDictionary(e => e.Id);

            return allSetsTask.Result
                .Where(s => exerciseDict.ContainsKey(s.ExerciseId))
                .GroupBy(s => s.ExerciseId)
                .Select(g => BuildExerciseProgress(exerciseDict[g.Key], g.ToList()))
                .Where(p => p.Sets.Count > 0)
                .OrderByDescending(p => p.MaxWeight)
                .ToList();
        }
        #endregion

        #region "GET STRENGTH PROGRESS ASYNC"
        public async Task<Dictionary<string, double>> GetStrengthProgressAsync(int days = 30)
        {
            // FIX 1: One bulk set fetch + cached exercise list replaces
            // 65 concurrent GetExerciseProgressAsync calls (each doing 2 DB queries).
            var allSetsTask = workoutService.GetAllSetsAsync(days);
            var allExercisesTask = workoutService.GetAllExercisesAsync();
            await Task.WhenAll(allSetsTask, allExercisesTask);

            var exerciseDict = allExercisesTask.Result.ToDictionary(e => e.Id);

            return allSetsTask.Result
                .Where(s => exerciseDict.ContainsKey(s.ExerciseId) && s.Weight > 0)
                .GroupBy(s => s.ExerciseId)
                .Select(g => (
                    Name: exerciseDict[g.Key].Name,
                    MaxWeight: g.Max(s => s.Weight)
                ))
                .OrderByDescending(x => x.MaxWeight)
                .ToDictionary(x => x.Name, x => x.MaxWeight);
        }
        #endregion

        #region "GET PERSONAL RECORDS ASYNC"
        public async Task<List<PersonalRecord>> GetPersonalRecordsAsync(int days = 0)
        {
            // FIX 1: One bulk set fetch + cached exercise list replaces
            // 65 concurrent GetExerciseHistoryAsync calls.
            var allSetsTask = workoutService.GetAllSetsAsync(days);
            var allExercisesTask = workoutService.GetAllExercisesAsync();
            await Task.WhenAll(allSetsTask, allExercisesTask);

            var exerciseDict = allExercisesTask.Result.ToDictionary(e => e.Id);

            return allSetsTask.Result
                .Where(s => exerciseDict.ContainsKey(s.ExerciseId))
                .GroupBy(s => s.ExerciseId)
                .Select(g => BuildPersonalRecord(exerciseDict[g.Key], g.OrderBy(s => s.CreatedDate).ToList()))
                .Where(r => r is not null && r.History.Count > 0)
                .OrderByDescending(r => r!.BestWeight)
                .ToList()!;
        }
        #endregion

        #region "PRIVATE HELPERS"

        // FIX 3: Shared helper builds ExerciseProgress from an already-fetched set
        // list and Exercise object — no DB calls, no repeated LINQ iterations.
        // FIX 6: Single pass over sets for max, total weight, and total reps.
        private static ExerciseProgress BuildExerciseProgress(
            Exercise exercise, List<WorkoutSet> sets)
        {
            var points = sets
                .GroupBy(s => s.CreatedDate.Date)
                .Select(g => new ProgressPoint
                {
                    Date = g.Key,
                    MaxWeight = g.Max(s => s.Weight)
                })
                .OrderBy(p => p.Date)
                .ToList();

            double maxWeight = 0, totalWeight = 0;
            int totalReps = 0;
            foreach (var s in sets)
            {
                if (s.Weight > maxWeight) maxWeight = s.Weight;
                totalWeight += s.Weight;
                totalReps += s.Reps;
            }

            return new ExerciseProgress
            {
                ExerciseId = exercise.Id,
                ExerciseName = exercise.Name,
                MuscleGroup = exercise.MuscleGroup,
                Sets = sets,
                MaxWeight = maxWeight,
                AverageWeight = sets.Count > 0 ? totalWeight / sets.Count : 0,
                TotalReps = totalReps,
                Points = points,
                EarliestMaxWeight = points.FirstOrDefault()?.MaxWeight ?? 0,
                LatestMaxWeight = points.LastOrDefault()?.MaxWeight ?? 0
            };
        }

        // Builds a PersonalRecord by scanning sets in date order for progressive PRs.
        private static PersonalRecord? BuildPersonalRecord(
            Exercise exercise, List<WorkoutSet> sets)
        {
            if (sets.Count == 0) return null;

            double runningMax = 0;
            var history = new List<PersonalRecordEntry>();

            foreach (var set in sets)
            {
                if (set.Weight > runningMax)
                {
                    runningMax = set.Weight;
                    history.Add(new PersonalRecordEntry
                    {
                        Weight = set.Weight,
                        Reps = set.Reps,
                        Date = set.CreatedDate
                    });
                }
            }

            if (history.Count == 0) return null;

            var best = history.Last();
            return new PersonalRecord
            {
                ExerciseId = exercise.Id,
                ExerciseName = exercise.Name,
                MuscleGroup = exercise.MuscleGroup,
                BestWeight = best.Weight,
                BestReps = best.Reps,
                BestDate = best.Date,
                History = history
            };
        }

        #endregion
    }
}
