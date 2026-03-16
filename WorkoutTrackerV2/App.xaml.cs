using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2
{
    public partial class App : Application
    {
        public App(ISettingsService settingsService)
        {
            InitializeComponent();
            Application.Current!.UserAppTheme = settingsService.IsDarkMode
                ? AppTheme.Dark
                : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }

    }
}