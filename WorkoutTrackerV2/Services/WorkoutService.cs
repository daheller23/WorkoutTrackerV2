using SQLite;
using WorkoutTrackerV2.Helpers;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public class WorkoutService : IWorkoutService
    {
        private readonly string _dbPath;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private SQLiteAsyncConnection _database = null!;
        private const string DbFileName = "workout_tracker.db3";
        private bool _initialized;
        private List<Exercise>? _exerciseCache;

        private class PrResult
        {
            public int ExerciseId { get; set; }
            public double MaxWeight { get; set; }
        }

        public WorkoutService(string? dbPath = null)
        {
            _dbPath = string.IsNullOrEmpty(dbPath)
                    ? Path.Combine(FileSystem.AppDataDirectory, DbFileName)
                    : dbPath;
        }

        // ==============================================================================================================
        //
        //      PUBLIC METHODS
        //
        // ==============================================================================================================

        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initLock.WaitAsync();
            try
            {
                if (_initialized)
                {
                    return;
                }

                _database = new SQLiteAsyncConnection(_dbPath);
                await _database.CreateTableAsync<Exercise>();
                await _database.CreateTableAsync<WorkoutSession>();
                await _database.CreateTableAsync<WorkoutSet>();
                await _database.CreateTableAsync<WorkoutTemplate>();
                await _database.CreateTableAsync<WorkoutTemplateSet>();

                await ExerciseMigrationHelper.RunSubMuscleMigrationAsync(_database);

                if (await _database.Table<Exercise>().CountAsync() == 0)
                {
                    await SeedDefaultExercises();
                }

                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<WorkoutSession?> GetPreviousSessionByDayAsync(int currentId, string dayName, DateTime currentDate)
        {
            await EnsureInitializedAsync();

            return await _database.Table<WorkoutSession>()
                .Where(s => s.Id != currentId
                          && s.DayName == dayName
                          && s.Date < currentDate)
                .OrderByDescending(s => s.Date)
                .FirstOrDefaultAsync();
        }

        public async Task<Dictionary<int, double>> GetPersonalRecordsAsync(List<int> exerciseIds)
        {
            await EnsureInitializedAsync();

            if (exerciseIds == null || exerciseIds.Count == 0)
            {
                return new Dictionary<int, double>();
            }

            // Create a comma-separated list of placeholders (?,?,?) matching the number of exercises
            var placeholders = string.Join(",", exerciseIds.Select(_ => "?"));

            // Native SQL query to let SQLite handle the grouping/max logic efficiently
            var query = $@"
                SELECT ExerciseId, MAX(Weight) AS MaxWeight 
                FROM WorkoutSet 
                WHERE ExerciseId IN ({placeholders}) 
                GROUP BY ExerciseId";

            // Execute query and cast list to object array
            var results = await _database.QueryAsync<PrResult>(query, exerciseIds.Cast<object>().ToArray());

            return results.ToDictionary(r => r.ExerciseId, r => r.MaxWeight);
        }

        public async Task<List<WorkoutSession>> GetAllSessionsAsync()
        {
            await EnsureInitializedAsync();
            return await _database.Table<WorkoutSession>()
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }

        public async Task<WorkoutSession?> GetSessionAsync(int id)
        {
            await EnsureInitializedAsync();
            return await _database.FindAsync<WorkoutSession>(id);
        }

        public async Task<DateTime?> GetLastWorkoutDateAsync()
        {
            await EnsureInitializedAsync();
            var lastSession = await _database.Table<WorkoutSession>()
                .OrderByDescending(x => x.Date)
                .FirstOrDefaultAsync();
            return lastSession?.Date;
        }

        public async Task<int> GetTotalWorkoutCountAsync()
        {
            await EnsureInitializedAsync();
            return await _database.Table<WorkoutSession>().CountAsync();
        }

        public async Task<List<WorkoutSession>> GetSessionsAsync(DateTime startDate, DateTime endDate)
        {
            await EnsureInitializedAsync();
            return await _database.Table<WorkoutSession>()
                .Where(x => x.Date >= startDate && x.Date <= endDate)
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }

        public async Task<int> SaveSessionAsync(WorkoutSession session)
        {
            await EnsureInitializedAsync();
            if (session.Id == 0)
            {
                await _database.InsertAsync(session);
                return session.Id;
            }
            await _database.UpdateAsync(session);
            return session.Id;
        }

        public async Task<int> DeleteSessionAsync(WorkoutSession session)
        {
            await EnsureInitializedAsync();
            return await _database.DeleteAsync(session);
        }

        public async Task<List<WorkoutSet>> GetSetsForSessionAsync(int sessionId)
        {
            await EnsureInitializedAsync();
            return await _database.Table<WorkoutSet>()
                .Where(x => x.WorkoutSessionId == sessionId)
                .OrderBy(x => x.SetNumber)
                .ToListAsync();
        }

        public async Task<List<WorkoutSet>> GetAllSetsAsync(int days = 0)
        {
            await EnsureInitializedAsync();
            var startDate = days == 0 ? DateTime.MinValue : DateTime.Now.AddDays(-days);
            return await _database.Table<WorkoutSet>()
                .Where(x => x.CreatedDate >= startDate)
                .OrderBy(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<WorkoutSet>> GetExerciseHistoryAsync(int exerciseId, int days = 30)
        {
            await EnsureInitializedAsync();
            var startDate = days == 0 ? DateTime.MinValue : DateTime.Now.AddDays(-days);
            return await _database.Table<WorkoutSet>()
                .Where(x => x.ExerciseId == exerciseId && x.CreatedDate >= startDate)
                .OrderBy(x => x.CreatedDate)
                .ToListAsync();
        }

        public async Task<int> SaveSetAsync(WorkoutSet set)
        {
            await EnsureInitializedAsync();
            if (set.Id == 0)
                return await _database.InsertAsync(set);
            return await _database.UpdateAsync(set);
        }

        public async Task DeleteSetsForSessionAsync(int sessionId)
        {
            await EnsureInitializedAsync();
            await _database.ExecuteAsync(
                "DELETE FROM WorkoutSet WHERE WorkoutSessionId = ?", sessionId);
        }

        public async Task SaveAllSetsAsync(List<WorkoutSet> sets)
        {
            await EnsureInitializedAsync();
            await _database.InsertAllAsync(sets);
        }

        public async Task<int> DeleteSetAsync(WorkoutSet set)
        {
            await EnsureInitializedAsync();
            return await _database.DeleteAsync(set);
        }

        public async Task DeleteSetAsync(int id)
        {
            await EnsureInitializedAsync();
            await _database.DeleteAsync<WorkoutSet>(id);
        }

        public async Task<IReadOnlyList<Exercise>> GetAllExercisesAsync()
        {
            await EnsureInitializedAsync();
            _exerciseCache ??= await _database.Table<Exercise>().ToListAsync();
            return _exerciseCache.AsReadOnly();
        }

        public async Task<Exercise?> GetExerciseAsync(int id)
        {
            await EnsureInitializedAsync();
            return await _database.FindAsync<Exercise>(id);
        }

        public async Task<List<int>> GetRecentExerciseIdsAsync(int days)
        {
            await EnsureInitializedAsync();
            var startDate = DateTime.Now.AddDays(-days);
            return await _database.QueryScalarsAsync<int>(
                "SELECT DISTINCT ExerciseId FROM WorkoutSet WHERE CreatedDate >= ?", startDate);
        }

        public async Task<int> SaveExerciseAsync(Exercise exercise)
        {
            await EnsureInitializedAsync();
            int result;
            if (exercise.Id == 0)
                result = await _database.InsertAsync(exercise);
            else
                result = await _database.UpdateAsync(exercise);
            _exerciseCache = null;
            return result;
        }

        public async Task DeleteExerciseAsync(int id)
        {
            await EnsureInitializedAsync();
            await _database.DeleteAsync<Exercise>(id);
            _exerciseCache = null;
        }

        public async Task<List<WorkoutTemplate>> GetAllTemplatesAsync()
        {
            await EnsureInitializedAsync();
            return await _database.Table<WorkoutTemplate>()
                .OrderBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<int> SaveTemplateAsync(WorkoutTemplate template)
        {
            await EnsureInitializedAsync();
            if (template.Id == 0)
            {
                await _database.InsertAsync(template);
                return template.Id;
            }
            await _database.UpdateAsync(template);
            return template.Id;
        }

        public async Task UpdateTemplateAsync(WorkoutTemplate template)
        {
            await EnsureInitializedAsync();
            await _database.UpdateAsync(template);
        }

        public async Task<List<WorkoutTemplateSet>> GetTemplateSetsAsync(int templateId)
        {
            await EnsureInitializedAsync();
            return await _database.Table<WorkoutTemplateSet>()
                .Where(x => x.TemplateId == templateId)
                .OrderBy(x => x.SetNumber)
                .ToListAsync();
        }

        public async Task<int> SaveTemplateSetAsync(WorkoutTemplateSet set)
        {
            await EnsureInitializedAsync();
            if (set.Id == 0)
                return await _database.InsertAsync(set);
            return await _database.UpdateAsync(set);
        }

        public async Task SaveAllTemplateSetsAsync(List<WorkoutTemplateSet> sets)
        {
            await EnsureInitializedAsync();
            await _database.InsertAllAsync(sets);
        }

        public async Task DeleteTemplateAsync(int templateId)
        {
            await EnsureInitializedAsync();
            await _database.RunInTransactionAsync(db =>
            {
                db.Execute(
                    "DELETE FROM WorkoutTemplateSet WHERE TemplateId = ?", templateId);
                db.Delete<WorkoutTemplate>(templateId);
            });
        }

        public async Task ClearAllDataAsync()
        {
            await EnsureInitializedAsync();
            await _database.RunInTransactionAsync(db =>
            {
                db.DeleteAll<Exercise>();
                db.DeleteAll<WorkoutSet>();
                db.DeleteAll<WorkoutSession>();
                db.DeleteAll<WorkoutTemplate>();
                db.DeleteAll<WorkoutTemplateSet>();
            });
            _exerciseCache = null;
            await SeedDefaultExercises();
        }

        public async Task<List<WorkoutSet>> GetSetsForSessionsAsync(List<int> sessionIds)
        {
            await EnsureInitializedAsync();

            return await _database.Table<WorkoutSet>()
                .Where(x => sessionIds.Contains(x.WorkoutSessionId))
                .OrderBy(x => x.WorkoutSessionId)
                .ThenBy(x => x.SetNumber)
                .ToListAsync();
        }

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================
        private Task EnsureInitializedAsync() => _initialized ? Task.CompletedTask : InitializeAsync();

        private async Task SeedDefaultExercises()
        {
            var defaultExercises = new List<Exercise>
            {
                // Biceps
                new() { Name = "Barbell Curls",                  MuscleGroup = "Biceps" },
                new() { Name = "Dumbbell Curls",                 MuscleGroup = "Biceps" },
                new() { Name = "Hammer Curls",                   MuscleGroup = "Biceps" },
                new() { Name = "Preacher Curls",                 MuscleGroup = "Biceps" },
                new() { Name = "Reverse Curls",                  MuscleGroup = "Biceps" },

                // Triceps
                new() { Name = "Bench Dip",                      MuscleGroup = "Triceps" },
                new() { Name = "Skull Crushers",                 MuscleGroup = "Triceps" },
                new() { Name = "Tricep Dips",                    MuscleGroup = "Triceps" },
                new() { Name = "Tricep Extensions",              MuscleGroup = "Triceps" },
                new() { Name = "Tricep Pushdowns with Bar",      MuscleGroup = "Triceps" },
                new() { Name = "Tricep Pushdowns with Ropes",    MuscleGroup = "Triceps" },

                // Forearms
                new() { Name = "Wrist Curls",                    MuscleGroup = "Forearms" },

                // Back
                new() { Name = "Back Assisted Row",              MuscleGroup = "Back" },
                new() { Name = "Barbell Rows",                   MuscleGroup = "Back" },
                new() { Name = "Chin-ups",                       MuscleGroup = "Back" },
                new() { Name = "Close Grip Low Pulley Rows",     MuscleGroup = "Back" },
                new() { Name = "Lat Pulldowns",                  MuscleGroup = "Back" },
                new() { Name = "Pull-ups",                       MuscleGroup = "Back" },
                new() { Name = "T-Bar Row",                      MuscleGroup = "Back" },
                new() { Name = "Wide Grip Low Pulley Rows",      MuscleGroup = "Back" },

                // Chest
                new() { Name = "Bench Press",                    MuscleGroup = "Chest" },
                new() { Name = "Cable Crossover",                MuscleGroup = "Chest" },
                new() { Name = "Declined Bench Press",           MuscleGroup = "Chest" },
                new() { Name = "Declined Smith Machine Press",   MuscleGroup = "Chest" },
                new() { Name = "Dumbbell Flyes",                 MuscleGroup = "Chest" },
                new() { Name = "Incline Bench Press",            MuscleGroup = "Chest" },
                new() { Name = "Inclined Smith Machine Press",   MuscleGroup = "Chest" },
                new() { Name = "Pec Deck",                       MuscleGroup = "Chest" },
                new() { Name = "Push-ups",                       MuscleGroup = "Chest" },
                new() { Name = "Smith Machine Press",            MuscleGroup = "Chest" },

                // Core
                new() { Name = "Ab Wheel",                       MuscleGroup = "Core" },
                new() { Name = "Ball Slams",                     MuscleGroup = "Core" },
                new() { Name = "Bicycle Crunches",               MuscleGroup = "Core" },
                new() { Name = "Cable Crunches",                 MuscleGroup = "Core" },
                new() { Name = "Flutter Kicks",                  MuscleGroup = "Core" },
                new() { Name = "Knee to Chest",                  MuscleGroup = "Core" },
                new() { Name = "Planks",                         MuscleGroup = "Core" },
                new() { Name = "Russian Twists",                 MuscleGroup = "Core" },
                new() { Name = "Weighted Sit-ups",               MuscleGroup = "Core" },

                // Legs
                new() { Name = "Back Squats",                    MuscleGroup = "Legs" },
                new() { Name = "Box Jumps",                      MuscleGroup = "Legs" },
                new() { Name = "Bulgarian Split Squat",          MuscleGroup = "Legs" },
                new() { Name = "Calf Raises",                    MuscleGroup = "Legs" },
                new() { Name = "Deadlifts",                      MuscleGroup = "Legs" },
                new() { Name = "Front Squats",                   MuscleGroup = "Legs" },
                new() { Name = "Goblet Squat",                   MuscleGroup = "Legs" },
                new() { Name = "Hack Squat",                     MuscleGroup = "Legs" },
                new() { Name = "Hip Abductor",                   MuscleGroup = "Legs" },
                new() { Name = "Hip Adductor",                   MuscleGroup = "Legs" },
                new() { Name = "Hip Thrust",                     MuscleGroup = "Legs" },
                new() { Name = "Leg Curls",                      MuscleGroup = "Legs" },
                new() { Name = "Leg Extensions",                 MuscleGroup = "Legs" },
                new() { Name = "Leg Press",                      MuscleGroup = "Legs" },
                new() { Name = "Lunges",                         MuscleGroup = "Legs" },
                new() { Name = "Pistol Squat",                   MuscleGroup = "Legs" },
                new() { Name = "Romanian Deadlift",              MuscleGroup = "Legs" },
                new() { Name = "Sumo Deadlift",                  MuscleGroup = "Legs" },
                new() { Name = "Sumo Squat",                     MuscleGroup = "Legs" },
                new() { Name = "V Squats",                       MuscleGroup = "Legs" },

                // Shoulders
                new() { Name = "Arnold Press",                   MuscleGroup = "Shoulders" },
                new() { Name = "Face Pulls",                     MuscleGroup = "Shoulders" },
                new() { Name = "Front Raises",                   MuscleGroup = "Shoulders" },
                new() { Name = "Lateral Raises",                 MuscleGroup = "Shoulders" },
                new() { Name = "Shoulder Press",                 MuscleGroup = "Shoulders" },
            };

            await _database.InsertAllAsync(defaultExercises);
        }
    }
}