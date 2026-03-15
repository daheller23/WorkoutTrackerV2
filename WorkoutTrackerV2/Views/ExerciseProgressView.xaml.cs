using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views
{
    public partial class ExerciseProgressView : ContentPage
    {
        public ExerciseProgressView(ExerciseProgressViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}