using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Tunnel;

/// <summary>
/// A textured tunnel seen from inside: every pixel looks up a procedural tile texture at
/// (angle + rotation, depth / distance + flight), shaded darker toward the vanishing point.
/// The lookup tables cover twice the buffer so the tunnel centre can sway without recomputing them.
/// </summary>
[Effect("tunnel", PixelMode.HalfBlock)]
public sealed class TunnelEffect : IPixelEffect
{
    private const int TextureBits = 6;
    private const int TextureSize = 1 << TextureBits;
    private const int TextureMask = TextureSize - 1;
    private const int TileBits = 3;

    private const int AngleRepeats = 2;            // texture copies around the circumference
    private const double DepthScale = 0.5;         // texture lengths at distance 1 (half the shorter screen side)
    private const double BlackRadius = 0.04;       // normalised distance of the fully black central disc
    private const double FullBrightRadius = 1.1;   // normalised distance from which the walls are fully lit
    private const double MinDistance = 0.01;

    private const double FlightSpeed = 1.5;        // texture lengths per second
    private const double RotationSpeed = 0.06;     // turns per second
    private const double RotationSwing = 0.08;     // turns of extra back-and-forth rotation
    private const double SwayX = 0.16;             // centre sway as a fraction of the width
    private const double SwayY = 0.14;             // centre sway as a fraction of the height

    private DemoRandom _rng = new(1);
    private Rgb[] _texture = [];
    private int[] _angle = [];
    private int[] _depth = [];
    private int[] _shade = [];
    private int _width;
    private int _height;
    private int _tableWidth;
    private double _time;

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _rng = rng;
        _time = ctx.Time;
        _width = ctx.PixelWidth;
        _height = ctx.PixelHeight;
        BuildTexture(_rng.NextDouble() * 360.0);
        BuildTables(ctx);
    }

    public void Update(FrameContext ctx) => _time = ctx.Time;

    public void Render(PixelBuffer pixels)
    {
        double t = _time;
        double turns = RotationSpeed * t + RotationSwing * Math.Sin(0.45 * t);
        int angleShift = (int)Math.Floor(turns * AngleRepeats * TextureSize);
        int depthShift = (int)Math.Floor(FlightSpeed * TextureSize * t);

        // Screen pixel (x, y) with the tunnel centre at (cx, cy) reads table entry (x + offsetX, y + offsetY).
        int offsetX = Math.Clamp((int)Math.Round(_width * (0.5 - SwayX * Math.Sin(0.55 * t))), 0, _width);
        int offsetY = Math.Clamp((int)Math.Round(_height * (0.5 - SwayY * Math.Sin(0.8 * t + 1.0))), 0, _height);

        int width = Math.Min(_width, pixels.Width);
        int height = Math.Min(_height, pixels.Height);
        Rgb[] output = pixels.Pixels;
        Rgb[] texture = _texture;
        int[] angle = _angle;
        int[] depth = _depth;
        int[] shade = _shade;

        for (int y = 0; y < height; y++)
        {
            int table = (y + offsetY) * _tableWidth + offsetX;
            int row = y * pixels.Width;
            for (int x = 0; x < width; x++)
            {
                int i = table + x;
                int u = (angle[i] + angleShift) & TextureMask;
                int v = (depth[i] + depthShift) & TextureMask;
                Rgb texel = texture[(v << TextureBits) | u];
                int s = shade[i];
                output[row + x] = new Rgb((byte)((texel.R * s) >> 8), (byte)((texel.G * s) >> 8), (byte)((texel.B * s) >> 8));
            }
        }
    }

    /// <summary>Colour tiles with an XOR pattern inside, separated by darker grout; no texel is black.</summary>
    private void BuildTexture(double baseHue)
    {
        _texture = new Rgb[TextureSize * TextureSize];
        int tileMask = (1 << TileBits) - 1;
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                int pattern = (x ^ y) & TextureMask;
                bool checker = (((x >> TileBits) + (y >> TileBits)) & 1) == 0;
                bool grout = (x & tileMask) == 0 || (y & tileMask) == 0;

                double hue = baseHue + (checker ? 0.0 : 150.0) + 50.0 * pattern / TextureMask;
                double value = grout ? 0.35 : 0.6 + 0.4 * pattern / TextureMask;
                _texture[(y << TextureBits) | x] = Rgb.FromHsv(hue, grout ? 0.3 : 0.75, value);
            }
        }
    }

    /// <summary>Angle, depth and shade relative to the centre of a (2·width) × (2·height) table.</summary>
    private void BuildTables(FrameContext ctx)
    {
        _tableWidth = 2 * _width;
        int tableHeight = 2 * _height;
        int size = _tableWidth * tableHeight;
        _angle = new int[size];
        _depth = new int[size];
        _shade = new int[size];

        // Height of a pixel relative to its width, assuming terminal cells twice as tall as wide.
        double aspect = _width == 0 || _height == 0
            ? 1.0
            : (2.0 * ctx.Rows / _height) / ((double)ctx.Cols / _width);
        double radius = Math.Max(1.0, 0.5 * Math.Min(_width, _height * aspect));

        for (int y = 0; y < tableHeight; y++)
        {
            double dy = (y - _height + 0.5) * aspect;
            for (int x = 0; x < _tableWidth; x++)
            {
                double dx = x - _width + 0.5;
                double distance = Math.Max(MinDistance, Math.Sqrt(dx * dx + dy * dy) / radius);
                double turn = Math.Atan2(dy, dx) / (2.0 * Math.PI);
                double lit = Math.Clamp((distance - BlackRadius) / (FullBrightRadius - BlackRadius), 0.0, 1.0);

                int i = y * _tableWidth + x;
                _angle[i] = (int)Math.Floor(turn * AngleRepeats * TextureSize);
                _depth[i] = (int)Math.Floor(DepthScale * TextureSize / distance);
                _shade[i] = (int)Math.Round(256.0 * Math.Sqrt(lit) * lit);
            }
        }
    }
}
