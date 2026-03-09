using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class WorkoutHistoryView : ContentPage
{
	public WorkoutHistoryView(WorkoutHistoryViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}