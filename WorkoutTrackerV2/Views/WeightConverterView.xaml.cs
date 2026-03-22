using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class WeightConverterView : ContentPage
{
    public WeightConverterView(WeightConverterViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}