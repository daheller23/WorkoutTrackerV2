using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class WorkoutDetailView : ContentPage
{
    public WorkoutDetailView(WorkoutDetailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // FIX 12: Cast BindingContext directly — no redundant _vm field.
        // ExecuteAsync is the correct call for async RelayCommands.
        if (BindingContext is WorkoutDetailViewModel vm)
            _ = vm.LoadDataCommand.ExecuteAsync(null);
    }
}
