using WorkoutTrackerV2.ViewModels;
namespace WorkoutTrackerV2.Views
{
    public partial class EditWorkoutView : ContentPage
    {
        private readonly EditWorkoutViewModel _vm;

        public EditWorkoutView(EditWorkoutViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            BindingContext = viewModel;
        }
    }
}