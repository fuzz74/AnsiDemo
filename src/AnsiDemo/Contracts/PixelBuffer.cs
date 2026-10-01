namespace AnsiDemo.Contracts;

public sealed class PixelBuffer
{
    public PixelBuffer(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException($"Pixel buffer size must be positive, got {width}x{height}.");
        }
        Width = width;
        Height = height;
        Pixels = new Rgb[width * height];
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>Row-major, index = y * Width + x.</summary>
    public Rgb[] Pixels { get; }

    /// <summary>Outside the buffer: get returns Black, set is ignored.</summary>
    public Rgb this[int x, int y]
    {
        get => Contains(x, y) ? Pixels[y * Width + x] : Rgb.Black;
        set
        {
            if (Contains(x, y))
            {
                Pixels[y * Width + x] = value;
            }
        }
    }

    public void Clear(Rgb color) => Array.Fill(Pixels, color);

    /// <summary>HalfBlock (cols, 2 * rows); Braille (2 * cols, 4 * rows).</summary>
    public static (int Width, int Height) SizeFor(PixelMode mode, int cols, int rows) => mode switch
    {
        PixelMode.HalfBlock => (cols, 2 * rows),
        PixelMode.Braille => (2 * cols, 4 * rows),
        _ => throw new ArgumentException($"Unknown pixel mode {mode}.", nameof(mode)),
    };

    private bool Contains(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;
}
