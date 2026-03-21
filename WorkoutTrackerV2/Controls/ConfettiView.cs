using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace WorkoutTrackerV2.Controls
{
    /// <summary>
    /// Polished confetti animation designed to rain down over the page content
    /// without blocking it. Particles are elongated ribbons, rounded squares,
    /// and circles — closer to real confetti than plain rectangles.
    /// Call Start() to begin. Raises Completed after the duration ends.
    /// </summary>
    public class ConfettiView : SKCanvasView
    {
        private const int DurationMs = 2800;
        private const int FadeStartMs = 1800;
        private const int ParticleCount = 90;
        private const int TickMs = 16;

        // Gold-weighted palette — primary colour matches the PR trophy badge.
        private static readonly SKColor[] Colors =
        [
            SKColor.Parse("#FFD700"),
            SKColor.Parse("#FFC107"),
            SKColor.Parse("#FFD700"),  // extra gold weight
            SKColor.Parse("#1F77F0"),
            SKColor.Parse("#64B5F6"),
            SKColor.Parse("#FFFFFF"),
            SKColor.Parse("#4CAF50"),
            SKColor.Parse("#FFD700"),
        ];

        private readonly List<Particle> _particles = [];
        private IDispatcherTimer? _timer;
        private int _elapsed;
        private static readonly Random Rng = new();

        public bool IsAnimating { get; private set; }
        public event EventHandler? Completed;

        public void Start()
        {
            Stop();
            _particles.Clear();
            _elapsed = 0;

            float w = (float)(Width > 0 ? Width : 400);

            for (int i = 0; i < ParticleCount; i++)
                _particles.Add(Particle.Create(w, Rng));

            IsAnimating = true;
            _timer = Application.Current!.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(TickMs);
            _timer.Tick += OnTick;
            _timer.Start();
        }

        public void Stop()
        {
            if (_timer is not null)
            {
                _timer.Stop();
                _timer.Tick -= OnTick;
                _timer = null;
            }
            IsAnimating = false;
            InvalidateSurface();
        }

        private void OnTick(object? sender, EventArgs e)
        {
            _elapsed += TickMs;
            foreach (var p in _particles) p.Update();
            InvalidateSurface();
            if (_elapsed >= DurationMs)
            {
                Stop();
                Completed?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            if (!IsAnimating) return;

            float fade = _elapsed > FadeStartMs
                ? 1f - (float)(_elapsed - FadeStartMs) / (DurationMs - FadeStartMs)
                : 1f;
            fade = Math.Clamp(fade, 0f, 1f);

            using var paint = new SKPaint { IsAntialias = true };

            foreach (var p in _particles)
            {
                paint.Color = p.Color.WithAlpha((byte)(255 * fade * p.Alpha));
                canvas.Save();
                canvas.Translate(p.X, p.Y);
                canvas.RotateDegrees(p.Rotation);

                if (p.IsRibbon)
                    canvas.DrawRoundRect(-p.W / 2f, -p.H / 2f, p.W, p.H, 2f, 2f, paint);
                else if (p.IsCircle)
                    canvas.DrawCircle(0, 0, p.W / 2f, paint);
                else
                    canvas.DrawRoundRect(-p.W / 2f, -p.W / 2f, p.W, p.W, 3f, 3f, paint);

                canvas.Restore();
            }
        }

        private sealed class Particle
        {
            public float X, Y, Vx, Vy;
            public float Rotation, RotationSpeed;
            public float W, H, Alpha;
            public SKColor Color;
            public bool IsRibbon, IsCircle;

            private float _wobble, _wobbleSpeed, _wobbleAmp;

            public static Particle Create(float screenW, Random rng)
            {
                int type = rng.Next(3);
                bool ribbon = type == 0;
                bool circle = type == 2;

                float w = ribbon
                    ? (float)(rng.NextDouble() * 6 + 8)
                    : (float)(rng.NextDouble() * 7 + 5);
                float h = ribbon
                    ? w * (float)(rng.NextDouble() * 1.5 + 2.5)
                    : w;

                return new Particle
                {
                    X = (float)(rng.NextDouble() * screenW),
                    Y = (float)(rng.NextDouble() * -150),
                    Vx = (float)((rng.NextDouble() - 0.5) * 3),
                    Vy = (float)(rng.NextDouble() * 3 + 2.5),
                    Rotation = (float)(rng.NextDouble() * 360),
                    RotationSpeed = (float)((rng.NextDouble() - 0.5) * 7),
                    W = w,
                    H = h,
                    Alpha = (float)(rng.NextDouble() * 0.3 + 0.7),
                    Color = Colors[rng.Next(Colors.Length)],
                    IsRibbon = ribbon,
                    IsCircle = circle,
                    _wobble = (float)(rng.NextDouble() * MathF.PI * 2),
                    _wobbleSpeed = (float)(rng.NextDouble() * 0.12 + 0.04),
                    _wobbleAmp = (float)(rng.NextDouble() * 1.2 + 0.3)
                };
            }

            public void Update()
            {
                Vy += 0.12f;
                Vy *= 0.994f;
                Vx *= 0.994f;
                _wobble += _wobbleSpeed;
                X += Vx + MathF.Sin(_wobble) * _wobbleAmp;
                Y += Vy;
                Rotation += RotationSpeed;
            }
        }

        protected override void OnHandlerChanging(HandlerChangingEventArgs e)
        {
            base.OnHandlerChanging(e);
            if (e.NewHandler is null) Stop();
        }
    }
}
