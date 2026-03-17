using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WorkoutTrackerV2.Models;
using WorkoutTrackerV2.Services;

namespace WorkoutTrackerV2.ViewModels
{
    public partial class AnalyticsViewModel(IWorkoutService workoutService, IAnalyticsService analyticsService) : BaseViewModel
    {
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
        #endregion

        #region "PARTIAL METHODS"
        partial void OnSelectedDaysChanged(int value) => LoadAnalyticsCommand.Execute(null);
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
            if (int.TryParse(days, out int result))
                SelectedDays = result;
        }
        #endregion

        #region "HEATMAP NAVIGATION"
        [RelayCommand]
        private void PreviousMonth()
        {
            HeatmapMonth = HeatmapMonth.AddMonths(-1);
        }

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
        private async Task LoadAnalytics()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;

                var statsTask = analyticsService.GetDailyStatsAsync(SelectedDays);
                var strengthTask = analyticsService.GetStrengthProgressAsync(SelectedDays);
                var muscleTask = analyticsService.GetMuscleGroupProgressAsync(SelectedDays);
                var exercisesTask = workoutService.GetAllExercisesAsync();

                await Task.WhenAll(statsTask, strengthTask, muscleTask, exercisesTask);

                // Summary stats
                var stats = statsTask.Result;
                DailyStats = stats;
                TotalVolumeLifted = stats.Sum(s => s.TotalWeightLifted);
                AverageVolume = stats.Count > 0 ? TotalVolumeLifted / stats.Count : 0;
                TotalSets = stats.Sum(s => s.SetsCompleted);

                // Heatmap
                UpdateHeatmapForMonth();

                // Sparklines
                var orderedStats = stats.OrderBy(s => s.Date).ToList();
                VolumeSparkline = orderedStats.Select(s => s.TotalWeightLifted).ToList();
                SetsSparkline = orderedStats.Select(s => (double)s.SetsCompleted).ToList();
                DaysSparkline = orderedStats.Select((s, i) => (double)(i + 1)).ToList();
                AvgVolumeSparkline = orderedStats.Select(s => s.TotalWeightLifted).ToList();

                // Top exercises
                var exercises = exercisesTask.Result;
                var topFive = strengthTask.Result.Take(5).ToList();
                var progressTasks = topFive
                    .Select(kvp => exercises.FirstOrDefault(e => e.Name == kvp.Key))
                    .Where(e => e is not null)
                    .Select(e => analyticsService.GetExerciseProgressAsync(e!.Id, SelectedDays))
                    .ToList();

                var topExercises = await Task.WhenAll(progressTasks);
                TopExercises = new ObservableCollection<ExerciseProgress>(topExercises);

                // Muscle group progress
                MuscleGroupProgress = new ObservableCollection<MuscleGroupProgress>(muscleTask.Result);

                // Dynamic insight
                GenerateInsight();

                // Best week
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

            // Days since last workout
            var lastWorkout = DailyStats.OrderByDescending(s => s.Date).FirstOrDefault();
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

            // Volume trend
            if (DailyStats.Count >= 6)
            {
                var ordered = DailyStats.OrderByDescending(s => s.Date).ToList();
                var recent = ordered.Take(3).Average(s => s.TotalWeightLifted);
                var older = ordered.Skip(3).Take(3).Average(s => s.TotalWeightLifted);
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
            if (TopExercises.Count > 0)
            {
                var trending = TopExercises.FirstOrDefault(e => e.IsTrending);
                if (trending is not null)
                    insights.Add(("🏆", $"You're making progress on {trending.ExerciseName} — keep it up!"));
            }

            // Consistency
            if (DailyStats.Count >= 7)
                insights.Add(("🔥", $"You've trained {DailyStats.Count} days this period — consistency is key!"));

            if (insights.Count > 0)
            {
                var pick = insights[new Random().Next(insights.Count)];
                InsightEmoji = pick.emoji;
                InsightMessage = pick.message;
            }
            else
            {
                InsightEmoji = "💡";
                InsightMessage = "Consistency matters more than perfection. Every workout counts!";
            }
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

            if (bestWeek is not null)
            {
                BestWeekVolume = bestWeek.Volume;
                BestWeekLabel = $"Week of {bestWeek.WeekStart:MMM d} — {bestWeek.Workouts} {(bestWeek.Workouts == 1 ? "workout" : "workouts")}";

                // Find muscle groups trained that week
                var weekEnd = bestWeek.WeekStart.AddDays(7);
                var muscleGroups = MuscleGroupProgress
                    .Where(m => m.Exercises.Any(e => e.Sets
                        .Any(s => s.CreatedDate >= bestWeek.WeekStart && s.CreatedDate < weekEnd)))
                    .Select(m => m.MuscleGroup)
                    .ToList();

                BestWeekMuscleGroups = muscleGroups.Count > 0
                    ? string.Join(" · ", muscleGroups)
                    : string.Empty;
            }
        }
        #endregion

        #region "SELECT MUSCLE GROUP PROGRESS"
        [RelayCommand]
        private async Task SelectMuscleGroupProgress(MuscleGroupProgress group)
        {
            await Shell.Current.GoToAsync(Routes.MuscleGroupProgress, new Dictionary<string, object>
            {
                { "MuscleGroup", group.MuscleGroup }
            });
        }
        #endregion
    }
}