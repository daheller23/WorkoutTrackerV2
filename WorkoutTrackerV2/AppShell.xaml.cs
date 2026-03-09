using WorkoutTrackerV2.Views;

namespace WorkoutTrackerV2
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(nameof(HomeView), typeof(HomeView));
            Routing.RegisterRoute(nameof(AddWorkoutView), typeof(AddWorkoutView));
            Routing.RegisterRoute(nameof(WorkoutHistoryView), typeof(WorkoutHistoryView));
            Routing.RegisterRoute(nameof(AnalyticsView), typeof(AnalyticsView));
        }
    }
}
