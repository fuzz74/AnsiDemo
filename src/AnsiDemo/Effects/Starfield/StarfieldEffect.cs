using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Starfield;

/// <summary>
/// Stars flying towards the viewer: (x, y, z) points projected in perspective from the buffer centre, grey when far
/// and white when near, near ones drawn as streaks. A star that leaves the buffer or reaches the near plane respawns
/// at the far plane. Sparse: only star pixels are written.
/// </summary>
[Effect("starfield", PixelMode.Braille)]
public sealed class StarfieldEffect : IPixelEffect
{
    private const int MinStars = 200;
    private const int PixelsPerStar = 64;
    private const double Far = 1.0;
    private const double Near = 0.04;
    private const double SpawnSpread = 0.6;          // stars spawn over this central fraction of the buffer
    private const double BaseSpeed = 0.42;           // depth units per second
    private const double SpeedWobble = 0.25;         // relative speed change over time
    private const double SpeedWobbleRate = 0.8;      // radians per second
    private const double TailDepth = 0.025;          // depth the streak reaches back
    private const double TailFade = 0.45;            // tail brightness relative to the head
    private const double DoubleHeadNearness = 0.5;   // from here on a star without streak is drawn 2 pixels wide
    private const int BrailleMinGrey = 160;          // far stars stay above most dither thresholds
    private const int HalfBlockMinGrey = 80;

    private readonly Rgb[] _palette = new Rgb[256];
    private DemoRandom _rng = new(1);
    private double[] _x = [];
    private double[] _y = [];
    private double[] _z = [];
    private int _width;
    private int _height;
    private double _cx;
    private double _cy;
    private double _maxStreak;
    private double _time;

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _rng = rng;
        _time = ctx.Time;
        _width = ctx.PixelWidth;
        _height = ctx.PixelHeight;
        _cx = _width / 2.0;
        _cy = _height / 2.0;
        _maxStreak = Math.Max(4.0, _width / 16.0);

        int minGrey = ctx.Mode == PixelMode.Braille ? BrailleMinGrey : HalfBlockMinGrey;
        for (int i = 0; i < _palette.Length; i++)
        {
            byte grey = (byte)(minGrey + (255 - minGrey) * i / 255);
            _palette[i] = new Rgb(grey, grey, grey);
        }

        int count = Math.Max(MinStars, _width * _height / PixelsPerStar);
        _x = new double[count];
        _y = new double[count];
        _z = new double[count];
        for (int i = 0; i < count; i++)
        {
            Place(i, SteadyDepth(_rng.NextDouble()));
        }
    }

    public void Update(FrameContext ctx)
    {
        _time = ctx.Time;
        double dz = BaseSpeed * (1.0 + SpeedWobble * Math.Sin(_time * SpeedWobbleRate)) * ctx.DeltaTime;
        for (int i = 0; i < _z.Length; i++)
        {
            double z = _z[i] - dz;
            _z[i] = z;
            if (z <= Near || !Inside(_cx + _x[i] / z, _cy + _y[i] / z))
            {
                Place(i, Far);
            }
        }
    }

    public void Render(PixelBuffer pixels)
    {
        if (pixels.Width != _width || pixels.Height != _height)
        {
            return;
        }

        Rgb[] target = pixels.Pixels;
        for (int i = 0; i < _z.Length; i++)
        {
            double z = _z[i];
            double headX = _cx + _x[i] / z;
            double headY = _cy + _y[i] / z;
            double tailZ = Math.Min(z + TailDepth, Far);
            double tailX = _cx + _x[i] / tailZ;
            double tailY = _cy + _y[i] / tailZ;

            double length = Math.Max(Math.Abs(headX - tailX), Math.Abs(headY - tailY));
            if (length > _maxStreak)
            {
                double keep = _maxStreak / length;
                tailX = headX - (headX - tailX) * keep;
                tailY = headY - (headY - tailY) * keep;
                length = _maxStreak;
            }

            double nearness = Math.Clamp((Far - z) / (Far - Near), 0.0, 1.0);
            int level = (int)(nearness * 255.0);

            int steps = (int)length;
            for (int s = 0; s < steps; s++)
            {
                double f = (double)s / steps;
                int tailLevel = (int)(level * (TailFade + (1.0 - TailFade) * f));
                Plot(target, tailX + (headX - tailX) * f, tailY + (headY - tailY) * f, _palette[tailLevel]);
            }

            Rgb head = _palette[level];
            Plot(target, headX, headY, head);
            if (steps == 0 && nearness >= DoubleHeadNearness)
            {
                Plot(target, headX + 1.0, headY, head);
            }
        }
    }

    /// <summary>
    /// Depth for quantile u of the stars in flight: stars spawn evenly over time at the far plane, so the depth
    /// density is flat down to SpawnSpread and falls off as (z / SpawnSpread)² below it, where stars start to leave
    /// the buffer. Sampling it in Reset makes the first frame look like any later one.
    /// </summary>
    private static double SteadyDepth(double u)
    {
        const double s = SpawnSpread;
        double below = (s * s * s - Near * Near * Near) / (3.0 * s * s);
        double v = u * (below + Far - s);
        return v < below ? Math.Cbrt(Near * Near * Near + 3.0 * s * s * v) : s + (v - below);
    }

    /// <summary>
    /// Puts star i at depth z on a random point of the spawn area that is still inside the buffer at that depth; at
    /// the far plane that is the central SpawnSpread part of the buffer.
    /// </summary>
    private void Place(int i, double z)
    {
        double spread = Math.Min(SpawnSpread, z);
        _x[i] = (2.0 * _rng.NextDouble() - 1.0) * _cx * spread;
        _y[i] = (2.0 * _rng.NextDouble() - 1.0) * _cy * spread;
        _z[i] = z;
    }

    private bool Inside(double x, double y) => x >= 0.0 && x < _width && y >= 0.0 && y < _height;

    private void Plot(Rgb[] target, double x, double y, Rgb color)
    {
        if (Inside(x, y))
        {
            target[(int)y * _width + (int)x] = color;
        }
    }
}
