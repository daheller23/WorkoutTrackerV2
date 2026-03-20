// AndroidNotificationSetup.cs
// Add this content to your existing MainApplication.cs (Platforms/Android),
// or create it as a new partial class if MainApplication already exists.
//
// This registers the "rest_timer" notification channel that RestTimerService
// references. Android 8.0+ requires channels before notifications can be shown.

using Android.App;
using Android.Runtime;
using Plugin.LocalNotification;

namespace WorkoutTrackerV2
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership) { }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();
            CreateNotificationChannel();
        }

        private void CreateNotificationChannel()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                var channel = new NotificationChannel(
                    "rest_timer",
                    "Rest Timer",
                    NotificationImportance.High)
                {
                    Description = "Notifies you when your rest period is complete"
                };
                channel.EnableVibration(true);

                var manager = (NotificationManager?)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(channel);
            }
        }
    }
}
