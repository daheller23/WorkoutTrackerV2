using WorkoutTrackerV2.Services;
using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class AddWorkoutView : ContentPage
{
    private readonly AddWorkoutViewModel _vm;
    private readonly ITemplateService _templateService;

    public AddWorkoutView(AddWorkoutViewModel vm, ITemplateService templateService)
    {
        InitializeComponent();
        _vm = vm;
        _templateService = templateService;
        BindingContext = vm;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_templateService.PendingTemplate is not null)
        {
            var template = _templateService.PendingTemplate;
            _templateService.PendingTemplate = null;
            _vm.LoadFromTemplateCommand.Execute(template);
        }
    }
}