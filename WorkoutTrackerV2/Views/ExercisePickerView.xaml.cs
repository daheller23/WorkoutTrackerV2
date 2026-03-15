using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views
{
    public partial class ExercisePickerView : ContentPage
    {
        public ExercisePickerView(ExercisePickerViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is ExercisePickerViewModel vm)
                vm.LoadExercisesCommand.Execute(null);
        }
    }
}