using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views
{
    public partial class EditWorkoutView : ContentPage
    {
        public EditWorkoutView(EditWorkoutViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}