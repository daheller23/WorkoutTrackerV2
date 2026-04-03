using CommunityToolkit.Mvvm.ComponentModel;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _errorMessage = string.Empty;
    }
}
