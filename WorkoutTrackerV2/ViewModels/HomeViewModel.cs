using CommunityToolkit.Mvvm.Input;
using WorkoutTrackerV2.Views;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class HomeViewModel : BaseViewModel
    {
        [RelayCommand]
        private async Task StartWorkout()
        {
            await Shell.Current.GoToAsync(Routes.Workout);
        }



    }
}
