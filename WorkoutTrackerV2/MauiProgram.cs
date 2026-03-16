using Microcharts.Maui;
using Microsoft.Extensions.Logging;
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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton<HomeViewModel>();
            builder.Services.AddSingleton<HomeView>();
            builder.Services.AddSingleton<AddWorkoutViewModel>();
            builder.Services.AddSingleton<AddWorkoutView>();
            builder.Services.AddSingleton<WorkoutHistoryViewModel>();
            builder.Services.AddSingleton<WorkoutHistoryView>();
            builder.Services.AddSingleton<AnalyticsViewModel>();
            builder.Services.AddSingleton<AnalyticsView>();
            builder.Services.AddSingleton<IWorkoutService, WorkoutService>();
            builder.Services.AddSingleton<IAnalyticsService, AnalyticsService>();
            builder.Services.AddTransient<WorkoutDetailView>();
            builder.Services.AddTransient<WorkoutDetailViewModel>();
            builder.Services.AddTransient<EditWorkoutView>();
            builder.Services.AddTransient<EditWorkoutViewModel>();
            builder.Services.AddTransient<SettingsView>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<ExercisePickerView>();
            builder.Services.AddTransient<ExercisePickerViewModel>();
            builder.Services.AddTransient<MuscleGroupProgressView>();
            builder.Services.AddTransient<MuscleGroupProgressViewModel>();
            builder.Services.AddTransient<ExerciseProgressView>();
            builder.Services.AddTransient<ExerciseProgressViewModel>();
            builder.Services.AddTransient<TemplatePickerView>();
            builder.Services.AddTransient<TemplatePickerViewModel>();
            builder.Services.AddSingleton<ITemplateService, TemplateService>();
            builder.Services.AddSingleton<ISettingsService, SettingsService>();
            builder.Services.AddTransient<PersonalRecordsView>();
            builder.Services.AddTransient<PersonalRecordsViewModel>();
            builder.Services.AddSingleton<AppShell>();

            return builder.Build();
        }
    }
}
