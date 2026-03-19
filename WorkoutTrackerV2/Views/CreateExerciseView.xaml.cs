using WorkoutTrackerV2.ViewModels;
namespace WorkoutTrackerV2.Views
{
    public partial class CreateExerciseView : ContentPage
    {
        public CreateExerciseView(CreateExerciseViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}