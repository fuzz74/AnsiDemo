using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Scroller;

/// <summary>
/// Classic sine scroller: <see cref="ScrollerText.Value"/> moves right to left at a constant speed, each character
/// riding a travelling sine wave inside the middle third of the rows, coloured by its index in the text. Spaces are
/// skipped, so the layer below shows between the letters; under a letter its background is kept, darkened.
/// </summary>
[Effect("scroller", PixelMode.HalfBlock)]
public sealed class ScrollerEffect : ICellEffect
{
    private const double ColumnsPerSecond = 24.0;
    private const double WaveLengthColumns = 56.0;
    private const double WaveRadiansPerSecond = 2.6;
    private const double HueStepDegrees = 9.0;
    private const double BackgroundKeep = 0.35;

    // Guards the floor of the scroll offset against step sums like 0.99999999 for one second.
    private const double OffsetEpsilon = 1e-9;

    private Rgb[] _colors = [];
    private int _cols;
    private int _top;
    private int _bottom;
    private double _centre;
    private double _amplitude;
    private double _time;

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        string text = ScrollerText.Value;
        if (_colors.Length != text.Length)
        {
            _colors = new Rgb[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                _colors[i] = Rgb.FromHsv(i * HueStepDegrees, 1.0, 1.0);
            }
        }

        // Middle third of the rows: rows/3 .. 2*rows/3 - 1; the wave fills it from the centre to both edges.
        _cols = ctx.Cols;
        _top = ctx.Rows / 3;
        _bottom = Math.Max(_top, 2 * ctx.Rows / 3 - 1);
        _centre = (_top + _bottom) / 2.0;
        _amplitude = (_bottom - _top) / 2.0;
        _time = ctx.Time;
    }

    public void Update(FrameContext ctx) => _time = ctx.Time;

    public void Render(Frame frame)
    {
        string text = ScrollerText.Value;
        int length = text.Length;
        int offset = (int)(Math.Floor(ColumnsPerSecond * _time + OffsetEpsilon) % length);
        double k = 2.0 * Math.PI / WaveLengthColumns;
        double phase = _time * WaveRadiansPerSecond;

        for (int x = 0; x < _cols; x++)
        {
            int index = (x + offset) % length;
            char ch = text[index];
            if (ch == ' ')
            {
                continue;
            }

            int y = (int)Math.Floor(_centre + _amplitude * Math.Sin(x * k + phase) + 0.5);
            y = Math.Clamp(y, _top, _bottom);
            Rgb bg = Rgb.Lerp(Rgb.Black, frame[x, y].Bg, BackgroundKeep);
            frame[x, y] = new Cell(ch, _colors[index], bg);
        }
    }
}
