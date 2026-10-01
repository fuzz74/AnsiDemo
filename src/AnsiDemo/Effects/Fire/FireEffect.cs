using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Fire;

/// <summary>
/// The classic heat-buffer fire: every tick random heat is fed in below the bottom edge and every cell takes the
/// average of the cells below it minus a cooling term, so flames rise, flicker and cool to black before the top.
/// The simulation runs at most <see cref="MaxSimHeight"/> rows high and is scaled up bilinearly.
/// </summary>
[Effect("fire", PixelMode.HalfBlock)]
public sealed class FireEffect : IPixelEffect
{
    private const double TickSeconds = 1.0 / 30;
    private const double TickEpsilon = 1e-9;
    private const int MaxCatchUpTicks = 30;
    private const int MaxSimHeight = 60;
    private const int SeedRows = 2;

    // Heat is fixed point: palette index · Unit.
    private const int Unit = 256;
    private const int MaxHeat = 255 * Unit;

    // No heat survives above this fraction of the height, whatever the seeds do.
    private const double ReachLimit = 0.85;

    // Seed columns flip between hot and cold; hot about 3/4 of the time, so the flames reach about half the height.
    private const double HotToCold = 0.15;
    private const double ColdToHot = 0.45;
    private const int HotSpread = 40 * Unit;
    private const int ColdMax = 60 * Unit;

    private const int JitterMask = 4095;

    private readonly Rgb[] _palette = BuildPalette();
    private DemoRandom _rng = new(1);
    private int _width;
    private int _height;
    private int _simWidth;
    private int _simHeight;
    private int[] _heat = [];
    private bool[] _hot = [];
    private int[] _jitter = [];
    private int _cooling;
    private double _pending;

    // Bilinear scale-up maps from pixel coordinates to simulation cells, weights in 0..Unit.
    private int[] _colA = [];
    private int[] _colB = [];
    private int[] _colWeight = [];
    private int[] _rowA = [];
    private int[] _rowB = [];
    private int[] _rowWeight = [];

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _rng = rng;
        _width = ctx.PixelWidth;
        _height = ctx.PixelHeight;
        int scale = Math.Max(1, (_height + MaxSimHeight - 1) / MaxSimHeight);
        _simWidth = Math.Max(1, (_width + scale - 1) / scale);
        _simHeight = Math.Max(1, (_height + scale - 1) / scale);

        _heat = new int[_simWidth * (_simHeight + SeedRows)];
        _hot = new bool[_simWidth];
        _cooling = (int)Math.Ceiling(MaxHeat / (ReachLimit * _simHeight));
        _jitter = new int[JitterMask + 1];
        for (int i = 0; i < _jitter.Length; i++)
        {
            _jitter[i] = rng.Next(_cooling / 2 + 1);
        }

        (_colA, _colB, _colWeight) = ScaleMap(_width, _simWidth, scale);
        (_rowA, _rowB, _rowWeight) = ScaleMap(_height, _simHeight, scale);

        for (int x = 0; x < _simWidth; x++)
        {
            _hot[x] = rng.NextDouble() < ColdToHot / (HotToCold + ColdToHot);
        }
        // Run until the heat has risen through the whole buffer twice, so the first frame is already burning.
        for (int i = 0; i < 2 * (_simHeight + SeedRows); i++)
        {
            Tick();
        }
        _pending = 0;
    }

    public void Update(FrameContext ctx)
    {
        _pending = Math.Min(_pending + ctx.DeltaTime, MaxCatchUpTicks * TickSeconds);
        while (_pending >= TickSeconds - TickEpsilon)
        {
            Tick();
            _pending -= TickSeconds;
        }
    }

    public void Render(PixelBuffer pixels)
    {
        int width = Math.Min(pixels.Width, _width);
        int height = Math.Min(pixels.Height, _height);
        Rgb[] dst = pixels.Pixels;
        int[] heat = _heat;
        for (int y = 0; y < height; y++)
        {
            int rowA = _rowA[y] * _simWidth;
            int rowB = _rowB[y] * _simWidth;
            int wy = _rowWeight[y];
            int dstRow = y * pixels.Width;
            for (int x = 0; x < width; x++)
            {
                int xa = _colA[x];
                int xb = _colB[x];
                int wx = _colWeight[x];
                int top = (heat[rowA + xa] * (Unit - wx) + heat[rowA + xb] * wx) >> 8;
                int bottom = (heat[rowB + xa] * (Unit - wx) + heat[rowB + xb] * wx) >> 8;
                dst[dstRow + x] = _palette[(top * (Unit - wy) + bottom * wy) >> 16];
            }
        }
    }

    /// <summary>Feeds new heat into the seed rows, then lets every visible row rise by one from the rows below it.</summary>
    private void Tick()
    {
        int w = _simWidth;
        int[] heat = _heat;
        int seed = _simHeight * w;
        for (int x = 0; x < w; x++)
        {
            bool hot = _hot[x];
            if (_rng.NextDouble() < (hot ? HotToCold : ColdToHot))
            {
                hot = !hot;
                _hot[x] = hot;
            }
            int value = hot ? MaxHeat - _rng.Next(HotSpread) : _rng.Next(ColdMax);
            heat[seed + x] = value;
            heat[seed + w + x] = value;
        }

        // Top-down in place: row y only reads rows y + 1 and y + 2, which this tick has not touched yet.
        int j = _rng.Next(JitterMask + 1);
        for (int y = 0; y < _simHeight; y++)
        {
            int row = y * w;
            int below = row + w;
            int below2 = below + w;
            for (int x = 0; x < w; x++)
            {
                int left = x > 0 ? x - 1 : x;
                int right = x < w - 1 ? x + 1 : x;
                int sum = heat[below + left] + heat[below + x] + heat[below + right] + heat[below2 + x];
                int value = (sum >> 2) - _cooling - _jitter[j++ & JitterMask];
                heat[row + x] = value > 0 ? value : 0;
            }
        }
    }

    private static (int[] A, int[] B, int[] Weight) ScaleMap(int size, int simSize, int scale)
    {
        var a = new int[size];
        var b = new int[size];
        var weight = new int[size];
        for (int i = 0; i < size; i++)
        {
            double f = Math.Clamp((i + 0.5) / scale - 0.5, 0, simSize - 1);
            int lo = (int)f;
            a[i] = lo;
            b[i] = Math.Min(lo + 1, simSize - 1);
            weight[i] = (int)Math.Round((f - lo) * Unit);
        }
        return (a, b, weight);
    }

    /// <summary>Black → red → yellow → white over heat 0..255.</summary>
    private static Rgb[] BuildPalette()
    {
        var palette = new Rgb[256];
        for (int i = 0; i < palette.Length; i++)
        {
            double t = 3.0 * i / 255;
            palette[i] = new Rgb(Channel(t), Channel(t - 1), Channel(t - 2));
        }
        return palette;
    }

    private static byte Channel(double v) => (byte)Math.Round(255 * Math.Clamp(v, 0, 1));
}
