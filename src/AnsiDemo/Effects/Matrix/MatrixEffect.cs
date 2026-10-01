using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Matrix;

/// <summary>
/// Matrix rain: one drop per column falls at its own speed over a grid of random printable ASCII glyphs, lighting
/// a near-white head and a green tail that fades to black with the distance from the head. Glyphs are re-rolled
/// while the rain falls. Only the cells of a drop are written, so the layer below shows between the columns.
/// </summary>
[Effect("matrix", PixelMode.Braille)]
public sealed class MatrixEffect : ICellEffect
{
    // The simulation runs in fixed ticks of accumulated DeltaTime, so any frame rate gives the same rain.
    private const double TickSeconds = 1.0 / 60.0;

    // Guards the tick count against step sums like 0.0166666 for one tick.
    private const double TickEpsilon = 1e-9;

    private const double MinRowsPerSecond = 6.0, MaxRowsPerSecond = 25.0;
    private const int MinTail = 5;
    private const double MaxTailOfRows = 0.75;

    // Empty rows above the top before a respawned drop enters, as a fraction of the rows.
    private const double MaxGapOfRows = 1.0;

    // Glyph cells re-rolled per tick, per cell of the grid: every glyph changes about every 2 seconds.
    private const double RerollsPerCellPerTick = 1.0 / 120.0;

    private const char FirstGlyph = '!', LastGlyph = '~';
    private static readonly Rgb Head = new(210, 255, 210);

    private DemoRandom _rng = new(1);
    private char[] _glyphs = [];
    private double[] _head = [];
    private double[] _speed = [];
    private int[] _tail = [];
    private int _cols;
    private int _rows;
    private int _rerollsPerTick;
    private double _pending;

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _rng = rng;
        _cols = ctx.Cols;
        _rows = ctx.Rows;
        _glyphs = new char[_cols * _rows];
        _head = new double[_cols];
        _speed = new double[_cols];
        _tail = new int[_cols];
        _rerollsPerTick = Math.Max(1, (int)Math.Round(_glyphs.Length * RerollsPerCellPerTick));
        _pending = 0;

        for (int i = 0; i < _glyphs.Length; i++)
        {
            _glyphs[i] = RandomGlyph();
        }

        // Drops start anywhere from a little above the top to where their tail is about to leave the bottom,
        // so the very first frame already shows rain.
        for (int x = 0; x < _cols; x++)
        {
            Respawn(x);
            _head[x] = -MaxGapOfRows * _rows * _rng.NextDouble() + (_rows + _tail[x]) * _rng.NextDouble();
        }
    }

    public void Update(FrameContext ctx)
    {
        _pending += ctx.DeltaTime;
        while (_pending >= TickSeconds - TickEpsilon)
        {
            _pending -= TickSeconds;
            Tick();
        }
    }

    public void Render(Frame frame)
    {
        for (int x = 0; x < _cols; x++)
        {
            int head = (int)Math.Floor(_head[x]);
            int tail = _tail[x];
            int first = Math.Max(0, head - tail);
            int last = Math.Min(_rows - 1, head);
            for (int y = first; y <= last; y++)
            {
                int distance = head - y;
                Rgb fg = distance == 0 ? Head : TailColor(distance, tail);
                frame[x, y] = new Cell(_glyphs[y * _cols + x], fg, Rgb.Black);
            }
        }
    }

    private void Tick()
    {
        for (int x = 0; x < _cols; x++)
        {
            int before = (int)Math.Floor(_head[x]);
            _head[x] += _speed[x] * TickSeconds;
            int after = (int)Math.Floor(_head[x]);

            // The head writes a fresh glyph into every row it enters.
            for (int y = Math.Max(0, before + 1); y <= after && y < _rows; y++)
            {
                _glyphs[y * _cols + x] = RandomGlyph();
            }

            if (after - _tail[x] >= _rows)
            {
                Respawn(x);
            }
        }

        for (int i = 0; i < _rerollsPerTick; i++)
        {
            _glyphs[_rng.Next(_glyphs.Length)] = RandomGlyph();
        }
    }

    /// <summary>New random speed and tail for column x, with the head above the top row.</summary>
    private void Respawn(int x)
    {
        int maxTail = Math.Max(MinTail + 1, (int)(_rows * MaxTailOfRows));
        _speed[x] = MinRowsPerSecond + (MaxRowsPerSecond - MinRowsPerSecond) * _rng.NextDouble();
        _tail[x] = MinTail + _rng.Next(maxTail - MinTail + 1);
        _head[x] = -1.0 - MaxGapOfRows * _rows * _rng.NextDouble();
    }

    private char RandomGlyph() => (char)(FirstGlyph + _rng.Next(LastGlyph - FirstGlyph + 1));

    /// <summary>Bright green next to the head, fading toward black at the end of the tail.</summary>
    private static Rgb TailColor(int distance, int tail)
    {
        double k = 1.0 - (double)distance / (tail + 1);
        double glow = k * k;
        double tint = glow * glow;
        return new Rgb((byte)(60 * tint), (byte)(255 * glow), (byte)(80 * tint));
    }
}
