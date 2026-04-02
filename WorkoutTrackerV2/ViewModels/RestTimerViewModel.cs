using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class RestTimerViewModel : ObservableObject, IDisposable
    {
        private readonly IRestTimerService _timer;

        public IReadOnlyList<int> Presets { get; } = [60, 90, 120, 180];

        [ObservableProperty] private int    _defaultCompound =      120;
        [ObservableProperty] private int    _defaultIsolation =     75;
        [ObservableProperty] private int    _customSeconds =        90;

        [ObservableProperty] private float  _progress =             0f;

        [ObservableProperty] private string _remainingLabel =       "0:00";

        [ObservableProperty] private bool   _isRunning =            false;
        [ObservableProperty] private bool   _isFinished =           false;
        [ObservableProperty] private bool   _isVisible =            false;

        public RestTimerViewModel(IRestTimerService timer)
        {
            _timer = timer;
            _timer.StateChanged += OnStateChanged;
            SyncFromService();
        }

        // ==============================================================================================================
        //
        //      PUBLIC METHODS
        //
        // ==============================================================================================================

        public void Dispose()
        {
            _timer.StateChanged -= OnStateChanged;
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

        // ==============================================================================================================
        //
        //      PRIVATE RELAY COMMANDS
        //
        // ==============================================================================================================

        [RelayCommand]
        private void StartPreset(int seconds)
        {
            if (seconds > 0)
            {
                _timer.Start(seconds);
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

        // ==============================================================================================================
        //
        //      PRIVATE METHODS
        //
        // ==============================================================================================================

        private void OnStateChanged()
        {
            SyncFromService();
        }

        private void SyncFromService()
        {
            IsRunning = _timer.IsRunning;
            IsFinished = _timer.IsFinished;
            IsVisible = IsRunning || IsFinished;
            Progress = _timer.Progress;

            RemainingLabel = IsFinished
                ? "Done! 💪"
                : _timer.Remaining.ToString(@"m\:ss");

            if (!IsRunning)
            {
                DefaultCompound = _timer.DefaultCompoundSeconds;
                DefaultIsolation = _timer.DefaultIsolationSeconds;
            }
        }
    }
}
