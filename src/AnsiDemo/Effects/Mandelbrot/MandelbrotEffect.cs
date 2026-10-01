using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Mandelbrot;

/// <summary>
/// A continuous zoom into seahorse valley with smooth colour bands outside the set and black inside. The zoom
/// ping-pongs between 1 and 2^17: deeper, the pixels around this point need hundreds and then (beyond 2^24) over
/// 1000 iterations to escape, so within the iteration budget the picture would turn black long before double
/// precision runs out at about 2^40.
/// </summary>
[Effect("mandelbrot", PixelMode.HalfBlock)]
public sealed class MandelbrotEffect : IPixelEffect
{
    /// <summary>Zoom speed in powers of two per second: 2^12 at 20 s.</summary>
    public const double ZoomOctavesPerSecond = 0.6;

    /// <summary>Deepest zoom in powers of two before the zoom turns back out.</summary>
    public const double MaxZoomOctaves = 17.0;

    // The boundary point zoomed into, and the centre of the first frame, from which the view drifts to the target.
    private const double TargetX = -0.743643887037151, TargetY = 0.131825904205330;
    private const double StartX = -0.65, StartY = 0.0;

    // The region shown whole at zoom 1, fitted into the buffer keeping pixels square.
    private const double StartWidth = 2.9, StartHeight = 2.4;

    private const int MinIterations = 64, MaxIterations = 288;
    private const double IterationsPerOctave = (MaxIterations - MinIterations) / MaxZoomOctaves;

    private const double BailoutSquared = 256.0 * 256.0;
    private const int PaletteSize = 512;
    // Bands follow sqrt(mu): a cycle spans about 4 iterations far outside and about 30 near the deep boundary.
    private const double PaletteCyclesPerSqrtIteration = 1.0;
    private const double PaletteCyclesPerSecond = 0.15;

    // Upper bound on computed samples per frame; larger buffers are computed in blocks and scaled up.
    private const int MaxSamples = 12_000;

    private Rgb[] _palette = [];
    private int _width, _height, _blockWidth, _blockHeight;
    private double _time;

    /// <summary>The zoom factor at scene time t: 2^(0.6·t), ping-ponging between 1 and 2^17.</summary>
    public static double ZoomFactor(double time) => Math.Pow(2.0, ZoomOctaves(time));

    private static double ZoomOctaves(double time)
    {
        double phase = (ZoomOctavesPerSecond * Math.Max(time, 0.0)) % (2.0 * MaxZoomOctaves);
        return phase <= MaxZoomOctaves ? phase : 2.0 * MaxZoomOctaves - phase;
    }

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _width = ctx.PixelWidth;
        _height = ctx.PixelHeight;
        _time = ctx.Time;
        _palette = BuildPalette();

        _blockWidth = 1;
        _blockHeight = 1;
        while (CeilDiv(_width, _blockWidth) * CeilDiv(_height, _blockHeight) > MaxSamples)
        {
            if (_blockWidth <= _blockHeight)
            {
                _blockWidth++;
            }
            else
            {
                _blockHeight++;
            }
        }
    }

    public void Update(FrameContext ctx) => _time = ctx.Time;

    public void Render(PixelBuffer pixels)
    {
        if (pixels.Width != _width || pixels.Height != _height)
        {
            return;
        }

        double octaves = ZoomOctaves(_time);
        double zoom = Math.Pow(2.0, octaves);
        double pixelSize = Math.Max(StartWidth / _width, StartHeight / _height) / zoom;
        // The offset from the target shrinks with zoom², so the target moves to the centre while the view narrows.
        double drift = 1.0 / (zoom * zoom);
        double centerX = TargetX + (StartX - TargetX) * drift;
        double centerY = TargetY + (StartY - TargetY) * drift;
        int maxIterations = (int)(MinIterations + IterationsPerOctave * octaves);
        double paletteScale = PaletteSize * PaletteCyclesPerSqrtIteration;
        double paletteOffset = PaletteSize * PaletteCyclesPerSecond * _time;

        Rgb[] output = pixels.Pixels;
        for (int by = 0; by < _height; by += _blockHeight)
        {
            double cy = centerY - (by + 0.5 * _blockHeight - 0.5 * _height) * pixelSize;
            int yEnd = Math.Min(by + _blockHeight, _height);
            for (int bx = 0; bx < _width; bx += _blockWidth)
            {
                double cx = centerX + (bx + 0.5 * _blockWidth - 0.5 * _width) * pixelSize;
                double mu = Escape(cx, cy, maxIterations);
                Rgb color = Rgb.Black;
                if (mu >= 0)
                {
                    int index = (int)(Math.Sqrt(mu) * paletteScale + paletteOffset) % PaletteSize;
                    color = _palette[index];
                }

                int xEnd = Math.Min(bx + _blockWidth, _width);
                for (int y = by; y < yEnd; y++)
                {
                    int row = y * _width;
                    for (int x = bx; x < xEnd; x++)
                    {
                        output[row + x] = color;
                    }
                }
            }
        }
    }

    /// <summary>The continuous iteration count n + 1 − log2(log|z|) shifted to be ≥ 0, or −1 inside the set.</summary>
    private static double Escape(double cx, double cy, int maxIterations)
    {
        // Closed-form tests for the main cardioid and the period-2 bulb.
        double xq = cx - 0.25;
        double q = xq * xq + cy * cy;
        if (q * (q + xq) <= 0.25 * cy * cy || (cx + 1.0) * (cx + 1.0) + cy * cy <= 0.0625)
        {
            return -1.0;
        }

        double x = 0.0, y = 0.0, x2 = 0.0, y2 = 0.0;
        for (int n = 0; n < maxIterations; n++)
        {
            y = 2.0 * x * y + cy;
            x = x2 - y2 + cx;
            x2 = x * x;
            y2 = y * y;
            if (x2 + y2 > BailoutSquared)
            {
                // log|z| ≥ log 256 ≈ 5.55, so log2(log|z|) ≥ 2.47 and n + 4 − log2(log|z|) stays positive.
                double logModulus = 0.5 * Math.Log(x2 + y2);
                return n + 4.0 - Math.Log2(logModulus);
            }
        }
        return -1.0;
    }

    /// <summary>A cyclic gradient, smoothed with a cosine ease between stops; its darkest stop is still not black.</summary>
    private static Rgb[] BuildPalette()
    {
        ReadOnlySpan<double> positions = [0.0, 0.18, 0.40, 0.62, 0.82];
        Rgb[] stops =
        [
            new(10, 14, 90),
            new(32, 110, 210),
            new(240, 250, 255),
            new(255, 170, 20),
            new(110, 20, 60),
        ];

        var palette = new Rgb[PaletteSize];
        for (int i = 0; i < PaletteSize; i++)
        {
            double p = (double)i / PaletteSize;
            int s = positions.Length - 1;
            while (positions[s] > p)
            {
                s--;
            }
            int next = (s + 1) % positions.Length;
            double end = next == 0 ? 1.0 : positions[next];
            double t = (p - positions[s]) / (end - positions[s]);
            double eased = 0.5 - 0.5 * Math.Cos(Math.PI * t);
            palette[i] = Rgb.Lerp(stops[s], stops[next], eased);
        }
        return palette;
    }

    private static int CeilDiv(int value, int divisor) => (value + divisor - 1) / divisor;
}
