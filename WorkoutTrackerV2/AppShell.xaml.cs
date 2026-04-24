using WorkoutTrackerV2.Views;

namespace WorkoutTrackerV2
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            Routing.RegisterRoute(Routes.WorkoutDetail, typeof(WorkoutDetailView));
            Routing.RegisterRoute(Routes.EditWorkout, typeof(EditWorkoutView));
            Routing.RegisterRoute(Routes.Settings, typeof(SettingsView));
            Routing.RegisterRoute(Routes.ExercisePicker, typeof(ExercisePickerView));
            Routing.RegisterRoute(Routes.MuscleGroupProgress, typeof(MuscleGroupProgressView));
            Routing.RegisterRoute(Routes.ExerciseProgress, typeof(ExerciseProgressView));
            Routing.RegisterRoute(Routes.TemplatePicker, typeof(TemplatePickerView));
            Routing.RegisterRoute(Routes.PersonalRecords, typeof(PersonalRecordsView));
            Routing.RegisterRoute(Routes.CreateExercise, typeof(CreateExerciseView));
            Routing.RegisterRoute(Routes.PlateCalculator, typeof(PlateCalculatorView));
            Routing.RegisterRoute(Routes.OneRmCalculator, typeof(OneRepMaxCalculatorView));
            Routing.RegisterRoute(Routes.WeightConverterCalculator, typeof(WeightConverterView));
        }
    }
}
