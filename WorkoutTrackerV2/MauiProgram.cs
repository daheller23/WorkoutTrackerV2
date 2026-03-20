using Microcharts.Maui;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using SkiaSharp.Views.Maui.Controls.Hosting;
using WorkoutTrackerV2.Services;
using WorkoutTrackerV2.ViewModels;
using WorkoutTrackerV2.Views;

namespace WorkoutTrackerV2
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .UseMicrocharts()
                .UseLocalNotification(config =>
                {
                    config.AddAndroid(android =>
                    {
                        android.AddChannel(new Plugin.LocalNotification.AndroidOption.NotificationChannelRequest
                        {
                            Id = "rest_timer",
                            Name = "Rest Timer",
                            Description = "Notifies you when your rest period is complete",
                            Importance = Plugin.LocalNotification.AndroidOption.AndroidImportance.High
                        });
                    });
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var s = builder.Services;

            // ── Services (Singleton — shared state, DB connection, caches) ────
            s.AddSingleton<IRestTimerService, RestTimerService>();
            s.AddSingleton<IWorkoutService, WorkoutService>();
            s.AddSingleton<IAnalyticsService, AnalyticsService>();
            s.AddSingleton<ITemplateService, TemplateService>();
            s.AddSingleton<ISettingsService, SettingsService>();

            // ── Shell ─────────────────────────────────────────────────────────
            s.AddSingleton<AppShell>();

            // ── Tab pages (Singleton — always resident, cheap to keep alive) ──
            // Home and its VM are singletons because the Home tab is always
            // resident and its data refreshes on every OnAppearing anyway.
            s.AddSingleton<HomeViewModel>();
            s.AddSingleton<HomeView>();

            // FIX 1: AddWorkout changed from Singleton to Transient.
            // The VM holds mutable workout state (ExerciseGroups, WorkoutName,
            // Notes, times). As a Singleton this state persisted across sessions —
            // returning to the tab after saving showed the previous workout's data.
            s.AddTransient<AddWorkoutViewModel>();
            s.AddTransient<AddWorkoutView>();

            // FIX 2: WorkoutHistory changed from Singleton to Transient.
            // The VM holds _allSessions which grows unboundedly as a Singleton.
            // Transient ensures a clean load on each navigation and avoids holding
            // the full session list in memory when History isn't visible.
            s.AddTransient<WorkoutHistoryViewModel>();
            s.AddTransient<WorkoutHistoryView>();

            // FIX 3: Analytics changed from Singleton to Transient.
            // Computed analytics data (sparklines, muscle group progress, daily
            // stats) held forever as a Singleton is unnecessary — the page reloads
            // on every OnAppearing regardless. Transient keeps memory usage flat.
            s.AddTransient<AnalyticsViewModel>();
            s.AddTransient<AnalyticsView>();

            // ── Detail / modal pages (Transient — fresh state each navigation) ─
            s.AddTransient<WorkoutDetailViewModel>();
            s.AddTransient<WorkoutDetailView>();

            s.AddTransient<EditWorkoutViewModel>();
            s.AddTransient<EditWorkoutView>();

            s.AddTransient<SettingsViewModel>();
            s.AddTransient<SettingsView>();

            s.AddTransient<ExercisePickerViewModel>();
            s.AddTransient<ExercisePickerView>();

            s.AddTransient<MuscleGroupProgressViewModel>();
            s.AddTransient<MuscleGroupProgressView>();

            s.AddTransient<ExerciseProgressViewModel>();
            s.AddTransient<ExerciseProgressView>();

            s.AddTransient<TemplatePickerViewModel>();
            s.AddTransient<TemplatePickerView>();

            s.AddTransient<PersonalRecordsViewModel>();
            s.AddTransient<PersonalRecordsView>();

            s.AddTransient<CreateExerciseViewModel>();
            s.AddTransient<CreateExerciseView>();

            return builder.Build();
        }
    }
}
