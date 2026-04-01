using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class RestTimerViewModel : ObservableObject, IDisposable
    {
        private readonly IRestTimerService _timer;

        [ObservableProperty] private int    _defaultCompound =      120;
        [ObservableProperty] private int    _defaultIsolation =     75;
        [ObservableProperty] private int    _customSeconds =        90;
        [ObservableProperty] private float  _progress =             0f;
        [ObservableProperty] private string _remainingLabel =       "0:00";
        [ObservableProperty] private bool   _isRunning =            false;
        [ObservableProperty] private bool   _isFinished =           false;
        [ObservableProperty] private bool   _isVisible =            false;

        public IReadOnlyList<int> Presets { get; } = [60, 90, 120, 180];

        public RestTimerViewModel(IRestTimerService timer)
        {
            _timer = timer;
            _timer.StateChanged += OnStateChanged;
            SyncFromService();
        }

        public void Dispose()
        {
            _timer.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged()
        {
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

        public void Subscribe()
        {
            _timer.StateChanged -= OnStateChanged;
            _timer.StateChanged += OnStateChanged;
            SyncFromService();
        }

        public void Unsubscribe()
        {
            _timer.StateChanged -= OnStateChanged;
        }

        [RelayCommand]
        private void StartPreset(string seconds)
        {
            if (int.TryParse(seconds, out int s))
            {
                _timer.Start(s);
            }              
        }

        [RelayCommand]
        private void StartCustom()
        {
            if (CustomSeconds > 0)
            {
                _timer.Start(CustomSeconds);
            }          
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
    }
}
