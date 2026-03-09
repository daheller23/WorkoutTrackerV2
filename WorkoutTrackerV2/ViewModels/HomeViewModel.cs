using CommunityToolkit.Mvvm.Input;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class HomeViewModel : BaseViewModel
    {
        [RelayCommand]
        private async Task StartWorkout()
        {
            await Shell.Current.GoToAsync(Routes.Workout);
        }

        [RelayCommand]
        private async Task ViewHistory()
        {
            await Shell.Current.GoToAsync(Routes.History);
        }


    }
}
