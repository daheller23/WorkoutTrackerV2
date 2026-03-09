using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class AddWorkoutView : ContentPage
{
    public AddWorkoutView(AddWorkoutViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}