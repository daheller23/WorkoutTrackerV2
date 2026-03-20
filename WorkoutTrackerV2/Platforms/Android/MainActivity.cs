using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace WorkoutTrackerV2
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges =
            ConfigChanges.ScreenSize | ConfigChanges.Orientation |
            ConfigChanges.UiMode | ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Force white status bar with dark (visible) icons.
            // FIX 1: Android.Graphics.Color.White replaces Color.ParseColor("#FFFFFF")
            // — same result with no string parsing at runtime.
            Window!.SetStatusBarColor(Android.Graphics.Color.White);
            WindowCompat.GetInsetsController(Window, Window.DecorView)
                        .AppearanceLightStatusBars = true;
        }
    }
}
