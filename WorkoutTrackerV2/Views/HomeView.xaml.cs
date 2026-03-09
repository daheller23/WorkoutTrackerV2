using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class HomeView : ContentPage
{
	public HomeView(HomeViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}