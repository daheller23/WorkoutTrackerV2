using WorkoutTrackerV2.Views;

namespace WorkoutTrackerV2
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(Routes.Home, typeof(HomeView));
            Routing.RegisterRoute(Routes.Workout, typeof(AddWorkoutView));
            Routing.RegisterRoute(Routes.History, typeof(WorkoutHistoryView));
            Routing.RegisterRoute(Routes.Analytics, typeof(AnalyticsView));
            Routing.RegisterRoute(Routes.WorkoutDetail, typeof(WorkoutDetailView));
            Routing.RegisterRoute(Routes.EditWorkout, typeof(EditWorkoutView));
            Routing.RegisterRoute(Routes.Settings, typeof(SettingsView));
            Routing.RegisterRoute(Routes.ExercisePicker, typeof(ExercisePickerView));
            Routing.RegisterRoute(Routes.MuscleGroupProgress, typeof(MuscleGroupProgressView));
            Routing.RegisterRoute(Routes.ExerciseProgress, typeof(ExerciseProgressView));
        }
    }
}
