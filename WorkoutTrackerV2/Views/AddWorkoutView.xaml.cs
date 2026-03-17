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
            var sets = _templateService.PendingTemplateSets;
            _templateService.PendingTemplate = null;
            _templateService.PendingTemplateSets = [];

            if (sets.Count > 0)
                _vm.LoadFromTemplateSetsCommand.Execute((template, sets));
            else
                _vm.LoadFromTemplateCommand.Execute(template);
        }
    }
}