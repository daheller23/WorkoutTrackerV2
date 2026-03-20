namespace WorkoutTrackerV2.Services
{
    public interface IRestTimerService
    {
        // ── State ────────────────────────────────────────────────────────────
        bool IsRunning { get; }
        bool IsFinished { get; }  // true for ~3s after timer hits 0
        TimeSpan Remaining { get; }
        TimeSpan Duration { get; }
        float Progress { get; }  // 0.0 → 1.0 (elapsed / duration)

        // ── Default durations (seconds) ──────────────────────────────────────
        int DefaultCompoundSeconds { get; set; }  // e.g. squats, deadlifts
        int DefaultIsolationSeconds { get; set; }  // e.g. curls, lateral raises

        // ── Events ───────────────────────────────────────────────────────────
        event Action? StateChanged;   // fires every tick and on start/stop/finish
        event Action? TimerFinished;  // fires once when countdown hits zero

        // ── Operations ───────────────────────────────────────────────────────
        void Start(int seconds);
        void StartDefault(string muscleGroup);  // picks compound vs isolation
        void AddTime(int seconds);
        void Stop();
    }
}
