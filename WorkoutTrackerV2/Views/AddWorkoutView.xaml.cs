using WorkoutTrackerV2.Services;
using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class AddWorkoutView : ContentPage
{
    // FIX 9: Only ITemplateService stored as a field — it's the only dependency
    // used in the code-behind. ViewModel is accessed via BindingContext cast
    // rather than a redundant private field.
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

        // FIX 9: Cast BindingContext directly rather than using a stored _vm field.
        if (BindingContext is not AddWorkoutViewModel vm) return;

        // Refresh WeightUnitLabel on every appear so it reflects the current
        // setting immediately if the user changed units in Settings and returned.
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
            // Clear stale QueryProperty value on every appear.
            vm.ClearSelectedExerciseCommand.Execute(null);
        }
    }
}
