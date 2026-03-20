using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    /// <summary>
    /// Thin ViewModel that exposes IRestTimerService state to the XAML bottom bar.
    /// The service is a singleton — this VM is a Transient that subscribes to it
    /// on creation and unsubscribes on disposal (called when AddWorkoutView is
    /// destroyed). Timer state survives navigation because it lives in the service.
    /// </summary>
    public partial class RestTimerViewModel : ObservableObject, IDisposable
    {
        private readonly IRestTimerService _timer;

        public RestTimerViewModel(IRestTimerService timer)
        {
            _timer = timer;
            _timer.StateChanged += OnStateChanged;
            SyncFromService();
        }

        // ── Observable properties ─────────────────────────────────────────────
        [ObservableProperty] private string _remainingLabel = "0:00";
        [ObservableProperty] private float _progress = 0f;
        [ObservableProperty] private bool _isRunning = false;
        [ObservableProperty] private bool _isFinished = false;
        [ObservableProperty] private bool _isVisible = false;  // bottom bar visibility
        [ObservableProperty] private int _customSeconds = 90;     // entry field value
        [ObservableProperty] private int _defaultCompound = 120;
        [ObservableProperty] private int _defaultIsolation = 75;

        // ── Preset quick-add durations shown as chips ─────────────────────────
        public IReadOnlyList<int> Presets { get; } = [60, 90, 120, 180];

        // ── Commands ──────────────────────────────────────────────────────────

        [RelayCommand]
        private void StartPreset(string seconds)
        {
            if (int.TryParse(seconds, out int s))
                _timer.Start(s);
        }

        [RelayCommand]
        private void StartCustom()
        {
            if (CustomSeconds > 0)
                _timer.Start(CustomSeconds);
        }

        [RelayCommand]
        private void AddThirty() => _timer.AddTime(30);

        [RelayCommand]
        private void Stop() => _timer.Stop();

        [RelayCommand]
        private void SaveDefaults()
        {
            _timer.DefaultCompoundSeconds = DefaultCompound;
            _timer.DefaultIsolationSeconds = DefaultIsolation;
        }

        // ── Service → ViewModel sync ──────────────────────────────────────────

        private void OnStateChanged()
        {
            // Already on main thread (RestTimerService marshals all events).
            SyncFromService();
        }

        private void SyncFromService()
        {
            IsRunning = _timer.IsRunning;
            IsFinished = _timer.IsFinished;
            IsVisible = _timer.IsRunning || _timer.IsFinished;
            Progress = _timer.Progress;

            var r = _timer.Remaining;
            RemainingLabel = _timer.IsFinished
                ? "Done! 💪"
                : $"{(int)r.TotalMinutes}:{r.Seconds:D2}";

            DefaultCompound = _timer.DefaultCompoundSeconds;
            DefaultIsolation = _timer.DefaultIsolationSeconds;
        }

        // ── IDisposable ───────────────────────────────────────────────────────
        public void Dispose()
        {
            _timer.StateChanged -= OnStateChanged;
        }
    }
}
