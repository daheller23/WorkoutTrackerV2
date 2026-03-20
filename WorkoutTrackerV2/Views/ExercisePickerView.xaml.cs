using WorkoutTrackerV2.ViewModels;

namespace WorkoutTrackerV2.Views;

public partial class ExercisePickerView : ContentPage
{
    private bool _isFirstAppear = true;

    public ExercisePickerView(ExercisePickerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is not ExercisePickerViewModel vm) return;

        if (_isFirstAppear)
        {
            _isFirstAppear = false;
            _ = vm.LoadExercisesCommand.ExecuteAsync(null);
        }
        else
        {
            // Returning from CreateExercise — reset search/filter state first
            // so the newly created exercise is visible, then reload.
            // ResetFilter is called synchronously (it's not async) before
            // LoadExercises so the filter state is clean before the DB fetch.
            // Note: LoadExercises will call ScheduleFilter at the end, so the
            // intermediate ScheduleFilter fired by ResetFilter will be cancelled
            // and replaced — this is harmless but expected.
            vm.ResetFilterCommand.Execute(null);
            _ = vm.LoadExercisesCommand.ExecuteAsync(null);
        }
    }
}
