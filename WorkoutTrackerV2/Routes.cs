
namespace WorkoutTrackerV2
{
    public static class Routes
    {
        // Tab routes stay absolute
        public const string Home = "//Home";
        public const string Workout = "//Workout";
        public const string Analytics = "//Analytics";
        public const string History = "//History";

        // Modal/detail routes should be relative
        public const string WorkoutDetail = "details";
        public const string EditWorkout = "edit";
        public const string Settings = "settings";
        public const string ExercisePicker = "exercisepicker";
        public const string MuscleGroupProgress = "musclegroupProgress";
        public const string ExerciseProgress = "exerciseprogress";
        public const string TemplatePicker = "templatepicker";
        public const string PersonalRecords = "personalrecords";
        public const string CreateExercise = "createexercise";
        public const string BodyWeight = "bodyweight";
        public const string PlateCalculator = "platecalculator";
        public const string OneRmCalculator = "onerepmaxcalculator";
        public const string WeightConverterCalculator = "weightconvertercalculator";
        public const string Back = "..";
    }
}
