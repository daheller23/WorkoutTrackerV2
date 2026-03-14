
using SQLite;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public class WorkoutService : IWorkoutService
    {
        #region "PRIVATE VARIABLES"
        private readonly string _dbPath;
        private readonly SemaphoreSlim _initLock = new(1, 1); // To avoid race condition if InitializeAsync is called from multiple threads simultaneuously
        private SQLiteAsyncConnection _database = null!;
        private const string DbFileName = "workout_tracker.db3";
        private bool _initialized = false;    
        #endregion

        public WorkoutService()
        {
            _dbPath = Path.Combine(FileSystem.AppDataDirectory, DbFileName);
        }

        #region "INITIALIZE ASYNC"
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

                int exerciseCount = await _database.Table<Exercise>().CountAsync();
                if (exerciseCount == 0)
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
        #endregion

        #region "SEED DEFAULT EXERCISES"
        private async Task SeedDefaultExercises()
        {
            var defaultExercises = new List<Exercise>
            {
                new Exercise { Name = "Bench Press", MuscleGroup = "Chest" },
                new Exercise { Name = "Incline Bench Press", MuscleGroup = "Chest" },
                new Exercise { Name = "Dumbbell Flyes", MuscleGroup = "Chest" },
                new Exercise { Name = "Cable Crossover", MuscleGroup = "Chest" },
                new Exercise { Name = "Deadlifts", MuscleGroup = "Back" },
                new Exercise { Name = "Barbell Rows", MuscleGroup = "Back" },
                new Exercise { Name = "Pull-ups", MuscleGroup = "Back" },
                new Exercise { Name = "Lat Pulldowns", MuscleGroup = "Back" },
                new Exercise { Name = "Squats", MuscleGroup = "Legs" },
                new Exercise { Name = "Leg Press", MuscleGroup = "Legs" },
                new Exercise { Name = "Leg Curls", MuscleGroup = "Legs" },
                new Exercise { Name = "Leg Extensions", MuscleGroup = "Legs" },
                new Exercise { Name = "Calf Raises", MuscleGroup = "Legs" },
                new Exercise { Name = "Shoulder Press", MuscleGroup = "Shoulders" },
                new Exercise { Name = "Lateral Raises", MuscleGroup = "Shoulders" },
                new Exercise { Name = "Face Pulls", MuscleGroup = "Shoulders" },
                new Exercise { Name = "Barbell Curls", MuscleGroup = "Arms" },
                new Exercise { Name = "Tricep Dips", MuscleGroup = "Arms" },
                new Exercise { Name = "Dumbbell Curls", MuscleGroup = "Arms" },
                new Exercise { Name = "Tricep Rope Pushdowns", MuscleGroup = "Arms" },
                new Exercise { Name = "Planks", MuscleGroup = "Core" },
                new Exercise { Name = "Ab Wheel", MuscleGroup = "Core" },
                new Exercise { Name = "Weighted Sit-ups", MuscleGroup = "Core" }
            };

            foreach (var exercise in defaultExercises)
            {
                await _database.InsertAsync(exercise);
            }
        }
        #endregion

        #region "GET ALL EXERCISES ASYNC
        public async Task<List<Exercise>> GetAllExercisesAsync()
        {
            await InitializeAsync();
            return await _database.Table<Exercise>().ToListAsync();
        }
        #endregion

        #region "GET EXERCISE ASYNC"
        public async Task<Exercise> GetExerciseAsync(int id)
        {
            await InitializeAsync();
            return await _database.GetAsync<Exercise>(id);
        }
        #endregion

        #region "SAVE SESSION ASYNC"
        public async Task<int> SaveSessionAsync(WorkoutSession session)
        {
            await InitializeAsync();
            if (session.Id == 0)
            {
                await _database.InsertAsync(session);
                return session.Id;
            }
            await _database.UpdateAsync(session);
            return session.Id;
        }
        #endregion

        #region "SAVE SET ASYNC"
        public async Task<int> SaveSetAsync(WorkoutSet set)
        {
            await InitializeAsync();
            if (set.Id == 0)
            {
                return await _database.InsertAsync(set);
            }
            return await _database.UpdateAsync(set);
        }
        #endregion

        #region "GET TOTAL WORKOUT COUNT ASYNC"
        public async Task<int> GetTotalWorkoutCountAsync()
        {
            await InitializeAsync();
            return await _database.Table<WorkoutSession>().CountAsync();
        }
        #endregion

        #region "GET LAST WORKOUT DATE ASYNC"
        public async Task<DateTime?> GetLastWorkoutDateAsync()
        {
            await InitializeAsync();
            var lastSession = await _database.Table<WorkoutSession>()
                .OrderByDescending(x => x.Date)
                .FirstOrDefaultAsync();
            return lastSession?.Date;
        }
        #endregion

        #region "GET ALL SESSION ASYNC"
        public async Task<List<WorkoutSession>> GetAllSessionsAsync()
        {
            await InitializeAsync();
            return await _database.Table<WorkoutSession>().OrderByDescending(x => x.Date).ToListAsync();
        }
        #endregion

        #region "GET SESSIONS ASYNC"
        public async Task<List<WorkoutSession>> GetSessionsAsync(DateTime startDate, DateTime endDate)
        {
            await InitializeAsync();
            return await _database.Table<WorkoutSession>()
                .Where(x => x.Date >= startDate && x.Date <= endDate)
                .OrderByDescending(x => x.Date)
                .ToListAsync();
        }
        #endregion

        #region "GET SETS FOR SESSION ASYNC"
        public async Task<List<WorkoutSet>> GetSetsForSessionAsync(int sessionId)
        {
            await InitializeAsync();
            return await _database.Table<WorkoutSet>()
                .Where(x => x.WorkoutSessionId == sessionId)
                .OrderBy(x => x.SetNumber)
                .ToListAsync();
        }
        #endregion

        #region "GET EXERCISE HISTORY ASYNC"
        public async Task<List<WorkoutSet>> GetExerciseHistoryAsync(int exerciseId, int days = 30)
        {
            await InitializeAsync();
            var startDate = DateTime.Now.AddDays(-days);
            return await _database.Table<WorkoutSet>()
                .Where(x => x.ExerciseId == exerciseId && x.CreatedDate >= startDate)
                .OrderBy(x => x.CreatedDate)
                .ToListAsync();
        }
        #endregion

        #region "DELETE SET ASYNC"
        public async Task<int> DeleteSetAsync(WorkoutSet set)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(set);
        }
        #endregion

        #region "DELETE SET ASYNC"
        public async Task DeleteSetAsync(int id)
        {
            await InitializeAsync();
            await _database.DeleteAsync<WorkoutSet>(id);
        }
        #endregion

        #region "DELETE SESSION ASYNC"
        public async Task<int> DeleteSessionAsync(WorkoutSession session)
        {
            await InitializeAsync();
            return await _database.DeleteAsync(session);
        }
        #endregion

    }
}
