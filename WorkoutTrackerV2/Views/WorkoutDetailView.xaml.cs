using WorkoutTrackerV2.ViewModels;
namespace WorkoutTrackerV2.Views
{
    public partial class WorkoutDetailView : ContentPage
    {
        private readonly WorkoutDetailViewModel _vm;

        public WorkoutDetailView(WorkoutDetailViewModel viewModel)
        {
            InitializeComponent();
            _vm = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _vm.LoadDataCommand.Execute(null);
        }
    }
}