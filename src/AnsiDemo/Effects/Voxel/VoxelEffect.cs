using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Voxel;

/// <summary>
/// Comanche-style voxel landscape: a tileable heightmap is marched front to back for every screen column and drawn
/// as vertical spans from the ground up, coloured green → brown → white by height and fogged with distance.
/// Pixels above the terrain (the sky) are never written.
/// </summary>
[Effect("voxel", PixelMode.HalfBlock)]
public sealed class VoxelEffect : IPixelEffect
{
    private const int MapSize = 256;                   // heightmap edge in world units; must be a power of two
    private const int MapMask = MapSize - 1;
    private const double TerrainHeight = 80.0;         // world units of a normalised height of 1
    private const double Clearance = 20.0;             // camera altitude above the highest terrain near its path
    private const double Speed = 24.0;                 // forward speed in world units per second
    private const double SwerveAmplitude = 70.0;       // sideways amplitude of the flight path in world units
    private const double SwerveRate = 0.16;            // radians per second of the sideways swerve
    private const double HorizonFraction = 0.45;       // horizon line as a fraction of the height from the top
    private const double SkyFraction = 0.15;           // terrain is never drawn above this fraction of the height
    private const double ProjectionScale = 1.1;        // vertical projection scale as a multiple of the height
    private const double HalfFieldOfView = 0.75;       // tangent of half the horizontal field of view
    private const double NearDistance = 2.0;
    private const double FarDistance = 230.0;
    private const double MinStep = 0.35;               // march step near the camera
    private const double StepGrowth = 0.018;           // march step grows by this fraction of the distance
    private const double FogStart = 0.35;              // fraction of FarDistance where fog begins
    private const double LookAheadSeconds = 1.5;       // span of the flight path the altitude keeps clear of
    private const int LookAheadSamples = 8;

    private static readonly Rgb FogColor = new(86, 98, 128);

