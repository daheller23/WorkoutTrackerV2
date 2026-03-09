using Microsoft.Extensions.Logging;
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

            return builder.Build();
        }
    }
}
