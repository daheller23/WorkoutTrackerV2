using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views
{
    public partial class SettingsView : ContentPage
    {
        public SettingsView(SettingsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is SettingsViewModel vm)
                vm.LoadSettingsCommand.Execute(null);
        }
    }
}