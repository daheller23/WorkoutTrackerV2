using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class AnalyticsView
{
	public AnalyticsView(AnalyticsViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}
}