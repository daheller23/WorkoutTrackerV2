using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class SettingsView : ContentPage
{
    private readonly SettingsViewModel _vm;

    public SettingsView(SettingsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
    }
}