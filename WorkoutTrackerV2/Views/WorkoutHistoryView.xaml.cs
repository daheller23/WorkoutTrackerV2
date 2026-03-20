using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class WorkoutHistoryView : ContentPage
{
    public WorkoutHistoryView(WorkoutHistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // FIX 13: Cast BindingContext directly — no redundant _vm field.
        // ExecuteAsync is the correct call for async RelayCommands.
        if (BindingContext is WorkoutHistoryViewModel vm)
            _ = vm.LoadSessionsCommand.ExecuteAsync(null);
    }
}
