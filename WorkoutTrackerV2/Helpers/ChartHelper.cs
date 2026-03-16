using Microcharts;
using SkiaSharp;
using WorkoutTrackerV2.Models;

namespace WorkoutTrackerV2.Helpers
{
    public static class ChartHelper
    {
        public static LineChart BuildProgressChart(List<ProgressPoint> points)
        {
            if (points.Count == 0) return new LineChart();

            // Determine trend color
            bool isTrending = points.Last().MaxWeight >= points.First().MaxWeight;
            var lineColor = isTrending ? SKColor.Parse("#4CAF50") : SKColor.Parse("#FF6B6B");
            var fillColor = lineColor.WithAlpha(30);

            // Find best session
            double bestWeight = points.Max(p => p.MaxWeight);

            var entries = points.Select(p =>
            {
                bool isBest = p.MaxWeight == bestWeight;
                return new ChartEntry((float)p.MaxWeight)
                {
                    Label = p.Date.ToString("MMM d"),
                    ValueLabel = isBest ? $"🏆 {p.MaxWeight:F0}" : p.MaxWeight.ToString("F0"),
                    Color = isBest ? SKColor.Parse("#FFD700") : lineColor,
                    TextColor = SKColor.Parse("#999999"),
                    ValueLabelColor = isBest ? SKColor.Parse("#FFD700") : lineColor
                };
            }).ToList();

            // Build trend line entries (linear regression)
            var trendEntries = BuildTrendLine(points, lineColor);

            return new LineChart
            {
                Entries = entries,
                LineMode = LineMode.Spline,
                LineSize = 4,
                PointMode = PointMode.Circle,
                PointSize = 12,
                BackgroundColor = SKColor.Parse("#FFFFFF"),
                LabelTextSize = 28,
                ValueLabelTextSize = 28,
                LabelOrientation = Orientation.Horizontal,
                ValueLabelOrientation = Orientation.Horizontal,
                ShowYAxisLines = true,
                ShowYAxisText = true,
                YAxisTextPaint = new SKPaint { Color = SKColor.Parse("#999999"), TextSize = 24 },
                YAxisLinesPaint = new SKPaint { Color = SKColor.Parse("#F0F0F0"), StrokeWidth = 1 },
                LineAreaAlpha = 30
            };
        }

        private static List<ChartEntry> BuildTrendLine(List<ProgressPoint> points, SKColor color)
        {
            if (points.Count < 2) return [];

            // Linear regression
            int n = points.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumY += points[i].MaxWeight;
                sumXY += i * points[i].MaxWeight;
                sumX2 += i * i;
            }

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;

            return points.Select((p, i) => new ChartEntry((float)(slope * i + intercept))
            {
                Color = color.WithAlpha(80),
                TextColor = SKColors.Transparent,
                ValueLabelColor = SKColors.Transparent
            }).ToList();
        }
    }
}