    private DemoRandom _rng = new(1);
    private double[] _heights = [];
    private Rgb[] _colors = [];
    private int[] _yBuffer = [];
    private int _width;
    private int _height;
    private double _time;
    private double _originX;
    private double _originY;
    private double _camX;
    private double _camY;
    private double _camAltitude;
    private double _forwardX;
    private double _forwardY;

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _rng = rng;
        _width = ctx.PixelWidth;
        _height = ctx.PixelHeight;
        _yBuffer = new int[_width];
        _heights = BuildHeightmap(_rng);
        _colors = BuildColorMap(_heights);
        _originX = _rng.NextDouble() * MapSize;
        _originY = _rng.NextDouble() * MapSize;
        _time = ctx.Time;
        PlaceCamera();
    }

    public void Update(FrameContext ctx)
    {
        _time = ctx.Time;
        PlaceCamera();
    }

    public void Render(PixelBuffer pixels)
    {
        if (pixels.Width != _width || pixels.Height != _height)
        {
            return;
        }

        Rgb[] dst = pixels.Pixels;
        int width = _width;
        int[] yBuffer = _yBuffer;
        Array.Fill(yBuffer, _height);
        int skyLimit = (int)(_height * SkyFraction);
        double horizon = _height * HorizonFraction;
        double projection = _height * ProjectionScale;
        double rightX = _forwardY;
        double rightY = -_forwardX;

        double z = NearDistance;
        while (z < FarDistance)
        {
            double halfWidth = z * HalfFieldOfView;
            double stepX = rightX * 2.0 * halfWidth / width;
            double stepY = rightY * 2.0 * halfWidth / width;
            double wx = _camX + _forwardX * z - rightX * halfWidth + stepX * 0.5;
            double wy = _camY + _forwardY * z - rightY * halfWidth + stepY * 0.5;
            double scale = projection / z;
            double distance = z / FarDistance;
            double fogLinear = Math.Clamp((distance - FogStart) / (1.0 - FogStart), 0.0, 1.0);
            int fog = (int)(256.0 * fogLinear * (2.0 - fogLinear));
            int keep = 256 - fog;

            for (int x = 0; x < width; x++, wx += stepX, wy += stepY)
            {
                int bottom = yBuffer[x];
                if (bottom <= skyLimit)
                {
                    continue;
                }

                double screenY = horizon + (_camAltitude - SampleHeight(wx, wy) * TerrainHeight) * scale;
                int top = screenY <= skyLimit ? skyLimit : (int)screenY;
                if (top >= bottom)
                {
                    continue;
                }

                Rgb ground = _colors[((int)Math.Floor(wy) & MapMask) * MapSize + ((int)Math.Floor(wx) & MapMask)];
                var color = new Rgb(
                    (byte)((ground.R * keep + FogColor.R * fog) >> 8),
                    (byte)((ground.G * keep + FogColor.G * fog) >> 8),
                    (byte)((ground.B * keep + FogColor.B * fog) >> 8));
                for (int y = top, i = top * width + x; y < bottom; y++, i += width)
                {
                    dst[i] = color;
                }
                yBuffer[x] = top;
            }

            z += Math.Max(MinStep, z * StepGrowth);
        }
    }

    /// <summary>Camera position, heading and altitude, all a function of the remembered time only.</summary>
    private void PlaceCamera()
    {
        (_camX, _camY) = PathAt(_time);
        double dx = SwerveAmplitude * SwerveRate * Math.Cos(SwerveRate * _time);
        double length = Math.Sqrt(dx * dx + Speed * Speed);
        _forwardX = dx / length;
        _forwardY = Speed / length;

        double highest = 0.0;
        for (int i = 0; i <= LookAheadSamples; i++)
        {
            (double px, double py) = PathAt(_time + LookAheadSeconds * i / LookAheadSamples);
            highest = Math.Max(highest, SampleHeight(px, py));
        }
        _camAltitude = highest * TerrainHeight + Clearance;
    }

    private (double X, double Y) PathAt(double time) =>
        (_originX + SwerveAmplitude * Math.Sin(SwerveRate * time), _originY + Speed * time);

    /// <summary>Bilinearly interpolated normalised height at world position (x, y); the map tiles in both directions.</summary>
    private double SampleHeight(double x, double y)
    {
        double fx = Math.Floor(x);
        double fy = Math.Floor(y);
        int x0 = (int)fx & MapMask;
        int y0 = (int)fy & MapMask;
        int x1 = (x0 + 1) & MapMask;
        int y1 = (y0 + 1) & MapMask;
        double tx = x - fx;
        double ty = y - fy;
        int row0 = y0 * MapSize;
        int row1 = y1 * MapSize;
        double top = _heights[row0 + x0] + (_heights[row0 + x1] - _heights[row0 + x0]) * tx;
        double bottom = _heights[row1 + x0] + (_heights[row1 + x1] - _heights[row1 + x0]) * tx;
        return top + (bottom - top) * ty;
    }

    /// <summary>Tileable value noise of five octaves, normalised to 0..1 and shaped for flat valleys and sharp peaks.</summary>
    private static double[] BuildHeightmap(DemoRandom rng)
    {
        var heights = new double[MapSize * MapSize];
        double amplitude = 1.0;
        for (int cells = 4; cells <= 64; cells *= 2, amplitude *= 0.5)
        {
            var lattice = new double[cells * cells];
            for (int i = 0; i < lattice.Length; i++)
            {
                lattice[i] = rng.NextDouble();
            }

            int cellSize = MapSize / cells;
            for (int y = 0; y < MapSize; y++)
            {
                int gy0 = y / cellSize;
                int gy1 = (gy0 + 1) % cells;
                double ty = SmoothStep((y % cellSize) / (double)cellSize);
                for (int x = 0; x < MapSize; x++)
                {
                    int gx0 = x / cellSize;
                    int gx1 = (gx0 + 1) % cells;
                    double tx = SmoothStep((x % cellSize) / (double)cellSize);
                    double top = lattice[gy0 * cells + gx0] + (lattice[gy0 * cells + gx1] - lattice[gy0 * cells + gx0]) * tx;
                    double bottom = lattice[gy1 * cells + gx0] + (lattice[gy1 * cells + gx1] - lattice[gy1 * cells + gx0]) * tx;
                    heights[y * MapSize + x] += amplitude * (top + (bottom - top) * ty);
                }
            }
        }

        double min = heights.Min();
        double range = Math.Max(heights.Max() - min, 1e-9);
        for (int i = 0; i < heights.Length; i++)
        {
            heights[i] = Math.Pow((heights[i] - min) / range, 1.25);
        }
        return heights;
    }

    /// <summary>Colour per map texel: green → brown → white by height, lit from one side by the local slope.</summary>
    private static Rgb[] BuildColorMap(double[] heights)
    {
        var colors = new Rgb[MapSize * MapSize];
        for (int y = 0; y < MapSize; y++)
        {
            for (int x = 0; x < MapSize; x++)
            {
                double h = heights[y * MapSize + x];
                double slope = heights[((y - 1) & MapMask) * MapSize + ((x - 1) & MapMask)]
                             - heights[((y + 1) & MapMask) * MapSize + ((x + 1) & MapMask)];
                double light = Math.Clamp(1.0 + slope * 9.0, 0.45, 1.35);
                double grain = 0.94 + 0.12 * (DemoRandom.Hash(x, y) / 4294967296.0);
                colors[y * MapSize + x] = Shade(HeightColor(h), light * grain);
            }
        }
        return colors;
    }

    private static Rgb HeightColor(double h) => h switch
    {
        < 0.25 => Rgb.Lerp(new Rgb(24, 84, 28), new Rgb(64, 138, 40), h / 0.25),
        < 0.45 => Rgb.Lerp(new Rgb(64, 138, 40), new Rgb(118, 92, 52), (h - 0.25) / 0.20),
        < 0.57 => Rgb.Lerp(new Rgb(118, 92, 52), new Rgb(140, 124, 108), (h - 0.45) / 0.12),
        _ => Rgb.Lerp(new Rgb(140, 124, 108), new Rgb(244, 244, 250), (h - 0.57) / 0.12),
    };

    private static Rgb Shade(Rgb c, double factor) => new(
        (byte)Math.Clamp(c.R * factor, 0.0, 255.0),
        (byte)Math.Clamp(c.G * factor, 0.0, 255.0),
        (byte)Math.Clamp(c.B * factor, 0.0, 255.0));

    private static double SmoothStep(double t) => t * t * (3.0 - 2.0 * t);
}
