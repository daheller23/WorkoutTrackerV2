using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using SkiaSharp;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.ViewModels
{
    [QueryProperty(nameof(Exercise), "Exercise")]
    public partial class ExerciseProgressViewModel : BaseViewModel
    {
        #region "OBSERVABLE PROPERTIES"
        [ObservableProperty] private ExerciseProgress? _exercise;
        [ObservableProperty] private LineChart? _chart;
        #endregion

        #region "ON EXERCISE CHANGED"
        partial void OnExerciseChanged(ExerciseProgress? value)
        {
            if (value is null)
            {
                return;
            }
            BuildChart(value);
        }
        #endregion

        #region "GO BACK"
        [RelayCommand]
        private static Task GoBack() => Shell.Current.GoToAsync(Routes.Back);
        #endregion

        #region "BUILD CHART"
        private void BuildChart(ExerciseProgress exercise)
        {
            if (exercise.Points.Count == 0)
            {
                return;
            }

            var color = SKColor.Parse("#4CAF50");
            var entries = exercise.Points.Select(p => new ChartEntry((float)p.MaxWeight)
            {
                Label = p.Date.ToString("MMM d"),
                ValueLabel = p.MaxWeight.ToString("F0"),
                Color = color,
                TextColor = SKColor.Parse("#999999"),
                ValueLabelColor = color
            }).ToList();

            Chart = new LineChart
            {
                Entries = entries,
                LineMode = LineMode.Spline,
                LineSize = 4,
                PointMode = PointMode.Circle,
                PointSize = 12,
                BackgroundColor = SKColor.Parse("#FFFFFF"),
                LabelTextSize = 28,
                ValueLabelTextSize = 32,
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                ShowYAxisLines = true,
                ShowYAxisText = true,
                YAxisTextPaint = new SKPaint { Color = SKColor.Parse("#999999"), TextSize = 24 },
                YAxisLinesPaint = new SKPaint { Color = SKColor.Parse("#F0F0F0"), StrokeWidth = 1 }
            };
        }
        #endregion
    }
}