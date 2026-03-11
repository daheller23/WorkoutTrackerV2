using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views
{
    public partial class WorkoutDetailView : ContentPage
    {
        public WorkoutDetailView(WorkoutDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}