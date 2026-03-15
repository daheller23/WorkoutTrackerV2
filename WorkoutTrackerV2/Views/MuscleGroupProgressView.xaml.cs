using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views
{
    public partial class MuscleGroupProgressView : ContentPage
    {
        public MuscleGroupProgressView(MuscleGroupProgressViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}