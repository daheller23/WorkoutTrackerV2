using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Helpers;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    // ── FormulaChipViewModel ─────────────────────────────────────────────────
    public partial class FormulaChipViewModel : ObservableObject
    {
        public string Key { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;

        [ObservableProperty] private bool _isSelected;
    }

    public partial class OneRepMaxCalculatorViewModel(
        ISettingsService settingsService) : BaseViewModel
    {
        [ObservableProperty] private string _weightText = string.Empty;
        [ObservableProperty] private string _repsText = string.Empty;
        [ObservableProperty] private string _weightUnit = "lbs";
        [ObservableProperty] private string _formula = OneRepMaxCalculator.FormulaEpley;

        // ── Result state ─────────────────────────────────────────────────────
        [ObservableProperty] private bool _hasResult;
        [ObservableProperty] private double _oneRepMax;
        [ObservableProperty] private string _formulaLabel = string.Empty;

        // ── Formula chips ────────────────────────────────────────────────────
        public ObservableCollection<FormulaChipViewModel> FormulaChips { get; } =
        [
            new() { Key = OneRepMaxCalculator.FormulaEpley,   Label = "Epley",   Subtitle = "w × (1 + r/30)",    IsSelected = true  },
            new() { Key = OneRepMaxCalculator.FormulaBrzycki, Label = "Brzycki", Subtitle = "w × 36/(37−r)",     IsSelected = false },
        ];

        // ── Rep percentage table ─────────────────────────────────────────────
        public List<RepPercentageRow> RepTable { get; } = [];

        // ── Initialise ───────────────────────────────────────────────────────
        [RelayCommand]
        private void Initialise()
        {
            WeightUnit = settingsService.WeightUnit;
            Formula = settingsService.RmFormula;
            FormulaLabel = Formula;
            SyncFormulaChips();
        }

        // ── Set formula ──────────────────────────────────────────────────────
        [RelayCommand]
        private void SetFormula(string formula)
        {
            Formula = formula;
            SyncFormulaChips();
        }

        private void SyncFormulaChips()
        {
            foreach (var chip in FormulaChips)
                chip.IsSelected = chip.Key == Formula;
        }

        // ── Calculate ────────────────────────────────────────────────────────
        [RelayCommand]
        private void Calculate()
        {
            HasResult = false;
            RepTable.Clear();

            if (!double.TryParse(WeightText, out double weight) || weight <= 0) return;
            if (!int.TryParse(RepsText, out int reps) || reps <= 0) return;

            double orm = OneRepMaxCalculator.Calculate(weight, reps, Formula);
            if (orm <= 0) return;

            OneRepMax = orm;
            FormulaLabel = Formula;
            HasResult = true;

            // Build a rep percentage table from 100% down to 50% in 5% steps.
            // Useful for knowing what weight to use for different rep ranges.
            RepTable.Clear();
            foreach (int pct in new[] { 100, 95, 90, 85, 80, 75, 70, 65, 60, 55, 50 })
            {
                double w = orm * pct / 100.0;
                // Estimate reps achievable at this % using inverse Epley:
                // reps ≈ 30 × (1RM/weight − 1)  — capped at 30 for display
                int estimatedReps = pct == 100 ? 1 : Math.Min(30, (int)(30.0 * (orm / w - 1)));
                RepTable.Add(new RepPercentageRow
                {
                    Percentage = pct,
                    Weight = w,
                    EstimatedReps = estimatedReps,
                    WeightUnit = WeightUnit
                });
            }

            OnPropertyChanged(nameof(RepTable));
        }

        // ── Clear ────────────────────────────────────────────────────────────
        [RelayCommand]
        private void Clear()
        {
            WeightText = string.Empty;
            RepsText = string.Empty;
            HasResult = false;
            RepTable.Clear();
            OnPropertyChanged(nameof(RepTable));
        }

        // ── Go back ──────────────────────────────────────────────────────────
        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);

        partial void OnWeightUnitChanged(string value) => HasResult = false;
        partial void OnFormulaChanged(string value)
        {
            FormulaLabel = value;
            SyncFormulaChips();
            if (HasResult) Calculate();
        }
    }

    // ── RepPercentageRow ──────────────────────────────────────────────────────
    public class RepPercentageRow
    {
        public int Percentage { get; init; }
        public double Weight { get; init; }
        public int EstimatedReps { get; init; }
        public string WeightUnit { get; init; } = "lbs";

        public string PercentageLabel => $"{Percentage}%";
        public string WeightLabel => $"{Weight:F1} {WeightUnit}";
        public string RepsLabel => EstimatedReps == 1 ? "1 rep" : $"~{EstimatedReps} reps";
        public bool IsMax => Percentage == 100;
    }
}
