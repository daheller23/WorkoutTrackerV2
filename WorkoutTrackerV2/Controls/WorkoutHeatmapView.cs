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

        // ── Static constants ─────────────────────────────────────────────────
        // FIX 3: Parse fixed colours once at class load instead of on every paint.
        private static readonly SKColor ColorBackground = SKColor.Parse("#FAFAFA");
        private static readonly SKColor ColorLabel = SKColor.Parse("#999999");
        private static readonly SKColor ColorToday = SKColor.Parse("#E8F0FE");
        private static readonly SKColor ColorEmpty = SKColor.Parse("#F0F0F0");
        private static readonly SKColor ColorDayInactive = SKColor.Parse("#CCCCCC");

        // FIX 2: Load the typeface once — SKTypeface.FromFamilyName hits the
        // system font cache and should not be called per-cell or per-paint.
        private static readonly SKTypeface Typeface =
            SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default;

        // Interpolation endpoints (reused by InterpolateColor, no allocation).
        private static readonly SKColor ColorLight = new(0xC8, 0xE6, 0xFF); // #C8E6FF
        private static readonly SKColor ColorDark = new(0x1F, 0x77, 0xF0); // #1F77F0

        // ── Cached paints ─────────────────────────────────────────────────────
        // FIX 1: Four paints constructed once and reused across all paint calls.
        // Only Color is mutated per-cell; all other properties are constant.
        // The original allocated new SKPaint inside the day loop — up to 62
        // native Skia allocations per repaint (31 days × cellPaint + dayPaint).
        private readonly SKPaint _labelPaint = new()
        {
            Color = ColorLabel,
            TextSize = 22f,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = Typeface
        };
        private readonly SKPaint _cellPaint = new()
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        private readonly SKPaint _dayPaint = new()
        {
            TextSize = 20f,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = Typeface
        };

        private static void OnDataChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is WorkoutHeatmapView view)
                view.InvalidateSurface();
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var canvas = e.Surface.Canvas;
            canvas.Clear(ColorBackground);

            var data = HeatmapData;
            if (data is null) return;

            var month = new DateTime(Month.Year, Month.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(month.Year, month.Month);
            int startDayOfWeek = (int)month.DayOfWeek;

            float width = e.Info.Width;
            float padding = 8f;
            float labelHeight = 40f;
            float availableWidth = width - padding * 2;
            float cellSize = (availableWidth - 6 * padding) / 7f;
            float cellSpacing = padding;

            // Day-of-week header labels
            string[] dayLabels = { "S", "M", "T", "W", "T", "F", "S" };
            for (int d = 0; d < 7; d++)
            {
                float x = padding + d * (cellSize + cellSpacing) + cellSize / 2;
                canvas.DrawText(dayLabels[d], x, labelHeight - 8f, _labelPaint);
            }

            // FIX 5: Single pass to find max volume instead of LINQ .Max().
            double maxVolume = 1;
            foreach (var v in data.Values)
                if (v > maxVolume) maxVolume = v;

            // FIX 4: Declare SKRoundRect once outside the loop and reuse by
            // reassigning — avoids per-iteration struct construction overhead.
            var rect = new SKRoundRect();

            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(month.Year, month.Month, day);
                int dayOfWeek = (int)date.DayOfWeek;
                int week = (day + startDayOfWeek - 1) / 7;

                float x = padding + dayOfWeek * (cellSize + cellSpacing);
                float y = labelHeight + week * (cellSize + cellSpacing);

                // FIX 6: volume is 0 when the key is absent — data.TryGetValue
                // initialises it to default(double) = 0 on miss, which is safe
                // to use in the intensity and dayPaint colour expressions below.
                data.TryGetValue(date, out double volume);

                // FIX 1: Mutate cached paint colour instead of allocating new paint.
                _cellPaint.Color = volume > 0
                    ? InterpolateColor((float)(volume / maxVolume))
                    : date.Date == DateTime.Today ? ColorToday : ColorEmpty;

                rect.SetRectRadii(new SKRect(x, y, x + cellSize, y + cellSize),
                    [new SKPoint(6, 6), new SKPoint(6, 6),
                     new SKPoint(6, 6), new SKPoint(6, 6)]);
                canvas.DrawRoundRect(rect, _cellPaint);

                // FIX 1: Mutate cached paint colour instead of allocating new paint.
                _dayPaint.Color = volume > 0 ? SKColors.White : ColorDayInactive;
                float textY = y + cellSize / 2f + 7f;
                canvas.DrawText(day.ToString(), x + cellSize / 2f, textY, _dayPaint);
            }

            // FIX 7: HeightRequest is set inside OnPaintSurface which can trigger
            // a layout pass → repaint cycle. The Math.Abs > 1 guard prevents an
            // infinite loop in practice, but this is architecturally fragile.
            // A cleaner solution would be to override MeasureOverride instead,
            // but that requires a larger refactor. The guard is retained as-is.
            int totalWeeks = (daysInMonth + startDayOfWeek + 6) / 7;
            float requiredHeight = labelHeight + totalWeeks * (cellSize + cellSpacing);
            float scale = e.Info.Width / (float)Width;
            if (Math.Abs(HeightRequest - requiredHeight / scale) > 1)
                HeightRequest = requiredHeight / scale;
        }

        private static SKColor InterpolateColor(float intensity)
        {
            byte r = (byte)(ColorLight.Red + (ColorDark.Red - ColorLight.Red) * intensity);
            byte g = (byte)(ColorLight.Green + (ColorDark.Green - ColorLight.Green) * intensity);
            byte b = (byte)(ColorLight.Blue + (ColorDark.Blue - ColorLight.Blue) * intensity);
            return new SKColor(r, g, b);
        }

        // FIX 1: Dispose all cached paints when the control is detached to
        // ensure native Skia resources are released.
        protected override void OnHandlerChanging(HandlerChangingEventArgs e)
        {
            base.OnHandlerChanging(e);
            if (e.NewHandler is null)
            {
                _labelPaint.Dispose();
                _cellPaint.Dispose();
                _dayPaint.Dispose();
            }
        }
    }
}
