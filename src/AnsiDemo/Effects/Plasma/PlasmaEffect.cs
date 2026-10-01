using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Plasma;

/// <summary>
/// Classic plasma: the sum of four sines (of x, of y, of x + y and of the distance to a moving centre) indexes a
/// cyclic 256-entry hue palette whose hue offset turns a full circle every 12 seconds.
/// </summary>
[Effect("plasma", PixelMode.HalfBlock)]
public sealed class PlasmaEffect : IPixelEffect
{
    private const int SineSize = 1024;
    private const int SineMask = SineSize - 1;
    private const int PaletteSize = 256;
    private const int PaletteMask = PaletteSize - 1;

    /// <summary>The buffer height spans this many plasma units, so blobs keep their size relative to the screen.</summary>
    private const double UnitsPerHeight = 6.0;

    /// <summary>Palette steps per unit of the summed value (range −4..4); the cyclic palette wraps once over that range.</summary>
    private const double PaletteStepsPerValue = 40.0;

    private const double HueDegreesPerSecond = 30.0;
    private const double Saturation = 0.85;

    private const double ColumnFrequency = 1.1, ColumnSpeed = 0.6;
    private const double RowFrequency = 1.3, RowSpeed = -0.45;
    private const double DiagonalFrequency = 0.8, DiagonalSpeed = 0.7;
    private const double RadialFrequency = 1.7, RadialSpeed = -1.1;

    private readonly double[] _sine = new double[SineSize];
    private readonly Rgb[] _palette = new Rgb[PaletteSize];
    private double[] _columnTerm = [];
    private double[] _rowTerm = [];
    private double[] _diagonalTerm = [];
    private double[] _dx2 = [];
    private double[] _dy2 = [];
    private int _width;
    private int _height;
    private double _unit;
    private double _time;
    private int _radialPhase;
    private double _columnOffset, _rowOffset, _diagonalOffset, _radialOffset;

    public PlasmaEffect()
    {
        for (int i = 0; i < SineSize; i++)
        {
            _sine[i] = Math.Sin(i * 2.0 * Math.PI / SineSize);
        }
    }

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _width = ctx.PixelWidth;
        _height = ctx.PixelHeight;
        _unit = UnitsPerHeight / _height;
        _columnTerm = new double[_width];
        _rowTerm = new double[_height];
        _diagonalTerm = new double[_width + _height - 1];
        _dx2 = new double[_width];
        _dy2 = new double[_height];

        // A runner resets with the same seed on resize, so these offsets stay put and the picture does not jump.
        _columnOffset = rng.NextDouble() * 2.0 * Math.PI;
        _rowOffset = rng.NextDouble() * 2.0 * Math.PI;
        _diagonalOffset = rng.NextDouble() * 2.0 * Math.PI;
        _radialOffset = rng.NextDouble() * 2.0 * Math.PI;

        _time = ctx.Time;
        Prepare();
    }

    public void Update(FrameContext ctx)
    {
        _time = ctx.Time;
        Prepare();
    }

    public void Render(PixelBuffer pixels)
    {
        Rgb[] target = pixels.Pixels;
        int width = Math.Min(pixels.Width, _width);
        int height = Math.Min(pixels.Height, _height);
        double radialScale = RadialFrequency * _unit * SineSize / (2.0 * Math.PI);
        for (int y = 0; y < height; y++)
        {
            double rowTerm = _rowTerm[y];
            double dy2 = _dy2[y];
            int row = y * pixels.Width;
            for (int x = 0; x < width; x++)
            {
                double radial = _sine[((int)(Math.Sqrt(_dx2[x] + dy2) * radialScale) + _radialPhase) & SineMask];
                double value = _columnTerm[x] + rowTerm + _diagonalTerm[x + y] + radial;
                target[row + x] = _palette[(int)((value + 4.0) * PaletteStepsPerValue) & PaletteMask];
            }
        }
    }

    /// <summary>Recomputes the per-column, per-row and per-diagonal terms and the palette for the current time.</summary>
    private void Prepare()
    {
        double t = _time;
        for (int x = 0; x < _width; x++)
        {
            _columnTerm[x] = Math.Sin(x * _unit * ColumnFrequency + t * ColumnSpeed + _columnOffset);
        }
        for (int y = 0; y < _height; y++)
        {
            _rowTerm[y] = Math.Sin(y * _unit * RowFrequency + t * RowSpeed + _rowOffset);
        }
        for (int d = 0; d < _diagonalTerm.Length; d++)
        {
            _diagonalTerm[d] = Math.Sin(d * _unit * DiagonalFrequency + t * DiagonalSpeed + _diagonalOffset);
        }

        // The centre of the rings wanders on a slow Lissajous path over the middle of the buffer.
        double centreX = _width * (0.5 + 0.3 * Math.Sin(t * 0.37));
        double centreY = _height * (0.5 + 0.3 * Math.Cos(t * 0.29));
        for (int x = 0; x < _width; x++)
        {
            double dx = x - centreX;
            _dx2[x] = dx * dx;
        }
        for (int y = 0; y < _height; y++)
        {
            double dy = y - centreY;
            _dy2[y] = dy * dy;
        }
        double radialPhase = (t * RadialSpeed + _radialOffset) * SineSize / (2.0 * Math.PI);
        _radialPhase = (int)Math.Floor(radialPhase % SineSize);

        // A cyclic hue wheel, with brightness rippling twice per cycle but never dropping below 0.75.
        double hueOffset = t * HueDegreesPerSecond;
        for (int i = 0; i < PaletteSize; i++)
        {
            double angle = i * 2.0 * Math.PI / PaletteSize;
            double value = 0.875 + 0.125 * Math.Sin(2.0 * angle);
            _palette[i] = Rgb.FromHsv(i * 360.0 / PaletteSize + hueOffset, Saturation, value);
        }
    }
}
