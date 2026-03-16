using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public class AnalyticsService(IWorkoutService workoutService) : IAnalyticsService
    {
        #region "GET AVERAGE WORKOUT DURATION ASYNC"
        public async Task<double> GetAverageWorkoutDurationAsync(int days = 30)
        {
            var startDate = days == 0 ? DateTime.MinValue : DateTime.Now.AddDays(-days).Date;
            var endDate = DateTime.Now.Date;
            var sessions = await workoutService.GetSessionsAsync(startDate, endDate);
            return sessions.Count == 0 ? 0 : sessions.Average(s => s.Duration.TotalMinutes);
        }
        #endregion

        #region "GET CURRENT STREAK"
        public async Task<int> GetCurrentStreak()
        {
            var allSessions = await workoutService.GetAllSessionsAsync();
            if (allSessions.Count == 0) return 0;

            // Use a HashSet for O(1) lookup instead of Any() which is O(n)
            var sessionDates = allSessions.Select(s => s.Date.Date).ToHashSet();

            int streak = 0;
            var today = DateTime.Now.Date;

            for (int i = 0; i < 365; i++)
            {
                var checkDate = today.AddDays(-i);
                if (sessionDates.Contains(checkDate))
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
            var endDate = DateTime.Now.Date;
            var sessions = await workoutService.GetSessionsAsync(startDate, endDate);

            if (sessions.Count == 0) return [];

            // Fetch all sets in parallel instead of one session at a time
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
                stats.TotalRepsCompleted += sets.Sum(s => s.Reps);
                stats.TotalWeightLifted += sets.Sum(s => s.Weight * s.Reps);
            }

            return dailyStatsDict.Values.OrderBy(x => x.Date).ToList();
        }
        #endregion

        #region "GET EXERCISE PROGRESS ASYNC"
        public async Task<ExerciseProgress> GetExerciseProgressAsync(int exerciseId, int days = 30)
        {
            // Fetch sets and exercise in parallel
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

            return new ExerciseProgress
            {
                ExerciseId = exerciseId,
                ExerciseName = exercise.Name,
                MuscleGroup = exercise.MuscleGroup,
                Sets = sets,
                MaxWeight = sets.Count > 0 ? sets.Max(s => s.Weight) : 0,
                AverageWeight = sets.Count > 0 ? sets.Average(s => s.Weight) : 0,
                TotalReps = sets.Sum(s => s.Reps),
                Points = points,
                EarliestMaxWeight = points.FirstOrDefault()?.MaxWeight ?? 0,
                LatestMaxWeight = points.LastOrDefault()?.MaxWeight ?? 0
            };
        }
        #endregion

        #region "GET MUSCLE GROUP PROGRESS ASYNC"
        public async Task<List<MuscleGroupProgress>> GetMuscleGroupProgressAsync(int days = 30)
        {
            var muscleGroups = new[] { "Arms", "Back", "Chest", "Core", "Legs", "Shoulders" };

            // Fetch all muscle groups in parallel
            var tasks = muscleGroups.Select(mg => GetProgressForMuscleGroupAsync(mg, days)).ToList();
            var results = await Task.WhenAll(tasks);

            return muscleGroups
                .Zip(results, (mg, exercises) => (mg, exercises))
                .Where(x => x.exercises.Count > 0)
                .Select(x => new MuscleGroupProgress
                {
                    MuscleGroup = x.mg,
                    Exercises = x.exercises,
                    EarliestMaxWeight = x.exercises.Min(e => e.EarliestMaxWeight),
                    LatestMaxWeight = x.exercises.Max(e => e.LatestMaxWeight)
                })
                .ToList();
        }
        #endregion

        #region "GET PROGRESS FOR MUSCLE GROUP ASYNC"
        public async Task<List<ExerciseProgress>> GetProgressForMuscleGroupAsync(string muscleGroup, int days = 30)
        {
            var exercises = await workoutService.GetAllExercisesAsync();
            var muscleExercises = exercises.Where(e => e.MuscleGroup == muscleGroup).ToList();

            // Fetch all exercise progress in parallel
            var progressTasks = muscleExercises
                .Select(e => GetExerciseProgressAsync(e.Id, days))
                .ToList();
            var allProgress = await Task.WhenAll(progressTasks);

            return allProgress
                .Where(p => p.Sets.Count > 0)
                .OrderByDescending(p => p.MaxWeight)
                .ToList();
        }
        #endregion

        #region "GET STRENGTH PROGRESS ASYNC"
        public async Task<Dictionary<string, double>> GetStrengthProgressAsync(int days = 30)
        {
            var exercises = await workoutService.GetAllExercisesAsync();

            // Fetch all exercise progress in parallel
            var progressTasks = exercises
                .Select(e => GetExerciseProgressAsync(e.Id, days))
                .ToList();
            var allProgress = await Task.WhenAll(progressTasks);

            return allProgress
                .Where(p => p.MaxWeight > 0)
                .OrderByDescending(p => p.MaxWeight)
                .ToDictionary(p => p.ExerciseName, p => p.MaxWeight);
        }
        #endregion

        #region "GET PERSONAL RECORDS ASYNC"
        public async Task<List<PersonalRecord>> GetPersonalRecordsAsync(int days = 0)
        {
            var exercises = await workoutService.GetAllExercisesAsync();
            var recordTasks = exercises
                .Select(e => GetPersonalRecordForExerciseAsync(e, days))
                .ToList();
            var allRecords = await Task.WhenAll(recordTasks);
            return allRecords
                .Where(r => r is not null && r.History.Count > 0)
                .OrderByDescending(r => r!.BestWeight)
                .ToList()!;
        }

        private async Task<PersonalRecord?> GetPersonalRecordForExerciseAsync(Exercise exercise, int days)
        {
            var sets = await workoutService.GetExerciseHistoryAsync(exercise.Id, days);
            if (sets.Count == 0) return null;

            // Find all times a new PR was set
            double runningMax = 0;
            var history = new List<PersonalRecordEntry>();

            foreach (var set in sets.OrderBy(s => s.CreatedDate))
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