using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class AddWorkoutView : ContentPage
{
    private readonly AddWorkoutViewModel _vm;

    public AddWorkoutView(AddWorkoutViewModel vm)
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