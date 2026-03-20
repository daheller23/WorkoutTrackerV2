using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class BodyWeightView : ContentPage
{
    public BodyWeightView(BodyWeightViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is BodyWeightViewModel vm)
            _ = vm.LoadDataCommand.ExecuteAsync(null);
    }
}
