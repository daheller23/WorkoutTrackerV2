using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class PlateCalculatorView : ContentPage
{
    public PlateCalculatorView(PlateCalculatorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm; 
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ((PlateCalculatorViewModel)BindingContext).InitialiseCommand.Execute(null);
    }
}
