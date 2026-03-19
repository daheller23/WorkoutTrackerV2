using WorkoutTrackerV2.ViewModels;
namespace WorkoutTrackerV2.Views
{
    public partial class ExercisePickerView : ContentPage
    {
        private readonly ExercisePickerViewModel _vm;

        public ExercisePickerView(ExercisePickerViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _vm.LoadExercisesCommand.Execute(null);
        }
    }
}