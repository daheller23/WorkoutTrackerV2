using WorkoutTrackerV2.ViewModels;
namespace WorkoutTrackerV2.Views;

public partial class AnalyticsView
{
    private readonly AnalyticsViewModel _vm;

    public AnalyticsView(AnalyticsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadAnalyticsCommand.Execute(null);
    }
}