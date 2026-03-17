using WorkoutTrackerV2.ViewModels;
namespace WorkoutTrackerV2.Views;

public partial class WorkoutHistoryView : ContentPage
{
    private readonly WorkoutHistoryViewModel _vm;
    public WorkoutHistoryView(WorkoutHistoryViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.LoadSessionsCommand.Execute(null);
    }
}