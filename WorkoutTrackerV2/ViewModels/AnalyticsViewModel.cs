using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    // FIX 8: Pill ViewModel — replaces DaysToColorConverter + DaysToTextColorConverter
    // (2 converter calls per pill × 6 pills = 12 converter invocations per tap).
    // DataTriggers on IsSelected drive colour with zero converter overhead.
    public partial class TimePeriodPillViewModel : ObservableObject
    {
        public string Label { get; init; } = string.Empty;
        public int Days { get; init; }
        public string DaysString => Days.ToString();

        [ObservableProperty]
        private bool _isSelected;
    }

    public partial class AnalyticsViewModel(
        IWorkoutService workoutService,
        IAnalyticsService analyticsService,
        ISettingsService settingsService) : BaseViewModel
    {
        // FIX 5: Static Random instance — avoids constructing a new Random on
        // every GenerateInsight call (which risks correlated seeds on fast CPUs).
        private static readonly Random _rng = new();

        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private List<DailyStats> _dailyStats = [];
        [ObservableProperty] private ObservableCollection<ExerciseProgress> _topExercises = [];
        [ObservableProperty] private ObservableCollection<MuscleGroupProgress> _muscleGroupProgress = [];
        [ObservableProperty] private int _selectedDays = 30;
        [ObservableProperty] private double _totalVolumeLifted;
        [ObservableProperty] private double _averageVolume;
        [ObservableProperty] private int _totalSets;
        [ObservableProperty] private Dictionary<DateTime, double> _heatmapData = [];
        [ObservableProperty] private DateTime _heatmapMonth = DateTime.Today;
        [ObservableProperty] private string _heatmapTitle = DateTime.Today.ToString("MMMM yyyy");
        [ObservableProperty] private List<double> _volumeSparkline = [];
        [ObservableProperty] private List<double> _setsSparkline = [];
        [ObservableProperty] private List<double> _daysSparkline = [];
        [ObservableProperty] private List<double> _avgVolumeSparkline = [];
        [ObservableProperty] private string _insightMessage = string.Empty;
        [ObservableProperty] private string _insightEmoji = "💡";
        [ObservableProperty] private string _bestWeekLabel = string.Empty;
        [ObservableProperty] private double _bestWeekVolume;
        [ObservableProperty] private string _bestWeekMuscleGroups = string.Empty;
        [ObservableProperty] private string _weightUnitLabel = "lbs";

        // FIX 8: Pill ViewModels — constructed once, IsSelected toggled on change.
        public List<TimePeriodPillViewModel> TimePeriodPills { get; } =
        [
            new() { Label = "All", Days = 0  },
            new() { Label = "7d",  Days = 7  },
            new() { Label = "14d", Days = 14 },
            new() { Label = "30d", Days = 30, IsSelected = true },
            new() { Label = "60d", Days = 60 },
            new() { Label = "90d", Days = 90 },
        ];
        #endregion

        #region "PARTIAL METHODS"
        partial void OnSelectedDaysChanged(int value)
        {
            // Update pill selection — cheap bool toggle on 6 items.
            foreach (var pill in TimePeriodPills)
                pill.IsSelected = pill.Days == value;

            // FIX 3: Call the async method directly instead of going through
            // LoadAnalyticsCommand.Execute() inside a property-changed handler.
            _ = LoadAnalyticsAsync();
        }

        partial void OnHeatmapMonthChanged(DateTime value)
        {
            HeatmapTitle = value.ToString("MMMM yyyy");
            UpdateHeatmapForMonth();
        }
        #endregion

        #region "SET TIME PERIOD"
        [RelayCommand]
        private void SetTimePeriod(string days)
        {
            // CommandParameter bindings from TapGestureRecognizer always arrive as
            // strings or boxed objects — parsing is safer than relying on int unboxing.
            if (int.TryParse(days, out int result))
                SelectedDays = result;
        }
        #endregion

        #region "HEATMAP NAVIGATION"
        [RelayCommand]
        private void PreviousMonth() => HeatmapMonth = HeatmapMonth.AddMonths(-1);

        [RelayCommand]
        private void NextMonth()
        {
            if (HeatmapMonth.Month < DateTime.Today.Month || HeatmapMonth.Year < DateTime.Today.Year)
                HeatmapMonth = HeatmapMonth.AddMonths(1);
        }

        private void UpdateHeatmapForMonth()
        {
            var monthStart = new DateTime(HeatmapMonth.Year, HeatmapMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var heatmap = new Dictionary<DateTime, double>();
            foreach (var stat in DailyStats.Where(s => s.Date >= monthStart && s.Date < monthEnd))
                heatmap[stat.Date] = stat.TotalWeightLifted;
            HeatmapData = heatmap;
        }
        #endregion

        #region "VIEW PERSONAL RECORDS"
        [RelayCommand]
        private static Task ViewPersonalRecords() => Shell.Current.GoToAsync(Routes.PersonalRecords);
        #endregion

        #region "LOAD ANALYTICS"
        [RelayCommand]
        private async Task LoadAnalytics() => await LoadAnalyticsAsync();

        // FIX 3: Shared implementation called directly from OnSelectedDaysChanged
        // and via the public RelayCommand — avoids Execute() inside a partial method.
        private async Task LoadAnalyticsAsync()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                WeightUnitLabel = settingsService.WeightUnit;

                // Fire all independent service calls concurrently.
                var statsTask = analyticsService.GetDailyStatsAsync(SelectedDays);
                var muscleTask = analyticsService.GetMuscleGroupProgressAsync(SelectedDays);
                await Task.WhenAll(statsTask, muscleTask);

                var stats = statsTask.Result;
                DailyStats = stats;

                // FIX 4: Single pass computes all three summary totals instead of
                // three separate .Sum() calls over the same list.
                double totalVolume = 0;
                int totalSets = 0;
                foreach (var s in stats)
                {
                    totalVolume += s.TotalWeightLifted;
                    totalSets += s.SetsCompleted;
                }
                TotalVolumeLifted = totalVolume;
                TotalSets = totalSets;
                AverageVolume = stats.Count > 0 ? totalVolume / stats.Count : 0;

                // Heatmap
                UpdateHeatmapForMonth();

                // Sparklines — computed from a single ordered pass.
                var orderedStats = stats.OrderBy(s => s.Date).ToList();
                VolumeSparkline = orderedStats.Select(s => s.TotalWeightLifted).ToList();
                SetsSparkline = orderedStats.Select(s => (double)s.SetsCompleted).ToList();
                DaysSparkline = orderedStats.Select((_, i) => (double)(i + 1)).ToList();
                AvgVolumeSparkline = VolumeSparkline; // same data, reuse the list

                // Muscle group progress
                var muscleProgress = muscleTask.Result;
                MuscleGroupProgress = new ObservableCollection<MuscleGroupProgress>(muscleProgress);

                // FIX 1: Extract top 5 exercises directly from MuscleGroupProgress
                // instead of calling GetStrengthProgressAsync + GetExerciseProgressAsync
                // (which triggered additional DB queries). All exercise progress data
                // is already inside MuscleGroupProgress from the single bulk fetch.
                var topFive = muscleProgress
                    .SelectMany(mg => mg.Exercises)
                    .Where(e => e.MaxWeight > 0)
                    .OrderByDescending(e => e.MaxWeight)
                    .Take(5)
                    .ToList();
                TopExercises = new ObservableCollection<ExerciseProgress>(topFive);

                // Dynamic insight and best week are pure CPU — no awaits needed.
                GenerateInsight();
                CalculateBestWeek();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Error", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
        #endregion

        #region "GENERATE INSIGHT"
        private void GenerateInsight()
        {
            if (DailyStats.Count == 0)
            {
                InsightEmoji = "🚀";
                InsightMessage = "Start logging workouts to see your personalized insights here!";
                return;
            }

            var insights = new List<(string emoji, string message)>();
            var today = DateTime.Today;

            // FIX 5: Build one ordered snapshot — avoids re-sorting inside each branch.
            var orderedStats = DailyStats.OrderByDescending(s => s.Date).ToList();

            // Days since last workout
            var lastWorkout = orderedStats.FirstOrDefault();
            if (lastWorkout is not null)
            {
                var daysSince = (today - lastWorkout.Date).Days;
                if (daysSince == 0)
                    insights.Add(("🔥", "You already trained today — great work!"));
                else if (daysSince == 1)
                    insights.Add(("💪", "You trained yesterday. Keep the momentum going!"));
                else if (daysSince >= 3)
                    insights.Add(("⚡", $"It's been {daysSince} days since your last workout. Time to get back at it!"));
            }

            // Most vs least trained muscle group
            if (MuscleGroupProgress.Count > 0)
            {
                var top = MuscleGroupProgress.OrderByDescending(m => m.Exercises.Count).FirstOrDefault();
                var least = MuscleGroupProgress.OrderBy(m => m.Exercises.Count).FirstOrDefault();
                if (top is not null)
                    insights.Add(("🏋️", $"Your most trained muscle group is {top.MuscleGroup} — keep pushing!"));
                if (least is not null && least.MuscleGroup != top?.MuscleGroup)
                    insights.Add(("📊", $"You haven't trained {least.MuscleGroup} much this period — consider adding it!"));
            }

            // Volume trend — compare most recent 3 sessions to previous 3
            if (orderedStats.Count >= 6)
            {
                var recent = orderedStats.Take(3).Average(s => s.TotalWeightLifted);
                var older = orderedStats.Skip(3).Take(3).Average(s => s.TotalWeightLifted);
                if (older > 0)
                {
                    var change = ((recent - older) / older) * 100;
                    if (change > 10)
                        insights.Add(("📈", $"Your volume is up {change:F0}% compared to earlier this period — great progress!"));
                    else if (change < -10)
                        insights.Add(("📉", $"Your volume is down {Math.Abs(change):F0}% — try to push a bit harder!"));
                }
            }

            // Trending exercise
            var trending = TopExercises.FirstOrDefault(e => e.IsTrending);
            if (trending is not null)
                insights.Add(("🏆", $"You're making progress on {trending.ExerciseName} — keep it up!"));

            // Consistency
            if (DailyStats.Count >= 7)
                insights.Add(("🔥", $"You've trained {DailyStats.Count} days this period — consistency is key!"));

            (InsightEmoji, InsightMessage) = insights.Count > 0
                ? insights[_rng.Next(insights.Count)]
                : ("💡", "Consistency matters more than perfection. Every workout counts!");
        }
        #endregion

        #region "CALCULATE BEST WEEK"
        private void CalculateBestWeek()
        {
            if (DailyStats.Count == 0)
            {
                BestWeekLabel = string.Empty;
                BestWeekMuscleGroups = string.Empty;
                return;
            }

            var bestWeek = DailyStats
                .GroupBy(s => s.Date.AddDays(-(int)s.Date.DayOfWeek).Date)
                .Select(g => new
                {
                    WeekStart = g.Key,
                    Volume = g.Sum(s => s.TotalWeightLifted),
                    Workouts = g.Count()
                })
                .OrderByDescending(w => w.Volume)
                .FirstOrDefault();

            if (bestWeek is null) return;

            BestWeekVolume = bestWeek.Volume;
            BestWeekLabel = $"Week of {bestWeek.WeekStart:MMM d} — " +
                             $"{bestWeek.Workouts} {(bestWeek.Workouts == 1 ? "workout" : "workouts")}";

            // FIX 6: Pre-build a HashSet of exercise IDs that have sets in the best
            // week window. Replaces the O(m×e×s) triple-nested LINQ query that walked
            // MuscleGroupProgress → Exercises → Sets for every muscle group.
            var weekEnd = bestWeek.WeekStart.AddDays(7);
            var activeExerciseIds = MuscleGroupProgress
                .SelectMany(m => m.Exercises)
                .Where(e => e.Sets.Any(s =>
                    s.CreatedDate >= bestWeek.WeekStart && s.CreatedDate < weekEnd))
                .Select(e => e.ExerciseId)
                .ToHashSet();

            var muscleGroups = MuscleGroupProgress
                .Where(m => m.Exercises.Any(e => activeExerciseIds.Contains(e.ExerciseId)))
                .Select(m => m.MuscleGroup)
                .ToList();

            BestWeekMuscleGroups = muscleGroups.Count > 0
                ? string.Join(" · ", muscleGroups)
                : string.Empty;
        }
        #endregion

        #region "SELECT MUSCLE GROUP PROGRESS"
        [RelayCommand]
        private static async Task SelectMuscleGroupProgress(MuscleGroupProgress group)
        {
            await Shell.Current.GoToAsync(Routes.MuscleGroupProgress, new Dictionary<string, object>
            {
                { "MuscleGroup", group.MuscleGroup }
            });
        }
        #endregion
    }
}
