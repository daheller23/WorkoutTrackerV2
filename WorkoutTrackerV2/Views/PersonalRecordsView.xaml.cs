using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class PersonalRecordsView : ContentPage
{
    public PersonalRecordsView(PersonalRecordsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // FIX 9: Cast BindingContext directly — no redundant _vm field.
        // ExecuteAsync is the correct call for async RelayCommands.
        if (BindingContext is PersonalRecordsViewModel vm)
            _ = vm.LoadRecordsCommand.ExecuteAsync(null);
    }
}
