using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Services
{
    public interface IBodyWeightService
    {
        Task<List<BodyWeightEntry>> GetEntriesAsync(int days = 0);
        Task SaveEntryAsync(BodyWeightEntry entry);
        Task DeleteEntryAsync(BodyWeightEntry entry);

        // Pre-computed stats — all returned together to avoid multiple DB round-trips.
        Task<BodyWeightStats> GetStatsAsync(string unit);
    }

    public class BodyWeightStats
    {
        public double? CurrentWeight { get; init; }   // most recent entry
        public double? StartingWeight { get; init; }   // oldest entry
        public double? Change { get; init; }   // current - starting
        public double? WeeklyAverage { get; init; }   // avg of last 7 days
        public double? PersonalLowest { get; init; }
        public double? PersonalHighest { get; init; }
        public double? Bmi { get; init; }   // null until height set
        public bool HasData => CurrentWeight.HasValue;
    }
}
