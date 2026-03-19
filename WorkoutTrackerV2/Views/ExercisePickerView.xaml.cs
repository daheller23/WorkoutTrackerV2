using WorkoutTrackerV2.ViewModels;
namespace WorkoutTrackerV2.Views
{
    public partial class ExercisePickerView : ContentPage
    {
        private readonly ExercisePickerViewModel _vm;
        private bool _isFirstAppear = true;

        public ExercisePickerView(ExercisePickerViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_isFirstAppear)
            {
                _isFirstAppear = false;
                _vm.LoadExercisesCommand.Execute(null);
            }
            else
            {
                // Returning from CreateExercise — reset filter and reload
                _vm.ResetFilterCommand.Execute(null);
                _vm.LoadExercisesCommand.Execute(null);
            }
        }
    }
}