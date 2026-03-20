using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class TemplatePickerView : ContentPage
{
    public TemplatePickerView(TemplatePickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // FIX 6: ExecuteAsync is the correct call for async RelayCommands.
        if (BindingContext is TemplatePickerViewModel vm)
            _ = vm.LoadTemplatesCommand.ExecuteAsync(null);
    }
}
