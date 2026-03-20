using SQLite;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public class BodyWeightService : IBodyWeightService
    {
        // Shares the same SQLite database file as WorkoutService so everything
        // stays in one file and there is no cross-database complexity.
        private SQLiteAsyncConnection _database = null!;
        private const string DbFileName = "workout_tracker.db3";
        private bool _initialized;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private readonly ISettingsService _settings;

        public BodyWeightService(ISettingsService settings)
        {
            _settings = settings;
        }

        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;
            await _initLock.WaitAsync();
            try
            {
                if (_initialized) return;
                var path = Path.Combine(FileSystem.AppDataDirectory, DbFileName);
                _database = new SQLiteAsyncConnection(path);
                await _database.CreateTableAsync<BodyWeightEntry>();
                _initialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<List<BodyWeightEntry>> GetEntriesAsync(int days = 0)
        {
            await EnsureInitializedAsync();
            if (days == 0)
                return await _database.Table<BodyWeightEntry>()
                    .OrderByDescending(e => e.Date)
                    .ToListAsync();

            var cutoff = DateTime.Now.AddDays(-days);
            return await _database.Table<BodyWeightEntry>()
                .Where(e => e.Date >= cutoff)
                .OrderByDescending(e => e.Date)
                .ToListAsync();
        }

        public async Task SaveEntryAsync(BodyWeightEntry entry)
        {
            await EnsureInitializedAsync();
            if (entry.Id == 0)
                await _database.InsertAsync(entry);
            else
                await _database.UpdateAsync(entry);
        }

        public async Task DeleteEntryAsync(BodyWeightEntry entry)
        {
            await EnsureInitializedAsync();
            await _database.DeleteAsync(entry);
        }

        public async Task<BodyWeightStats> GetStatsAsync(string unit)
        {
            await EnsureInitializedAsync();

            // Single DB fetch — all stats computed in memory from one query.
            var all = await _database.Table<BodyWeightEntry>()
                .OrderBy(e => e.Date)
                .ToListAsync();

            if (all.Count == 0) return new BodyWeightStats();

            // Convert to requested unit if needed.
            var weights = all.Select(e =>
                (e.Unit == unit) ? e.Weight : ConvertWeight(e.Weight, e.Unit, unit))
                .ToList();

            var weekCutoff = DateTime.Now.AddDays(-7);
            var weekWeights = all
                .Where(e => e.Date >= weekCutoff)
                .Select(e => (e.Unit == unit) ? e.Weight : ConvertWeight(e.Weight, e.Unit, unit))
                .ToList();

            double current = weights[^1];
            double starting = weights[0];
            double change = current - starting;

            // BMI: requires height stored in settings.
            // HeightCm is 0 until the user sets it — we return null to hide the card.
            double? bmi = null;
            double heightCm = _settings.HeightCm;
            if (heightCm > 0)
            {
                double weightKg = unit == "kg"
                    ? current
                    : current * 0.453592;
                double heightM = heightCm / 100.0;
                bmi = Math.Round(weightKg / (heightM * heightM), 1);
            }

            return new BodyWeightStats
            {
                CurrentWeight = Math.Round(current, 1),
                StartingWeight = Math.Round(starting, 1),
                Change = Math.Round(change, 1),
                WeeklyAverage = weekWeights.Count > 0
                    ? Math.Round(weekWeights.Average(), 1)
                    : null,
                PersonalLowest = Math.Round(weights.Min(), 1),
                PersonalHighest = Math.Round(weights.Max(), 1),
                Bmi = bmi
            };
        }

        private static double ConvertWeight(double weight, string from, string to)
        {
            if (from == to) return weight;
            return (from == "lbs" && to == "kg")
                ? weight * 0.453592
                : weight / 0.453592;
        }
    }
}
