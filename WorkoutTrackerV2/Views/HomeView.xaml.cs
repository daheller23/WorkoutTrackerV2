using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class HomeView : ContentPage
{
    public HomeView(HomeViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // FIX: Cast BindingContext rather than storing a redundant private field.
        // ExecuteAsync is the correct call for async RelayCommands — it awaits the
        // underlying task and surfaces exceptions rather than swallowing them.
        if (BindingContext is HomeViewModel vm)
            _ = vm.LoadDataCommand.ExecuteAsync(null);
    }
}
