using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace WorkoutTrackerV2.Controls
{
    public class WorkoutHeatmapView : SKCanvasView
    {
        public static readonly BindableProperty HeatmapDataProperty =
            BindableProperty.Create(nameof(HeatmapData), typeof(Dictionary<DateTime, double>),
                typeof(WorkoutHeatmapView), null, propertyChanged: OnDataChanged);

        public static readonly BindableProperty MonthProperty =
            BindableProperty.Create(nameof(Month), typeof(DateTime),
                typeof(WorkoutHeatmapView), DateTime.Today, propertyChanged: OnDataChanged);

        public Dictionary<DateTime, double> HeatmapData
        {
            get => (Dictionary<DateTime, double>)GetValue(HeatmapDataProperty);
            set => SetValue(HeatmapDataProperty, value);
        }

        public DateTime Month
        {
            get => (DateTime)GetValue(MonthProperty);
            set => SetValue(MonthProperty, value);
        }

        private static void OnDataChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is WorkoutHeatmapView view)
                view.InvalidateSurface();
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColor.Parse("#FAFAFA"));

            var data = HeatmapData;
            if (data is null) return;

            var month = new DateTime(Month.Year, Month.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
            int startDayOfWeek = (int)month.DayOfWeek; // 0 = Sunday

            float width = e.Info.Width;
            float height = e.Info.Height;

            // Layout constants
            float labelHeight = 40f;
            float padding = 8f;
            float availableWidth = width - padding * 2;
            float cellSize = (availableWidth - 6 * padding) / 7f;
            float cellSpacing = padding;

            // Day labels
            string[] dayLabels = { "S", "M", "T", "W", "T", "F", "S" };
            using var labelPaint = new SKPaint
            {
                Color = SKColor.Parse("#999999"),
                TextSize = 22f,
                IsAntialias = true,
                TextAlign = SKTextAlign.Center,
                Typeface = SKTypeface.FromFamilyName("Arial")
            };

            for (int d = 0; d < 7; d++)
            {
                float x = padding + d * (cellSize + cellSpacing) + cellSize / 2;
                canvas.DrawText(dayLabels[d], x, labelHeight - 8f, labelPaint);
            }

            // Find max volume for intensity scaling
            double maxVolume = data.Values.Count > 0 ? data.Values.Max() : 1;

            // Draw cells
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(month.Year, month.Month, day);
                int dayOfWeek = (int)date.DayOfWeek;
                int week = (day + startDayOfWeek - 1) / 7;

                float x = padding + dayOfWeek * (cellSize + cellSpacing);
                float y = labelHeight + week * (cellSize + cellSpacing);

                // Determine color
                SKColor cellColor;
                if (data.TryGetValue(date, out double volume) && volume > 0)
                {
                    float intensity = (float)(volume / maxVolume);
                    cellColor = InterpolateColor(intensity);
                }
                else if (date.Date == DateTime.Today)
                {
                    cellColor = SKColor.Parse("#E8F0FE");
                }
                else
                {
                    cellColor = SKColor.Parse("#F0F0F0");
                }

                using var cellPaint = new SKPaint
                {
                    Color = cellColor,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill
                };

                var rect = new SKRoundRect(
                    new SKRect(x, y, x + cellSize, y + cellSize), 6f, 6f);
                canvas.DrawRoundRect(rect, cellPaint);

                // Draw day number
                using var dayPaint = new SKPaint
                {
                    Color = volume > 0 ? SKColors.White : SKColor.Parse("#CCCCCC"),
                    TextSize = 20f,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center,
                    Typeface = SKTypeface.FromFamilyName("Arial")
                };

                float textY = y + cellSize / 2f + 7f;
                canvas.DrawText(day.ToString(), x + cellSize / 2f, textY, dayPaint);
            }

            // Calculate required height and request it
            int totalWeeks = (daysInMonth + startDayOfWeek + 6) / 7;
            float requiredHeight = labelHeight + totalWeeks * (cellSize + cellSpacing);
            float scale = e.Info.Width / (float)Width;
            if (Math.Abs(HeightRequest - requiredHeight / scale) > 1)
                HeightRequest = requiredHeight / scale;
        }

        private static SKColor InterpolateColor(float intensity)
        {
            // Light blue to deep blue
            var light = new SKColor(0xC8, 0xE6, 0xFF); // #C8E6FF
            var dark = new SKColor(0x1F, 0x77, 0xF0);  // #1F77F0

            byte r = (byte)(light.Red + (dark.Red - light.Red) * intensity);
            byte g = (byte)(light.Green + (dark.Green - light.Green) * intensity);
            byte b = (byte)(light.Blue + (dark.Blue - light.Blue) * intensity);

            return new SKColor(r, g, b);
        }
    }
}