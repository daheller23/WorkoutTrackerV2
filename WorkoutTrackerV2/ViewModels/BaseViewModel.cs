using CommunityToolkit.Mvvm.ComponentModel;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        // FIX 1: Manual PropertyChanged event, SetProperty, and OnPropertyChanged
        // removed entirely. ObservableObject already provides all three correctly —
        // re-declaring them created a shadow conflict where [ObservableProperty]
        // source-generated code and manual property setters could fire different
        // PropertyChanged instances, causing silent binding failures.

        // FIX 2+3: [ObservableProperty] replaces manual backing fields and property
        // boilerplate. ErrorMessage initialized to string.Empty — the manual field
        // was uninitialized (null), which caused binding failures before first set.
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _errorMessage = string.Empty;
    }
}
