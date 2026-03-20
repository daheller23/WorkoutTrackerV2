using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using SkiaSharp;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class BodyWeightViewModel(
        IBodyWeightService bodyWeightService,
        ISettingsService settingsService) : BaseViewModel
    {
        // Cached chart colours — parsed once, reused on every BuildChart call.
        private static readonly SKColor ColorBlue = SKColor.Parse("#1F77F0");
        private static readonly SKColor ColorGreen = SKColor.Parse("#4CAF50");
        private static readonly SKColor ColorGrey = SKColor.Parse("#999999");
        private static readonly SKColor ColorWhite = SKColor.Parse("#FFFFFF");
        private static readonly SKColor ColorGrid = SKColor.Parse("#F0F0F0");
        private static readonly SKPaint PaintText = new() { Color = ColorGrey, TextSize = 24 };
        private static readonly SKPaint PaintGrid = new() { Color = ColorGrid, StrokeWidth = 1 };

        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ObservableCollection<BodyWeightEntry> _entries = [];
        [ObservableProperty] private LineChart? _chart;
        [ObservableProperty] private int _selectedDays = 90;
        [ObservableProperty] private string _weightUnitLabel = "lbs";

        // Add entry fields
        [ObservableProperty] private string _newWeight = string.Empty;
        [ObservableProperty] private string _newNotes = string.Empty;
        [ObservableProperty] private DateTime _newDate = DateTime.Today;
        [ObservableProperty] private bool _isAddingEntry;

        // True only when there are >= 2 entries so the chart section shows/hides correctly.
        [ObservableProperty] private bool _hasChart;

        // Stats
        [ObservableProperty] private string _currentWeightLabel = "--";
        [ObservableProperty] private string _startingWeightLabel = "--";
        [ObservableProperty] private string _changeLabel = "--";
        [ObservableProperty] private string _changeColor = "#999999";
        [ObservableProperty] private string _weeklyAverageLabel = "--";
        [ObservableProperty] private string _personalLowLabel = "--";
        [ObservableProperty] private string _personalHighLabel = "--";
        [ObservableProperty] private string _bmiLabel = "--";
        [ObservableProperty] private string _bmiCategory = string.Empty;
        [ObservableProperty] private bool _hasBmi;
        [ObservableProperty] private bool _hasData;

        public List<TimePeriodPillViewModel> TimePeriodPills { get; } =
        [
            new() { Label = "30d",  Days = 30  },
            new() { Label = "90d",  Days = 90,  IsSelected = true },
            new() { Label = "180d", Days = 180 },
            new() { Label = "1yr",  Days = 365 },
            new() { Label = "All",  Days = 0   },
        ];
        #endregion

        #region "PARTIAL METHODS"
        partial void OnSelectedDaysChanged(int value)
        {
            foreach (var pill in TimePeriodPills)
                pill.IsSelected = pill.Days == value;
            _ = LoadDataAsync();
        }
        #endregion

        #region "LOAD DATA"
        [RelayCommand]
        private async Task LoadData() => await LoadDataAsync();

        private async Task LoadDataAsync()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                WeightUnitLabel = settingsService.WeightUnit;

                // Single DB fetch — stats recomputed in memory from same list.
                var entries = await bodyWeightService.GetEntriesAsync(SelectedDays);
                Entries = new ObservableCollection<BodyWeightEntry>(entries);
                RefreshUI(entries);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region "ADD ENTRY"
        [RelayCommand]
        private void ShowAddEntry() => IsAddingEntry = true;

        [RelayCommand]
        private void CancelAddEntry()
        {
            IsAddingEntry = false;
            NewWeight = string.Empty;
            NewNotes = string.Empty;
            NewDate = DateTime.Today;
        }

        [RelayCommand]
        private async Task SaveEntry()
        {
            if (!double.TryParse(NewWeight, out double weight) || weight <= 0)
            {
                await Shell.Current.DisplayAlertAsync(
                    "Invalid Weight", "Please enter a valid weight.", "OK");
                return;
            }

            try
            {
                var entry = new BodyWeightEntry
                {
                    Weight = weight,
                    Unit = WeightUnitLabel,
                    Date = NewDate.Date.Add(TimeOfDay()),
                    Notes = NewNotes.Trim()
                };

                // Save to DB first so entry gets its auto-increment Id.
                await bodyWeightService.SaveEntryAsync(entry);

                // Reset form immediately so the UI feels responsive.
                IsAddingEntry = false;
                NewWeight = string.Empty;
                NewNotes = string.Empty;
                NewDate = DateTime.Today;

                // Insert into the in-memory collection at the correct position
                // (entries are ordered descending by date) — no DB reload needed.
                var insertAt = Entries.TakeWhile(e => e.Date > entry.Date).Count();
                Entries.Insert(insertAt, entry);

                // Recompute stats and chart from the updated in-memory list.
                RefreshUI(Entries.ToList());
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
        #endregion

        #region "DELETE ENTRY"
        [RelayCommand]
        private async Task DeleteEntry(BodyWeightEntry entry)
        {
            bool confirmed = await Shell.Current.DisplayAlertAsync(
                "Delete Entry",
                $"Delete {entry.Weight:F1} {entry.Unit} on {entry.Date:MMM d}?",
                "Delete", "Cancel");
            if (!confirmed) return;

            try
            {
                await bodyWeightService.DeleteEntryAsync(entry);

                // Remove from in-memory collection and recompute — no DB reload.
                Entries.Remove(entry);
                RefreshUI(Entries.ToList());
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
        }
        #endregion

        #region "SET TIME PERIOD"
        [RelayCommand]
        private void SetTimePeriod(string days)
        {
            if (int.TryParse(days, out int result))
                SelectedDays = result;
        }
        #endregion

        #region "GO BACK"
        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
        #endregion

        #region "PRIVATE HELPERS"
        private static TimeSpan TimeOfDay() =>
            new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute, 0);

        // RefreshUI: computes all stats and rebuilds the chart entirely in memory
        // from the provided list. Called after load, save, and delete so none of
        // those paths need a second DB round-trip.
        private void RefreshUI(List<BodyWeightEntry> entries)
        {
            HasData = entries.Count > 0;
            if (!HasData)
            {
                ApplyStats(new BodyWeightStats());
                Chart = null;
                HasChart = false;
                return;
            }

            var unit = WeightUnitLabel;
            var ordered = entries.OrderBy(e => e.Date).ToList();

            // Convert all weights to the display unit in a single pass.
            var weights = ordered.Select(e =>
                e.Unit == unit ? e.Weight
                : e.Unit == "lbs" ? e.Weight * 0.453592
                : e.Weight / 0.453592).ToList();

            double current = weights[^1];
            double starting = weights[0];
            var weekCutoff = DateTime.Now.AddDays(-7);
            var weekWeights = ordered
                .Select((e, i) => (entry: e, w: weights[i]))
                .Where(x => x.entry.Date >= weekCutoff)
                .Select(x => x.w)
                .ToList();

            double? bmi = null;
            double heightCm = settingsService.HeightCm;
            if (heightCm > 0)
            {
                double weightKg = unit == "kg" ? current : current * 0.453592;
                double heightM = heightCm / 100.0;
                bmi = Math.Round(weightKg / (heightM * heightM), 1);
            }

            ApplyStats(new BodyWeightStats
            {
                CurrentWeight = Math.Round(current, 1),
                StartingWeight = Math.Round(starting, 1),
                Change = Math.Round(current - starting, 1),
                WeeklyAverage = weekWeights.Count > 0
                    ? Math.Round(weekWeights.Average(), 1) : null,
                PersonalLowest = Math.Round(weights.Min(), 1),
                PersonalHighest = Math.Round(weights.Max(), 1),
                Bmi = bmi
            });

            BuildChart(ordered, weights);
        }

        private void ApplyStats(BodyWeightStats stats)
        {
            if (!stats.HasData)
            {
                CurrentWeightLabel = "--";
                StartingWeightLabel = "--";
                ChangeLabel = "--";
                WeeklyAverageLabel = "--";
                PersonalLowLabel = "--";
                PersonalHighLabel = "--";
                BmiLabel = "--";
                HasBmi = false;
                return;
            }

            var u = WeightUnitLabel;
            CurrentWeightLabel = $"{stats.CurrentWeight:F1} {u}";
            StartingWeightLabel = $"{stats.StartingWeight:F1} {u}";
            WeeklyAverageLabel = stats.WeeklyAverage.HasValue
                ? $"{stats.WeeklyAverage:F1} {u}"
                : "--";
            PersonalLowLabel = $"{stats.PersonalLowest:F1} {u}";
            PersonalHighLabel = $"{stats.PersonalHighest:F1} {u}";

            if (stats.Change.HasValue)
            {
                double c = stats.Change.Value;
                ChangeLabel = c == 0 ? "No change"
                    : c > 0 ? $"+{c:F1} {u}"
                    : $"{c:F1} {u}";
                ChangeColor = c > 0 ? "#FF9800" : c < 0 ? "#4CAF50" : "#999999";
            }

            if (stats.Bmi.HasValue)
            {
                BmiLabel = stats.Bmi.Value.ToString("F1");
                BmiCategory = stats.Bmi.Value switch
                {
                    < 18.5 => "Underweight",
                    < 25.0 => "Normal",
                    < 30.0 => "Overweight",
                    _ => "Obese"
                };
                HasBmi = true;
            }
            else
            {
                HasBmi = false;
            }
        }

        // Accepts pre-ordered entries and pre-converted weights from RefreshUI
        // so this method does zero sorting, zero unit conversion, zero SKColor.Parse.
        private void BuildChart(List<BodyWeightEntry> ordered, List<double> weights)
        {
            if (ordered.Count < 2)
            {
                Chart = null;
                HasChart = false;
                return;
            }

            double best = weights.Min();  // lowest weight = personal best marker

            var chartEntries = ordered.Select((e, i) =>
            {
                double w = weights[i];
                bool isBest = Math.Abs(w - best) < 0.01;
                return new ChartEntry((float)w)
                {
                    Label = e.Date.ToString("MMM d"),
                    ValueLabel = isBest ? $"⭐ {w:F1}" : w.ToString("F1"),
                    Color = isBest ? ColorGreen : ColorBlue,
                    TextColor = ColorGrey,
                    ValueLabelColor = isBest ? ColorGreen : ColorBlue
                };
            }).ToList();

            HasChart = true;
            Chart = new LineChart
            {
                Entries = chartEntries,
                LineMode = LineMode.Spline,
                LineSize = 3,
                PointMode = PointMode.Circle,
                PointSize = 10,
                BackgroundColor = ColorWhite,
                LabelTextSize = 26,
                ValueLabelTextSize = 26,
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                ShowYAxisLines = true,
                ShowYAxisText = true,
                YAxisTextPaint = PaintText,
                YAxisLinesPaint = PaintGrid,
                LineAreaAlpha = 20
            };
        }
        #endregion
    }
}
