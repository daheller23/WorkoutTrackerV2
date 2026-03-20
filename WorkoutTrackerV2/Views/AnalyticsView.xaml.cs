using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class AnalyticsView : ContentPage
{
    public AnalyticsView(AnalyticsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // FIX 11: Cast BindingContext directly — no redundant _vm field.
        // ExecuteAsync is the correct call for async RelayCommands.
        if (BindingContext is AnalyticsViewModel vm)
            _ = vm.LoadAnalyticsCommand.ExecuteAsync(null);
    }
}
