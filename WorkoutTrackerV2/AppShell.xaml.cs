using WorkoutTrackerV2.Views;

namespace WorkoutTrackerV2
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // FIX 1: Routes.Home, Workout, History, and Analytics are NOT registered
            // here because they are declared as ShellContent Route= in AppShell.xaml.
            // Shell registers those routes automatically — explicit registration would
            // cause a duplicate route conflict. Only modal/detail routes that are NOT
            // ShellContent tabs need to be registered here.
            Routing.RegisterRoute(Routes.WorkoutDetail, typeof(WorkoutDetailView));
            Routing.RegisterRoute(Routes.EditWorkout, typeof(EditWorkoutView));
            Routing.RegisterRoute(Routes.Settings, typeof(SettingsView));
            Routing.RegisterRoute(Routes.ExercisePicker, typeof(ExercisePickerView));
            Routing.RegisterRoute(Routes.MuscleGroupProgress, typeof(MuscleGroupProgressView));
            Routing.RegisterRoute(Routes.ExerciseProgress, typeof(ExerciseProgressView));
            Routing.RegisterRoute(Routes.TemplatePicker, typeof(TemplatePickerView));
            Routing.RegisterRoute(Routes.PersonalRecords, typeof(PersonalRecordsView));
            Routing.RegisterRoute(Routes.CreateExercise, typeof(CreateExerciseView));
            Routing.RegisterRoute(Routes.BodyWeight, typeof(BodyWeightView));
            Routing.RegisterRoute(Routes.PlateCalculator, typeof(PlateCalculatorView));
        }
    }
}
