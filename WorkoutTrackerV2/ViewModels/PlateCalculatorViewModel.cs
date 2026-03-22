using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    // ── PlateViewModel ────────────────────────────────────────────────────────
    public partial class PlateViewModel : ObservableObject
    {
        public double Weight { get; init; }
        public string Label { get; init; } = string.Empty;

        [ObservableProperty] private bool _isAvailable = true;
        [ObservableProperty] private int _count;

        partial void OnCountChanged(int value) => OnPropertyChanged(nameof(SubtotalPerSide));

        public double SubtotalPerSide => Count * Weight;
    }

    // ── BarChipViewModel ──────────────────────────────────────────────────────
    // Wraps a bar weight option so IsSelected (a plain bool) can drive
    // DataTrigger.Value in the XAML. DataTrigger.Value must be a literal —
    // it cannot be a Binding — so comparing BarWeight == chip.Weight has to
    // happen in the VM, not in the XAML.
    public partial class BarChipViewModel : ObservableObject
    {
        public double Weight { get; init; }
        public string Label { get; init; } = string.Empty;

        [ObservableProperty] private bool _isSelected;
    }

    // ── PlateCalculatorViewModel ──────────────────────────────────────────────
    public partial class PlateCalculatorViewModel(
        ISettingsService settingsService) : BaseViewModel
    {
        [ObservableProperty] private string _targetWeightText = string.Empty;
        [ObservableProperty] private double _barWeight;
        [ObservableProperty] private string _weightUnit = "lbs";
        [ObservableProperty] private string _resultMessage = string.Empty;
        [ObservableProperty] private bool _hasResult;
        [ObservableProperty] private bool _hasError;
        [ObservableProperty] private string _errorMessage = string.Empty;
        [ObservableProperty] private double _totalPerSide;

        public ObservableCollection<PlateViewModel> AvailablePlates { get; } = [];
        public ObservableCollection<PlateViewModel> PlatesPerSide { get; } = [];
        public ObservableCollection<BarChipViewModel> BarChips { get; } = [];

        // ── Initialise ───────────────────────────────────────────────────────
        [RelayCommand]
        private void Initialise()
        {
            WeightUnit = settingsService.WeightUnit;
            BuildPlateInventory();
            BuildBarChips();
        }

        private void BuildPlateInventory()
        {
            AvailablePlates.Clear();

            if (WeightUnit == "lbs")
            {
                AvailablePlates.Add(new PlateViewModel { Weight = 45, Label = "45", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 35, Label = "35", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 25, Label = "25", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 10, Label = "10", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 5, Label = "5", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 2.5, Label = "2.5", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 1.25, Label = "1.25", IsAvailable = false });
            }
            else
            {
                AvailablePlates.Add(new PlateViewModel { Weight = 20, Label = "20", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 15, Label = "15", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 10, Label = "10", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 5, Label = "5", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 2.5, Label = "2.5", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 1.25, Label = "1.25", IsAvailable = true });
                AvailablePlates.Add(new PlateViewModel { Weight = 0.5, Label = "0.5", IsAvailable = false });
            }
        }

        private void BuildBarChips()
        {
            BarChips.Clear();

            double[] weights = WeightUnit == "lbs" ? [45, 35, 15] : [20, 15, 10];
            double defaultBar = weights[0];

            foreach (var w in weights)
                BarChips.Add(new BarChipViewModel
                {
                    Weight = w,
                    Label = w.ToString("F0"),
                    IsSelected = w == defaultBar
                });

            BarWeight = defaultBar;
        }

        // ── Commands ─────────────────────────────────────────────────────────
        [RelayCommand]
        private void SetBar(double weight)
        {
            BarWeight = weight;
            foreach (var chip in BarChips)
                chip.IsSelected = chip.Weight == weight;
            Calculate();
        }

        [RelayCommand]
        private void TogglePlate(PlateViewModel plate)
        {
            plate.IsAvailable = !plate.IsAvailable;
            Calculate();
        }

        [RelayCommand]
        private void Calculate()
        {
            HasResult = false;
            HasError = false;
            ErrorMessage = string.Empty;
            PlatesPerSide.Clear();

            if (!double.TryParse(TargetWeightText, out double target) || target <= 0)
                return;

            if (target <= BarWeight)
            {
                HasError = true;
                ErrorMessage = $"Target ({target:F1}) must be greater than bar weight ({BarWeight:F1} {WeightUnit}).";
                return;
            }

            double needed = (target - BarWeight) / 2.0;
            double remaining = needed;

            var plates = AvailablePlates
                .Where(p => p.IsAvailable)
                .OrderByDescending(p => p.Weight)
                .ToList();

            // Zero all counts before recalculating.
            foreach (var p in AvailablePlates) p.Count = 0;

            var used = new List<PlateViewModel>();
            foreach (var plate in plates)
            {
                if (remaining < 0.001) break;
                int count = (int)(remaining / plate.Weight + 0.001); // small epsilon for float precision
                if (count > 0)
                {
                    remaining -= count * plate.Weight;
                    plate.Count = count;
                    used.Add(plate);
                }
            }

            if (remaining > 0.01)
            {
                HasError = true;
                ErrorMessage = $"Closest to {target:F1} is {target - remaining * 2:F1} {WeightUnit} — {remaining * 2:F1} short with available plates.";
            }

            foreach (var p in used)
                PlatesPerSide.Add(p);

            TotalPerSide = needed - remaining;
            double actual = BarWeight + TotalPerSide * 2;
            ResultMessage = remaining < 0.01
                ? $"{actual:F1} {WeightUnit} — {PlatesPerSide.Sum(p => p.Count)} plates per side"
                : $"Closest: {actual:F1} {WeightUnit}";

            HasResult = true;
        }

        [RelayCommand]
        private void Clear()
        {
            TargetWeightText = string.Empty;
            PlatesPerSide.Clear();
            HasResult = false;
            HasError = false;
            ErrorMessage = string.Empty;
            foreach (var p in AvailablePlates) p.Count = 0;
        }

        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);

        partial void OnWeightUnitChanged(string value)
        {
            BuildPlateInventory();
            BuildBarChips();
            Clear();
        }
    }
}
