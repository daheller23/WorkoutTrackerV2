using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class OneRepMaxCalculatorView : ContentPage
{
    public OneRepMaxCalculatorView(OneRepMaxCalculatorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ((OneRepMaxCalculatorViewModel)BindingContext).InitialiseCommand.Execute(null);
    }
}
