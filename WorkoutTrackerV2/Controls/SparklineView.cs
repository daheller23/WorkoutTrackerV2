using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace WorkoutTrackerV2.Controls
{
    public class SparklineView : SKCanvasView
    {
        public static readonly BindableProperty DataProperty =
            BindableProperty.Create(nameof(Data), typeof(List<double>),
                typeof(SparklineView), null, propertyChanged: OnDataChanged);

        public static readonly BindableProperty LineColorProperty =
            BindableProperty.Create(nameof(LineColor), typeof(string),
                typeof(SparklineView), "#FFFFFF", propertyChanged: OnDataChanged);

        public static readonly BindableProperty BackgroundColorHexProperty =
            BindableProperty.Create(nameof(BackgroundColorHex), typeof(string),
                typeof(SparklineView), "#1F77F0", propertyChanged: OnDataChanged);

        public List<double> Data
        {
            get => (List<double>)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public string LineColor
        {
            get => (string)GetValue(LineColorProperty);
            set => SetValue(LineColorProperty, value);
        }

        public string BackgroundColorHex
        {
            get => (string)GetValue(BackgroundColorHexProperty);
            set => SetValue(BackgroundColorHexProperty, value);
        }

        private static void OnDataChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SparklineView view)
                view.InvalidateSurface();
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);
            var canvas = e.Surface.Canvas;

            // Clear with the card background color instead of transparent
            canvas.Clear(SKColor.Parse(BackgroundColorHex));

            var data = Data;
            if (data is null || data.Count < 2) return;

            float width = e.Info.Width;
            float height = e.Info.Height;
            float padding = 4f;

            double min = data.Min();
            double max = data.Max();
            double range = max - min;

            if (range == 0)
            {
                float midY = height / 2f;
                using var flatPaint = new SKPaint
                {
                    Style = SKPaintStyle.Stroke,
                    Color = SKColor.Parse(LineColor).WithAlpha(200),
                    StrokeWidth = 3f,
                    IsAntialias = true
                };
                canvas.DrawLine(padding, midY, width - padding, midY, flatPaint);
                return;
            }

            var path = new SKPath();
            var fillPath = new SKPath();
            float stepX = (width - padding * 2) / (data.Count - 1);

            for (int i = 0; i < data.Count; i++)
            {
                float x = padding + i * stepX;
                float normalised = (float)((data[i] - min) / range);
                float y = height - padding - normalised * (height - padding * 2);

                if (i == 0)
                {
                    path.MoveTo(x, y);
                    fillPath.MoveTo(x, height);
                    fillPath.LineTo(x, y);
                }
                else
                {
                    path.LineTo(x, y);
                    fillPath.LineTo(x, y);
                }
            }

            fillPath.LineTo(padding + (data.Count - 1) * stepX, height);
            fillPath.Close();

            var color = SKColor.Parse(LineColor);

            using var fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = color.WithAlpha(50),
                IsAntialias = true
            };
            canvas.DrawPath(fillPath, fillPaint);

            using var linePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = color.WithAlpha(220),
                StrokeWidth = 3f,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };
            canvas.DrawPath(path, linePaint);

            float lastX = padding + (data.Count - 1) * stepX;
            float lastNorm = (float)((data[data.Count - 1] - min) / range);
            float lastY = height - padding - lastNorm * (height - padding * 2);

            using var dotPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = color,
                IsAntialias = true
            };
            canvas.DrawCircle(lastX, lastY, 6f, dotPaint);
        }
    }
}