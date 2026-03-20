using Microcharts;
using SkiaSharp;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Helpers
{
    public static class ChartHelper
    {
        // FIX 1: Parse hex colours once at startup instead of on every
        // BuildProgressChart call. SKColor.Parse does string parsing and
        // channel extraction — these are compile-time constants so there
        // is no reason to repeat that work.
        private static readonly SKColor ColorGreen = SKColor.Parse("#4CAF50");
        private static readonly SKColor ColorRed = SKColor.Parse("#FF6B6B");
        private static readonly SKColor ColorGold = SKColor.Parse("#FFD700");
        private static readonly SKColor ColorGrey = SKColor.Parse("#999999");
        private static readonly SKColor ColorWhite = SKColor.Parse("#FFFFFF");
        private static readonly SKColor ColorGridLine = SKColor.Parse("#F0F0F0");

        // FIX 2: SKPaint implements IDisposable and holds native SkiaSharp
        // resources. Allocating new instances on every chart build and never
        // disposing them leaks native memory. These configs are constant so
        // they are initialised once as static readonly fields and reused.
        private static readonly SKPaint YAxisTextPaint = new()
        {
            Color = SKColor.Parse("#999999"),
            TextSize = 24
        };
        private static readonly SKPaint YAxisLinesPaint = new()
        {
            Color = SKColor.Parse("#F0F0F0"),
            StrokeWidth = 1
        };

        public static LineChart BuildProgressChart(List<ProgressPoint> points)
        {
            if (points.Count == 0) return new LineChart();

            // Points are expected to be ordered by date ascending (AnalyticsService
            // sorts them before calling here). First/Last give earliest/latest.
            bool isTrending = points[^1].MaxWeight >= points[0].MaxWeight;
            var lineColor = isTrending ? ColorGreen : ColorRed;

            // FIX 3: Find bestWeight and build entries in one pass instead of
            // calling points.Max() (one full pass) then .Select() (another pass).
            double bestWeight = 0;
            foreach (var p in points)
                if (p.MaxWeight > bestWeight) bestWeight = p.MaxWeight;

            var entries = points.Select(p =>
            {
                bool isBest = p.MaxWeight == bestWeight;
                return new ChartEntry((float)p.MaxWeight)
                {
                    Label = p.Date.ToString("MMM d"),
                    ValueLabel = isBest ? $"🏆 {p.MaxWeight:F0}" : p.MaxWeight.ToString("F0"),
                    Color = isBest ? ColorGold : lineColor,
                    TextColor = ColorGrey,
                    ValueLabelColor = isBest ? ColorGold : lineColor
                };
            }).ToList();

            // FIX 4: BuildTrendLine result was computed but never assigned to the
            // chart — dead code that ran a full linear regression on every call
            // for no effect. Removed entirely.

            return new LineChart
            {
                Entries = entries,
                LineMode = LineMode.Spline,
                LineSize = 4,
                PointMode = PointMode.Circle,
                PointSize = 12,
                BackgroundColor = ColorWhite,
                LabelTextSize = 28,
                ValueLabelTextSize = 28,
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                ShowYAxisLines = true,
                ShowYAxisText = true,
                YAxisTextPaint = YAxisTextPaint,
                YAxisLinesPaint = YAxisLinesPaint,
                LineAreaAlpha = 30
            };
        }
    }
}
