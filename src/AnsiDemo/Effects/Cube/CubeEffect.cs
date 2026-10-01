using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Cube;

/// <summary>
/// A wireframe cube rotating about the X and Y axes in perspective, drawn over whatever is
/// already in the buffer (only edge pixels are written). The edge colour cycles through the hues.
/// </summary>
[Effect("cube", PixelMode.Braille)]
public sealed class CubeEffect : IPixelEffect
{
    // Camera distance from the cube centre, in cube half-edges.
    private const double CameraDistance = 4.0;

    // Focal length as a fraction of the smaller buffer side. With the camera at 4 the projected
    // half-extent stays below 0.48 * focal, so the cube spans about 57-82% of the height.
    private const double FocalFraction = 0.85;

    private const double PitchSpeed = 0.9, PitchPhase = 0.6;   // radians per second, radians
    private const double YawSpeed = 0.55, YawPhase = 0.4;
    private const double HueDegreesPerSecond = 45.0;

    // Low saturation keeps the luma high, so the Braille dither lights most dots of every hue.
    private const double Saturation = 0.4;

    private static ReadOnlySpan<sbyte> Corners =>
    [
        -1, -1, -1,   1, -1, -1,   1, 1, -1,   -1, 1, -1,
        -1, -1, 1,    1, -1, 1,    1, 1, 1,    -1, 1, 1,
    ];

    private static ReadOnlySpan<byte> Edges =>
    [
        0, 1,  1, 2,  2, 3,  3, 0,
        4, 5,  5, 6,  6, 7,  7, 4,
        0, 4,  1, 5,  2, 6,  3, 7,
    ];

    private readonly int[] _screenX = new int[8];
    private readonly int[] _screenY = new int[8];
    private double _time;
    private double _centerX, _centerY, _focal;
    private bool _thick;
    private Rgb _color;

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _centerX = (ctx.PixelWidth - 1) / 2.0;
        _centerY = (ctx.PixelHeight - 1) / 2.0;
        _focal = FocalFraction * Math.Min(ctx.PixelWidth, ctx.PixelHeight);
        _thick = ctx.Mode == PixelMode.Braille;
        Advance(ctx.Time);
    }

    public void Update(FrameContext ctx) => Advance(ctx.Time);

    public void Render(PixelBuffer pixels)
    {
        for (int e = 0; e < Edges.Length; e += 2)
        {
            int a = Edges[e], b = Edges[e + 1];
            DrawLine(pixels, _screenX[a], _screenY[a], _screenX[b], _screenY[b]);
        }
    }

    private void Advance(double time)
    {
        _time = time;
        _color = Rgb.FromHsv(_time * HueDegreesPerSecond, Saturation, 1.0);

        double pitch = PitchPhase + PitchSpeed * _time;
        double yaw = YawPhase + YawSpeed * _time;
        double cp = Math.Cos(pitch), sp = Math.Sin(pitch);
        double cy = Math.Cos(yaw), sy = Math.Sin(yaw);

        for (int i = 0; i < 8; i++)
        {
            double x = Corners[3 * i], y = Corners[3 * i + 1], z = Corners[3 * i + 2];

            // Rotate about X, then about Y.
            double y1 = y * cp - z * sp;
            double z1 = y * sp + z * cp;
            double x2 = x * cy + z1 * sy;
            double z2 = -x * sy + z1 * cy;

            // Pixels are square in both modes, so one focal length serves both axes.
            double scale = _focal / (CameraDistance + z2);
            _screenX[i] = (int)Math.Round(_centerX + x2 * scale);
            _screenY[i] = (int)Math.Round(_centerY - y1 * scale);
        }
    }

    /// <summary>Bresenham line; pixels outside the buffer are skipped by the indexer.</summary>
    private void DrawLine(PixelBuffer pixels, int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int stepX = x0 < x1 ? 1 : -1, stepY = y0 < y1 ? 1 : -1;
        bool steep = -dy > dx;
        int err = dx + dy;
        while (true)
        {
            Plot(pixels, x0, y0, steep);
            if (x0 == x1 && y0 == y1)
            {
                return;
            }
            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += stepX;
            }
            if (e2 <= dx)
            {
                err += dx;
                y0 += stepY;
            }
        }
    }

    private void Plot(PixelBuffer pixels, int x, int y, bool steep)
    {
        pixels[x, y] = _color;
        if (_thick)
        {
            // Thicken across the line so the Braille dither cannot break it up.
            if (steep)
            {
                pixels[x + 1, y] = _color;
            }
            else
            {
                pixels[x, y + 1] = _color;
            }
        }
    }
}
