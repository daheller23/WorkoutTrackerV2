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
                typeof(SparklineView), "#FFFFFF", propertyChanged: OnColorChanged);

        public static readonly BindableProperty BackgroundColorHexProperty =
            BindableProperty.Create(nameof(BackgroundColorHex), typeof(string),
                typeof(SparklineView), "#1F77F0", propertyChanged: OnColorChanged);

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

        // FIX 1+2+6: SKPaint objects and parsed SKColors cached as fields.
        // OnPaintSurface is called on every frame — allocating native Skia
        // resources and parsing hex strings inside it leaks memory and wastes
        // CPU. Paints are recreated only when LineColor or BackgroundColorHex
        // actually change (OnColorChanged), not on every draw.
        private SKColor _lineColor = SKColor.Parse("#FFFFFF");
        private SKColor _bgColor = SKColor.Parse("#1F77F0");
        private SKPaint _fillPaint = null!;
        private SKPaint _linePaint = null!;
        private SKPaint _dotPaint = null!;
        private SKPaint _flatPaint = null!;

        public SparklineView()
        {
            RebuildPaints();
        }

        // FIX 1: Separate changed handler for colour properties — only rebuilds
        // paints when colour actually changes, not on every data update.
        private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SparklineView view)
            {
                view._lineColor = SKColor.Parse(view.LineColor);
                view._bgColor = SKColor.Parse(view.BackgroundColorHex);
                view.RebuildPaints();
                view.InvalidateSurface();
            }
        }

        private static void OnDataChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SparklineView view)
                view.InvalidateSurface();
        }

        // FIX 1: Dispose old paints before creating new ones to release native
        // Skia resources, then construct fresh instances from current colours.
        private void RebuildPaints()
        {
            _fillPaint?.Dispose();
            _linePaint?.Dispose();
            _dotPaint?.Dispose();
            _flatPaint?.Dispose();

            _fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = _lineColor.WithAlpha(50),
                IsAntialias = true
            };
            _linePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = _lineColor.WithAlpha(220),
                StrokeWidth = 3f,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round
            };
            _dotPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = _lineColor,
                IsAntialias = true
            };
            _flatPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = _lineColor.WithAlpha(200),
                StrokeWidth = 3f,
                IsAntialias = true
            };
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);
            var canvas = e.Surface.Canvas;
            canvas.Clear(_bgColor);

            var data = Data;
            if (data is null || data.Count < 2) return;

            float width = e.Info.Width;
            float height = e.Info.Height;
            float padding = 4f;

            // FIX 4: Single loop finds min and max instead of two separate
            // data.Min() and data.Max() passes over the list.
            double min = data[0], max = data[0];
            foreach (var v in data)
            {
                if (v < min) min = v;
                if (v > max) max = v;
            }
            double range = max - min;

            if (range == 0)
            {
                float midY = height / 2f;
                canvas.DrawLine(padding, midY, width - padding, midY, _flatPaint);
                return;
            }

            // FIX 3: SKPath wrapped in using blocks so native path data is
            // released after each paint call instead of leaking on every frame.
            using var path = new SKPath();
            using var fillPath = new SKPath();

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

            canvas.DrawPath(fillPath, _fillPaint);
            canvas.DrawPath(path, _linePaint);

            // FIX 5: data[^1] index instead of data[data.Count - 1].
            float lastX = padding + (data.Count - 1) * stepX;
            float lastNorm = (float)((data[^1] - min) / range);
            float lastY = height - padding - lastNorm * (height - padding * 2);
            canvas.DrawCircle(lastX, lastY, 6f, _dotPaint);
        }

        // FIX 1: Dispose all cached paints when the control is destroyed to
        // ensure native Skia resources are released.
        protected override void OnHandlerChanging(HandlerChangingEventArgs e)
        {
            base.OnHandlerChanging(e);
            if (e.NewHandler is null)
            {
                _fillPaint?.Dispose();
                _linePaint?.Dispose();
                _dotPaint?.Dispose();
                _flatPaint?.Dispose();
            }
        }
    }
}
