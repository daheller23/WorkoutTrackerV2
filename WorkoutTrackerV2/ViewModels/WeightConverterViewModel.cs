using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WorkoutTrackerV2.ViewModels;

public partial class WeightConverterViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _inputValue = string.Empty;

    [ObservableProperty]
    private bool _isLbsToKg = true;

    public string InputLabel => IsLbsToKg ? "POUNDS" : "KILOGRAMS";
    public string ResultLabel => IsLbsToKg ? "KILOGRAMS" : "POUNDS";
    public string InputUnit => IsLbsToKg ? "lbs" : "kg";
    public string ResultUnit => IsLbsToKg ? "kg" : "lbs";

    public double CalculatedValue
    {
        get
        {
            if (string.IsNullOrWhiteSpace(InputValue) || !double.TryParse(InputValue, out double val))
                return 0;

            return IsLbsToKg ? val * 0.453592 : val * 2.20462;
        }
    }

    [RelayCommand]
    private void SwitchUnits()
    {
        // If switching units, we try to convert the current input 
        // to the new unit so the user doesn't lose their place
        if (double.TryParse(InputValue, out double currentVal))
        {
            double converted = IsLbsToKg ? currentVal * 0.453592 : currentVal * 2.20462;
            InputValue = converted.ToString("F1");
        }

        IsLbsToKg = !IsLbsToKg;
        RefreshProperties();
    }

    partial void OnInputValueChanged(string value) => OnPropertyChanged(nameof(CalculatedValue));

    private void RefreshProperties()
    {
        OnPropertyChanged(nameof(InputLabel));
        OnPropertyChanged(nameof(ResultLabel));
        OnPropertyChanged(nameof(InputUnit));
        OnPropertyChanged(nameof(ResultUnit));
        OnPropertyChanged(nameof(CalculatedValue));
    }

    [RelayCommand]
    private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
}