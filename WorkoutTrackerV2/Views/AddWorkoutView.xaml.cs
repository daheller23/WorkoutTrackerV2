using WorkoutTrackerV2.Services;
using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class AddWorkoutView : ContentPage
{
    private readonly ITemplateService _templateService;

    public AddWorkoutView(AddWorkoutViewModel vm, ITemplateService templateService)
    {
        InitializeComponent();
        _templateService = templateService;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is not AddWorkoutViewModel vm) return;

        vm.TimerViewModel.Subscribe();
        vm.RefreshWeightUnitCommand.Execute(null);

        if (_templateService.PendingTemplate is not null)
        {
            var template = _templateService.PendingTemplate;
            var sets = _templateService.PendingTemplateSets;
            _templateService.PendingTemplate = null;
            _templateService.PendingTemplateSets = [];

            if (sets.Count > 0)
                _ = vm.LoadFromTemplateSetsCommand.ExecuteAsync((template, sets));
            else
                _ = vm.LoadFromTemplateCommand.ExecuteAsync(template);
        }
        else
        {
            vm.ClearSelectedExerciseCommand.Execute(null);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (BindingContext is AddWorkoutViewModel vm)
            vm.TimerViewModel.Unsubscribe();
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (BindingContext is AddWorkoutViewModel viewModel)
        {
            await viewModel.OnNavigatedToAsync();
        }
    }
}