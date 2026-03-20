using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace WorkoutTrackerV2.Services
{
    /// <summary>
    /// Singleton rest timer service. Owns a System.Threading.Timer that ticks
    /// every second on a thread-pool thread. StateChanged is always marshalled
    /// back to the main thread so ViewModel observers can update UI directly.
    ///
    /// Background notification: when Start() is called, a local notification is
    /// scheduled via Plugin.LocalNotification to fire at (now + duration). If the
    /// user stops the timer early the pending notification is cancelled. This
    /// means the notification fires even when the app is backgrounded or the
    /// screen is locked — the OS delivers it independently of our in-process timer.
    /// </summary>
    public class RestTimerService : IRestTimerService, IDisposable
    {
        // ── Notification id — fixed so we always cancel/replace the same one ──
        private const int NotificationId = 9001;

        // ── Muscle groups that warrant a longer compound rest ─────────────────
        private static readonly HashSet<string> CompoundGroups =
            new(StringComparer.OrdinalIgnoreCase)
            { "Legs", "Back", "Chest" };

        // ── Timer internals ───────────────────────────────────────────────────
        private System.Threading.Timer? _timer;
        private readonly object _lock = new();
        private DateTime _endTime;
        private bool _isFinished;

        // ── IRestTimerService ─────────────────────────────────────────────────
        public bool IsRunning { get; private set; }
        public bool IsFinished => _isFinished;
        public TimeSpan Remaining { get; private set; }
        public TimeSpan Duration { get; private set; }
        public float Progress =>
            Duration.TotalSeconds > 0
                ? 1f - (float)(Remaining.TotalSeconds / Duration.TotalSeconds)
                : 0f;

        public int DefaultCompoundSeconds { get; set; } = 120;  // 2 minutes
        public int DefaultIsolationSeconds { get; set; } = 75;   // 75 seconds

        public event Action? StateChanged;
        public event Action? TimerFinished;

        // ── Public operations ─────────────────────────────────────────────────

        public void Start(int seconds)
        {
            if (seconds <= 0) return;

            lock (_lock)
            {
                StopInternal();

                Duration = TimeSpan.FromSeconds(seconds);
                Remaining = Duration;
                _endTime = DateTime.UtcNow + Duration;
                IsRunning = true;
                _isFinished = false;

                _timer = new System.Threading.Timer(
                    OnTick, null,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(1));
            }

            _ = ScheduleNotificationAsync(seconds);
            NotifyStateChanged();
        }

        public void StartDefault(string muscleGroup)
        {
            int seconds = CompoundGroups.Contains(muscleGroup)
                ? DefaultCompoundSeconds
                : DefaultIsolationSeconds;
            Start(seconds);
        }

        public void AddTime(int seconds)
        {
            if (!IsRunning || seconds <= 0) return;

            lock (_lock)
            {
                _endTime = _endTime.AddSeconds(seconds);
                Duration = Duration.Add(TimeSpan.FromSeconds(seconds));
                Remaining = Remaining.Add(TimeSpan.FromSeconds(seconds));
            }

            // Reschedule notification with the new end time.
            CancelNotification();
            var newRemaining = (int)Math.Ceiling(Remaining.TotalSeconds);
            if (newRemaining > 0)
                _ = ScheduleNotificationAsync(newRemaining);

            NotifyStateChanged();
        }

        public void Stop()
        {
            lock (_lock) { StopInternal(); }
            CancelNotification();
            NotifyStateChanged();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void StopInternal()
        {
            // Must be called inside _lock.
            _timer?.Dispose();
            _timer = null;
            IsRunning = false;
            _isFinished = false;
            Remaining = TimeSpan.Zero;
        }

        private void OnTick(object? _)
        {
            DateTime now;
            lock (_lock)
            {
                if (!IsRunning) return;
                now = DateTime.UtcNow;
                Remaining = _endTime - now;
            }

            if (Remaining <= TimeSpan.Zero)
            {
                lock (_lock)
                {
                    _timer?.Dispose();
                    _timer = null;
                    IsRunning = false;
                    Remaining = TimeSpan.Zero;
                    _isFinished = true;
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StateChanged?.Invoke();
                    TimerFinished?.Invoke();

                    // Clear the finished state after 3 seconds so the bar
                    // returns to idle without requiring user interaction.
                    Task.Delay(3000).ContinueWith(_ =>
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _isFinished = false;
                            StateChanged?.Invoke();
                        }));
                });
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => StateChanged?.Invoke());
        }

        private void NotifyStateChanged() =>
            MainThread.BeginInvokeOnMainThread(() => StateChanged?.Invoke());

        // ── Local notification helpers ────────────────────────────────────────

        private static async Task ScheduleNotificationAsync(int secondsFromNow)
        {
            try
            {
                var notification = new NotificationRequest
                {
                    NotificationId = NotificationId,
                    Title = "Rest complete 💪",
                    Description = "Time to get back to it!",
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = DateTime.Now.AddSeconds(secondsFromNow)
                    },
#if ANDROID
                    // AndroidOptions is Android-only — #if ANDROID prevents a
                    // compile error on iOS. AndroidPriority.High produces a
                    // heads-up notification visible even when the phone is in use.
                    Android = new AndroidOptions
                    {
                        Priority = AndroidPriority.High,
                        ChannelId = "rest_timer"
                    }
#endif
                };

                await LocalNotificationCenter.Current.Show(notification);
            }
            catch
            {
                // Notification permission not granted — timer still works
                // in-app, notification just won't fire when backgrounded.
            }
        }

        private static void CancelNotification()
        {
            try { LocalNotificationCenter.Current.Cancel(NotificationId); }
            catch { /* ignore */ }
        }

        // ── IDisposable ───────────────────────────────────────────────────────
        public void Dispose()
        {
            lock (_lock) { _timer?.Dispose(); }
        }
    }
}
