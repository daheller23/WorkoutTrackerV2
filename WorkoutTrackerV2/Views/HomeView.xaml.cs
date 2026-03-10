using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class HomeView : ContentPage
{
    private readonly HomeViewModel _vm;

    public HomeView(HomeViewModel vm)
	{
		InitializeComponent();
        _vm = vm;
		BindingContext = vm;
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadDataCommand.Execute(null);
    }
}