using Plugin.LocalNotification;
using WorkoutTrackerV2.Services;

#if ANDROID || IOS
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.Core;
#endif

namespace WorkoutTrackerV2
{
    public partial class App : Application
    {
        public App(ISettingsService settingsService)
        {
            InitializeComponent();
            bool isDark = settingsService.IsDarkMode;
            Application.Current!.UserAppTheme = isDark ? AppTheme.Dark : AppTheme.Light;
            Application.Current.RequestedThemeChanged += (s, a) =>
            {
                UpdateStatusBar(a.RequestedTheme == AppTheme.Dark);
            };
        }

        private void UpdateStatusBar(bool isDark)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
#if ANDROID || IOS
                var bgColor = isDark ? Color.FromArgb("#1F1F1F") : Colors.White;
                var iconStyle = isDark ? StatusBarStyle.LightContent : StatusBarStyle.DarkContent;
                StatusBar.SetColor(bgColor);
                StatusBar.SetStyle(iconStyle);
#endif
            });
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            window.Created += (s, e) =>
            {
                UpdateStatusBar(Application.Current!.UserAppTheme == AppTheme.Dark);
            };
            return window;
        }

        protected override async void OnStart()
        {
            base.OnStart();
            await LocalNotificationCenter.Current.RequestNotificationPermission();
        }
    }
}