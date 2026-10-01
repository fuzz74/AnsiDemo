using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Rotozoomer;

/// <summary>
/// A tiled procedural texture (checkerboard quadrants with an XOR gradient, a ring per quadrant and a grid)
/// rotating, zooming in and out and drifting across the screen.
/// </summary>
[Effect("rotozoomer", PixelMode.HalfBlock)]
public sealed class RotozoomerEffect : IPixelEffect
{
    private const int TextureBits = 6;
    private const int TextureSize = 1 << TextureBits;
    private const int TextureMask = TextureSize - 1;
    private const int QuadrantSize = TextureSize / 2;
    private const int FixedShift = 16;
    private const double FixedOne = 1 << FixedShift;

    /// <summary>Texture texels per screen pixel at zoom 1 when the buffer is this many pixels high.</summary>
    private const double ReferenceHeight = 48.0;
    private const double ZoomBase = 1.1, ZoomAmplitude = 0.75, ZoomSpeed = 0.9;
    private const double SpinPhase = 0.35, SpinSpeed = 0.45, WobbleAmplitude = 0.4, WobbleSpeed = 0.31;
    private const double DriftU = 9.0, DriftV = 5.0;

    private Rgb[] _texture = [];
    private int _width, _height;
    private double _aspect = 1.0;

    // Texture walk for the current time, in 16.16 fixed point.
    private int _u0, _v0, _duDx, _dvDx, _duDy, _dvDy;

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _width = ctx.PixelWidth;
        _height = ctx.PixelHeight;

        // Pixel height over pixel width, with a terminal cell twice as high as it is wide.
        double pixelW = (double)ctx.Cols / ctx.PixelWidth;
        double pixelH = 2.0 * ctx.Rows / ctx.PixelHeight;
        _aspect = pixelH / pixelW;

        _texture = new Rgb[TextureSize * TextureSize];
        for (int v = 0; v < TextureSize; v++)
        {
            for (int u = 0; u < TextureSize; u++)
            {
                double grain = (rng.NextDouble() - 0.5) * 0.08;
                _texture[(v << TextureBits) | u] = Texel(u, v, grain);
            }
        }

        SetTime(ctx.Time);
    }

    public void Update(FrameContext ctx) => SetTime(ctx.Time);

    public void Render(PixelBuffer pixels)
    {
        Rgb[] target = pixels.Pixels;
        Rgb[] texture = _texture;
        int width = Math.Min(_width, pixels.Width);
        int height = Math.Min(_height, pixels.Height);
        int rowU = _u0, rowV = _v0;
        for (int y = 0; y < height; y++)
        {
            int u = rowU, v = rowV;
            int index = y * pixels.Width;
            for (int x = 0; x < width; x++)
            {
                int texel = (((v >> FixedShift) & TextureMask) << TextureBits) | ((u >> FixedShift) & TextureMask);
                target[index + x] = texture[texel];
                u += _duDx;
                v += _dvDx;
            }
            rowU += _duDy;
            rowV += _dvDy;
        }
    }

    private void SetTime(double time)
    {
        double angle = SpinPhase + SpinSpeed * time +WobbleAmplitude * Math.Sin(WobbleSpeed * time);
        double zoom = ZoomBase + ZoomAmplitude * Math.Sin(ZoomSpeed * time);
        double scale = zoom * ReferenceHeight / Math.Max(1, _height);
        double cos = Math.Cos(angle) * scale, sin = Math.Sin(angle) * scale;

        double duDx = cos, dvDx = sin;
        double duDy = -sin * _aspect, dvDy = cos * _aspect;

        // The screen centre looks at a texel that drifts; wrapped so the fixed-point values stay small.
        double centreU = Wrap(QuadrantSize + DriftU * time);
        double centreV = Wrap(QuadrantSize + DriftV * time);
        double halfW = _width * 0.5 - 0.5, halfH = _height * 0.5 - 0.5;
        double u0 = centreU - duDx * halfW - duDy * halfH;
        double v0 = centreV - dvDx * halfW - dvDy * halfH;

        _u0 = ToFixed(u0);
        _v0 = ToFixed(v0);
        _duDx = ToFixed(duDx);
        _dvDx = ToFixed(dvDx);
        _duDy = ToFixed(duDy);
        _dvDy = ToFixed(dvDy);
    }

    private static Rgb Texel(int u, int v, double grain)
    {
        int lu = u & (QuadrantSize - 1), lv = v & (QuadrantSize - 1);
        bool warm = (((u / QuadrantSize) ^ (v / QuadrantSize)) & 1) == 0;
        double xor = ((u ^ v) & (QuadrantSize - 1)) / (double)(QuadrantSize - 1);

        if (lu == 0 || lv == 0)
        {
            return Rgb.FromHsv(warm ? 280 : 250, 0.6, 0.35);
        }

        double dx = lu - QuadrantSize / 2.0, dy = lv - QuadrantSize / 2.0;
        double r = Math.Sqrt(dx * dx + dy * dy);
        if (Math.Abs(r - 9.5) < 2.0)
        {
            return Rgb.FromHsv(warm ? 55 : 170, 0.3, 1.0 + grain);
        }
        if (r < 3.0)
        {
            return Rgb.FromHsv(warm ? 330 : 120, 0.8, 0.95);
        }

        double hue = warm ? 20 + 35 * xor : 190 + 50 * xor;
        return Rgb.FromHsv(hue, 0.85, 0.55 + 0.4 * xor + grain);
    }

    private static double Wrap(double coordinate)
    {
        double wrapped = coordinate % TextureSize;
        return wrapped < 0 ? wrapped + TextureSize : wrapped;
    }

    private static int ToFixed(double value) => (int)Math.Round(value * FixedOne);
}
