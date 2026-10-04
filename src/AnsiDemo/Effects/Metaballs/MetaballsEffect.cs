using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Metaballs;

/// <summary>
/// Glowing blobs on Lissajous paths through the centre: the field Σ r²/d² is evaluated on a half-resolution grid,
/// interpolated per pixel and mapped to black below a threshold, a tinted glow, then a bright near-white core.
/// Reset and Update evaluate the grid; Render only reads it.
/// </summary>
[Effect("metaballs", PixelMode.Braille)]
public sealed class MetaballsEffect : IPixelEffect
{
    private const int BallCount = 6;

    // Field levels: black below GlowStart, tinted glow up to CoreStart, then the core whitens up to CoreFull.
    private const double GlowStart = 0.6;
    private const double CoreStart = 1.0;
    private const double CoreFull = 2.2;
    private const double CoreMinWhite = 0.45;
    private const double CoreMaxWhite = 0.9;
    private const double HueDriftDegreesPerSecond = 24.0;

    private readonly double[] _radiusSquared = new double[BallCount];
    private readonly double[] _amplitudeX = new double[BallCount];
    private readonly double[] _amplitudeY = new double[BallCount];
    private readonly double[] _speedX = new double[BallCount];
    private readonly double[] _speedY = new double[BallCount];
    private readonly double[] _phaseX = new double[BallCount];
    private readonly double[] _phaseY = new double[BallCount];
    private readonly double[] _hue = new double[BallCount];

    // Per-frame ball state, in units of the buffer height relative to its centre, and tint colours (0..1).
    private readonly double[] _ballX = new double[BallCount];
    private readonly double[] _ballY = new double[BallCount];
    private readonly double[] _tintR = new double[BallCount];
    private readonly double[] _tintG = new double[BallCount];
    private readonly double[] _tintB = new double[BallCount];

    private DemoRandom _rng = new(1);
    private double _time;
    private int _width;
    private int _height;

    // Half-resolution grid: sample (i, j) sits at pixel (2i, 2j); one extra column and row for interpolation.
    private int _gridWidth;
    private int _gridHeight;
    private double[] _field = [];
    private double[] _gridR = [];
    private double[] _gridG = [];
    private double[] _gridB = [];

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _rng = rng;
        _width = ctx.PixelWidth;
        _height = ctx.PixelHeight;
        _gridWidth = (_width >> 1) + 2;
        _gridHeight = (_height >> 1) + 2;
        int samples = _gridWidth * _gridHeight;
        _field = new double[samples];
        _gridR = new double[samples];
        _gridG = new double[samples];
        _gridB = new double[samples];

        double halfAspect = 0.5 * _width / _height;
        for (int b = 0; b < BallCount; b++)
        {
            double radius = 0.06 + 0.035 * _rng.NextDouble();
            _radiusSquared[b] = radius * radius;
            _amplitudeX[b] = (halfAspect - 1.5 * radius) * (0.6 + 0.4 * _rng.NextDouble());
            _amplitudeY[b] = (0.5 - 1.5 * radius) * (0.6 + 0.4 * _rng.NextDouble());
            _speedX[b] = 0.35 + 0.65 * _rng.NextDouble();
            _speedY[b] = 0.35 + 0.65 * _rng.NextDouble();
            // Evenly spread horizontal phases keep the balls apart most of the time; they still cross near the centre.
            _phaseX[b] = 2 * Math.PI * (b + 0.5 * _rng.NextDouble()) / BallCount;
            _phaseY[b] = 2 * Math.PI * _rng.NextDouble();
            _hue[b] = 360.0 * b / BallCount + 40.0 * _rng.NextDouble();
        }

        _time = ctx.Time;
        PlaceBalls();
        EvaluateGrid();
    }

    public void Update(FrameContext ctx)
    {
        _time = ctx.Time;
        PlaceBalls();
        EvaluateGrid();
    }

    public void Render(PixelBuffer pixels)
    {
        if (pixels.Width != _width || pixels.Height != _height)
        {
            return;
        }

        Rgb[] output = pixels.Pixels;
        int gridWidth = _gridWidth;
        double[] field = _field;
        for (int y = 0; y < _height; y++)
        {
            int row = (y >> 1) * gridWidth;
            double fy = (y & 1) * 0.5;
            int rowOut = y * _width;
            for (int x = 0; x < _width; x++)
            {
                int g = row + (x >> 1);
                double fx = (x & 1) * 0.5;
                double top = field[g] + (field[g + 1] - field[g]) * fx;
                double bottom = field[g + gridWidth] + (field[g + gridWidth + 1] - field[g + gridWidth]) * fx;
                double f = top + (bottom - top) * fy;
                output[rowOut + x] = Shade(f, _gridR[g], _gridG[g], _gridB[g]);
            }
        }
    }

    private static Rgb Shade(double f, double r, double g, double b)
    {
        if (f < GlowStart)
        {
            return Rgb.Black;
        }
        if (f < CoreStart)
        {
            double gain = 255.0 * (f - GlowStart) / (CoreStart - GlowStart);
            return new Rgb((byte)(r * gain), (byte)(g * gain), (byte)(b * gain));
        }

        double s = Math.Min(1.0, (f - CoreStart) / (CoreFull - CoreStart));
        double white = CoreMinWhite + (CoreMaxWhite - CoreMinWhite) * s;
        double tint = 255.0 * (1.0 - white);
        double add = 255.0 * white;
        return new Rgb((byte)(r * tint + add), (byte)(g * tint + add), (byte)(b * tint + add));
    }

    private void PlaceBalls()
    {
        double hueShift = HueDriftDegreesPerSecond * _time;
        for (int b = 0; b < BallCount; b++)
        {
            _ballX[b] = _amplitudeX[b] * Math.Sin(_speedX[b] * _time + _phaseX[b]);
            _ballY[b] = _amplitudeY[b] * Math.Sin(_speedY[b] * _time + _phaseY[b]);
            Rgb tint = Rgb.FromHsv(_hue[b] + hueShift, 0.85, 1.0);
            _tintR[b] = tint.R / 255.0;
            _tintG[b] = tint.G / 255.0;
            _tintB[b] = tint.B / 255.0;
        }
    }

    // Field and field-weighted tint at every grid sample; coordinates are square pixels scaled by 1 / height.
    private void EvaluateGrid()
    {
        double scale = 2.0 / _height;
        double originX = -0.5 * _width / _height;
        for (int j = 0; j < _gridHeight; j++)
        {
            double v = j * scale - 0.5;
            int row = j * _gridWidth;
            for (int i = 0; i < _gridWidth; i++)
            {
                double u = originX + i * scale;
                double sum = 0, r = 0, g = 0, b = 0;
                for (int k = 0; k < BallCount; k++)
                {
                    double dx = u - _ballX[k];
                    double dy = v - _ballY[k];
                    double c = _radiusSquared[k] / (dx * dx + dy * dy + 1e-6);
                    sum += c;
                    r += c * _tintR[k];
                    g += c * _tintG[k];
                    b += c * _tintB[k];
                }
                double inv = 1.0 / sum;
                _field[row + i] = sum;
                _gridR[row + i] = r * inv;
                _gridG[row + i] = g * inv;
                _gridB[row + i] = b * inv;
            }
        }
    }
}
