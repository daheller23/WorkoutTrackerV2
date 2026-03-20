using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

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
        // LoadSettings is synchronous — Execute() is correct here,
        // not ExecuteAsync. No async work is performed.
        if (BindingContext is SettingsViewModel vm)
            vm.LoadSettingsCommand.Execute(null);
    }
}
