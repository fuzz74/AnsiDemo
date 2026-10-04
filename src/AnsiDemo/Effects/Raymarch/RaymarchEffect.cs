using AnsiDemo.Contracts;

namespace AnsiDemo.Effects.Raymarch;

/// <summary>
/// Raymarched spheres: three spheres resting on a checkered floor and a fourth orbiting the middle one, lit by one
/// point light (Lambert plus Blinn-Phong specular, soft shadows) under a dark sky, seen by a camera that slowly
/// circles and sways. The spheres are sphere-traced through their signed distance field on a coarse internal grid
/// (at most about 100×60 rays), which is traced in <see cref="Reset"/> and <see cref="Update"/> and scaled up
/// bilinearly in <see cref="Render"/>.
/// </summary>
[Effect("raymarch", PixelMode.Braille)]
public sealed class RaymarchEffect : IPixelEffect
{
    private const int MaxGridWidth = 100;
    private const int MaxGridHeight = 60;

    private const int MaxSteps = 40;
    private const int MaxShadowSteps = 20;
    private const double HitEpsilon = 0.002;
    private const double NearMissDistance = 0.05;
    private const double FarDistance = 40.0;
    private const double ShadowSoftness = 8.0;

    private const int SphereCount = 4;
    private const int Orbiter = SphereCount - 1;
    private const double OrbitRadius = 1.95;
    private const double OrbitHeight = 1.5;
    private const double OrbitBob = 0.3;
    private const double OrbitSpeed = 1.2;

    /// <summary>The bounding sphere of all spheres is centred here; rays that miss it skip the march.</summary>
    private const double BoundY = 0.9;

    private const double CameraRadius = 5.2, CameraRadiusSway = 0.5;
    private const double CameraHeight = 1.6, CameraHeightSway = 0.3;
    private const double CameraStartAngle = -0.35, CameraSpeed = 0.11;
    private const double TargetY = 0.9;

    /// <summary>Distance from the eye to the screen plane, in units of half the screen height.</summary>
    private const double Focal = 2.1;

    private const double LightX = -5.5, LightY = 6.5, LightZ = -1.5;

    private const double Ambient = 0.22;
    private const double Diffuse = 0.9;
    private const double Shininess = 48.0;
    private const double Specular = 1.1;
    private const double FloorShininess = 64.0;
    private const double FloorSpecular = 0.35;

    private const double FogStart = 5.0;
    private const double FogDensity = 0.07;
    private const double CheckerFadeStart = 7.0, CheckerFadeEnd = 16.0;

    private const double LightSquare = 245.0;
    private const double DarkSquareR = 22.0, DarkSquareG = 26.0, DarkSquareB = 60.0;
    private const double HorizonR = 8.0, HorizonG = 10.0, HorizonB = 30.0;
    private const double SkyFadeRate = 10.0;

    // Sphere centres and radii; the last one is the orbiter, whose centre is set from the time.
    private readonly double[] _cx = [0.0, -3.0, 3.1, 0.0];
    private readonly double[] _cy = [1.0, 0.75, 0.9, OrbitHeight];
    private readonly double[] _cz = [0.0, 1.3, -1.3, 0.0];
    private readonly double[] _radius = [1.0, 0.75, 0.9, 0.42];

    // Base colours: warm red, green, blue and yellow.
    private readonly double[] _red = [255.0, 90.0, 100.0, 250.0];
    private readonly double[] _green = [95.0, 230.0, 150.0, 235.0];
    private readonly double[] _blue = [75.0, 120.0, 255.0, 80.0];

    private int _pixelWidth;
    private int _pixelHeight;
    private int _gridWidth;
    private int _gridHeight;
    private double _time;
    private double _orbitPhase;

    // Screen-plane offsets of the ray through each grid column and row.
    private double[] _rayU = [];
    private double[] _rayV = [];

    // The traced picture, three doubles (R, G, B in 0..255) per grid cell.
    private double[] _grid = [];

    // Bilinear upscaling: for each pixel column and row, the two grid samples and the weight of the second one.
    private int[] _column0 = [], _column1 = [];
    private double[] _columnWeight = [];
    private int[] _row0 = [], _row1 = [];
    private double[] _rowWeight = [];

    // The camera of the current frame.
    private double _eyeX, _eyeY, _eyeZ;
    private double _forwardX, _forwardY, _forwardZ;
    private double _rightX, _rightZ;
    private double _upX, _upY, _upZ;

    private double _boundRadius;

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _pixelWidth = ctx.PixelWidth;
        _pixelHeight = ctx.PixelHeight;
        int scale = Math.Max(1, Math.Max(
            (_pixelWidth + MaxGridWidth - 1) / MaxGridWidth,
            (_pixelHeight + MaxGridHeight - 1) / MaxGridHeight));
        _gridWidth = (_pixelWidth + scale - 1) / scale;
        _gridHeight = (_pixelHeight + scale - 1) / scale;

        // Square pixels in both modes, so one unit of the screen plane is half the pixel height in both directions.
        double halfHeight = _pixelHeight / 2.0;
        _rayU = new double[_gridWidth];
        for (int i = 0; i < _gridWidth; i++)
        {
            _rayU[i] = ((i + 0.5) * scale - _pixelWidth / 2.0) / halfHeight;
        }
        _rayV = new double[_gridHeight];
        for (int j = 0; j < _gridHeight; j++)
        {
            _rayV[j] = (halfHeight - (j + 0.5) * scale) / halfHeight;
        }
        _grid = new double[_gridWidth * _gridHeight * 3];

        (_column0, _column1, _columnWeight) = UpscaleTaps(_pixelWidth, _gridWidth, scale);
        (_row0, _row1, _rowWeight) = UpscaleTaps(_pixelHeight, _gridHeight, scale);

        // A runner resets with the same seed on resize, so the orbiter does not jump.
        _orbitPhase = rng.NextDouble() * 2.0 * Math.PI;

        _time = ctx.Time;
        Trace();
    }

    public void Update(FrameContext ctx)
    {
        _time = ctx.Time;
        Trace();
    }

    public void Render(PixelBuffer pixels)
    {
        Rgb[] target = pixels.Pixels;
        int width = Math.Min(pixels.Width, _pixelWidth);
        int height = Math.Min(pixels.Height, _pixelHeight);
        double[] grid = _grid;
        int stride = _gridWidth * 3;
        for (int y = 0; y < height; y++)
        {
            int top = _row0[y] * stride;
            int bottom = _row1[y] * stride;
            double wy = _rowWeight[y];
            int row = y * pixels.Width;
            for (int x = 0; x < width; x++)
            {
                int left = _column0[x] * 3;
                int right = _column1[x] * 3;
                double wx = _columnWeight[x];
                double w00 = (1.0 - wx) * (1.0 - wy), w10 = wx * (1.0 - wy), w01 = (1.0 - wx) * wy, w11 = wx * wy;
                int a = top + left, b = top + right, c = bottom + left, d = bottom + right;
                double r = grid[a] * w00 + grid[b] * w10 + grid[c] * w01 + grid[d] * w11;
                double g = grid[a + 1] * w00 + grid[b + 1] * w10 + grid[c + 1] * w01 + grid[d + 1] * w11;
                double bl = grid[a + 2] * w00 + grid[b + 2] * w10 + grid[c + 2] * w01 + grid[d + 2] * w11;
                target[row + x] = new Rgb((byte)(r + 0.5), (byte)(g + 0.5), (byte)(bl + 0.5));
            }
        }
    }

    /// <summary>For each of <paramref name="pixels"/> positions, the two grid samples around its centre and the weight of the second.</summary>
    private static (int[] First, int[] Second, double[] Weight) UpscaleTaps(int pixels, int samples, int scale)
    {
        var first = new int[pixels];
        var second = new int[pixels];
        var weight = new double[pixels];
        for (int p = 0; p < pixels; p++)
        {
            double position = Math.Clamp((p + 0.5) / scale - 0.5, 0.0, samples - 1);
            int index = (int)position;
            first[p] = index;
            second[p] = Math.Min(index + 1, samples - 1);
            weight[p] = position - index;
        }
        return (first, second, weight);
    }

    /// <summary>Places the orbiter and the camera for the current time and traces one ray per grid cell.</summary>
    private void Trace()
    {
        double t = _time;
        double orbit = t * OrbitSpeed + _orbitPhase;
        _cx[Orbiter] = OrbitRadius * Math.Cos(orbit);
        _cz[Orbiter] = OrbitRadius * Math.Sin(orbit);
        _cy[Orbiter] = OrbitHeight + OrbitBob * Math.Sin(2.0 * orbit);

        _boundRadius = 0.0;
        for (int s = 0; s < SphereCount; s++)
        {
            double dy = _cy[s] - BoundY;
            double reach = Math.Sqrt(_cx[s] * _cx[s] + dy * dy + _cz[s] * _cz[s]) + _radius[s];
            _boundRadius = Math.Max(_boundRadius, reach);
        }

        double angle = CameraStartAngle + t * CameraSpeed;
        double distance = CameraRadius + CameraRadiusSway * Math.Sin(t * 0.23);
        _eyeX = distance * Math.Sin(angle);
        _eyeY = CameraHeight + CameraHeightSway * Math.Sin(t * 0.31);
        _eyeZ = -distance * Math.Cos(angle);

        double fx = -_eyeX, fy = TargetY - _eyeY, fz = -_eyeZ;
        double fLength = Math.Sqrt(fx * fx + fy * fy + fz * fz);
        _forwardX = fx / fLength;
        _forwardY = fy / fLength;
        _forwardZ = fz / fLength;

        // right = world up × forward (horizontal); up = forward × right.
        double rLength = Math.Sqrt(_forwardZ * _forwardZ + _forwardX * _forwardX);
        _rightX = _forwardZ / rLength;
        _rightZ = -_forwardX / rLength;
        _upX = _forwardY * _rightZ;
        _upY = _forwardZ * _rightX - _forwardX * _rightZ;
        _upZ = -_forwardY * _rightX;

        int index = 0;
        for (int j = 0; j < _gridHeight; j++)
        {
            double v = _rayV[j];
            for (int i = 0; i < _gridWidth; i++)
            {
                double u = _rayU[i];
                double dx = _forwardX * Focal + _rightX * u + _upX * v;
                double dy = _forwardY * Focal + _upY * v;
                double dz = _forwardZ * Focal + _rightZ * u + _upZ * v;
                double length = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                ShadeRay(dx / length, dy / length, dz / length, index);
                index += 3;
            }
        }
    }

    /// <summary>Traces the ray from the eye along the unit direction (dx, dy, dz) and stores its colour at <paramref name="index"/>.</summary>
    private void ShadeRay(double dx, double dy, double dz, int index)
    {
        double ox = _eyeX, oy = _eyeY, oz = _eyeZ;

        // The floor y = 0 is a plane, so its distance is exact; the spheres are marched only up to it.
        double floorDistance = dy < 0.0 ? -oy / dy : double.PositiveInfinity;
        double limit = Math.Min(floorDistance, FarDistance);

        int sphere = -1;
        double t = 0.0;
        if (BoundingInterval(ox, oy, oz, dx, dy, dz, out double enter, out double exit))
        {
            t = Math.Max(enter, 0.0);
            limit = Math.Min(limit, exit);
            sphere = March(ox, oy, oz, dx, dy, dz, ref t, limit);
        }

        double r, g, b;
        if (sphere >= 0)
        {
            ShadeSphere(sphere, ox + dx * t, oy + dy * t, oz + dz * t, dx, dy, dz, out r, out g, out b);
        }
        else if (floorDistance < FarDistance)
        {
            ShadeFloor(ox + dx * floorDistance, oz + dz * floorDistance, floorDistance, dx, dy, dz, out r, out g, out b);
        }
        else
        {
            double sky = Math.Clamp(1.0 - dy * SkyFadeRate, 0.0, 1.0);
            sky *= sky;
            r = HorizonR * sky;
            g = HorizonG * sky;
            b = HorizonB * sky;
        }

        _grid[index] = Math.Min(r, 255.0);
        _grid[index + 1] = Math.Min(g, 255.0);
        _grid[index + 2] = Math.Min(b, 255.0);
    }

    /// <summary>
    /// Sphere-traces the spheres' distance field from <paramref name="t"/> to <paramref name="limit"/>; returns the
    /// sphere hit (with <paramref name="t"/> at the surface) or −1.
    /// </summary>
    private int March(double ox, double oy, double oz, double dx, double dy, double dz, ref double t, double limit)
    {
        int nearest = -1;
        double nearestDistance = double.PositiveInfinity;
        for (int step = 0; step < MaxSteps && t < limit; step++)
        {
            nearestDistance = SceneDistance(ox + dx * t, oy + dy * t, oz + dz * t, -1, out nearest);
            if (nearestDistance < HitEpsilon * (1.0 + t))
            {
                return nearest;
            }
            t += nearestDistance;
        }

        // Out of steps while grazing a silhouette counts as a hit, so edges do not get holes.
        return t < limit && nearestDistance < NearMissDistance ? nearest : -1;
    }

    /// <summary>The signed distance from (x, y, z) to the nearest sphere other than <paramref name="skip"/>.</summary>
    private double SceneDistance(double x, double y, double z, int skip, out int nearest)
    {
        double best = double.PositiveInfinity;
        nearest = -1;
        for (int s = 0; s < SphereCount; s++)
        {
            if (s == skip)
            {
                continue;
            }
            double ex = x - _cx[s], ey = y - _cy[s], ez = z - _cz[s];
            double distance = Math.Sqrt(ex * ex + ey * ey + ez * ez) - _radius[s];
            if (distance < best)
            {
                best = distance;
                nearest = s;
            }
        }
        return best;
    }

    /// <summary>Where the ray enters and leaves the sphere bounding all spheres; false if it misses or it lies behind.</summary>
    private bool BoundingInterval(double ox, double oy, double oz, double dx, double dy, double dz, out double enter, out double exit)
    {
        double px = ox, py = oy - BoundY, pz = oz;
        double half = px * dx + py * dy + pz * dz;
        double c = px * px + py * py + pz * pz - _boundRadius * _boundRadius;
        double discriminant = half * half - c;
        if (discriminant <= 0.0)
        {
            enter = exit = 0.0;
            return false;
        }
        double root = Math.Sqrt(discriminant);
        enter = -half - root;
        exit = -half + root;
        return exit > 0.0;
    }

    /// <summary>Soft shadow factor (0 = dark, 1 = lit) of the point towards the light, ignoring sphere <paramref name="skip"/>.</summary>
    private double Shadow(double px, double py, double pz, double lx, double ly, double lz, double lightDistance, int skip)
    {
        if (!BoundingInterval(px, py, pz, lx, ly, lz, out double enter, out double exit))
        {
            return 1.0;
        }
        double t = Math.Max(enter, 0.02);
        double limit = Math.Min(exit, lightDistance);
        double result = 1.0;
        for (int step = 0; step < MaxShadowSteps && t < limit; step++)
        {
            double h = SceneDistance(px + lx * t, py + ly * t, pz + lz * t, skip, out _);
            if (h < 0.001)
            {
                return 0.0;
            }
            result = Math.Min(result, ShadowSoftness * h / t);
            t += h;
        }
        return Math.Clamp(result, 0.0, 1.0);
    }

    private void ShadeSphere(int s, double px, double py, double pz, double dx, double dy, double dz,
        out double r, out double g, out double b)
    {
        double radius = _radius[s];
        double nx = (px - _cx[s]) / radius, ny = (py - _cy[s]) / radius, nz = (pz - _cz[s]) / radius;
        Light(px, py, pz, nx, ny, nz, dx, dy, dz, s, Shininess, out double diffuse, out double specular);

        double shade = Ambient + Diffuse * diffuse;
        double highlight = Specular * specular * 255.0;
        r = _red[s] * shade + highlight;
        g = _green[s] * shade + highlight;
        b = _blue[s] * shade + highlight;
    }

    private void ShadeFloor(double px, double pz, double distance, double dx, double dy, double dz,
        out double r, out double g, out double b)
    {
        Light(px, 0.0, pz, 0.0, 1.0, 0.0, dx, dy, dz, -1, FloorShininess, out double diffuse, out double specular);

        // Squares far away fade to their mean, so the coarse grid does not alias them into noise.
        bool light = (((int)Math.Floor(px) + (int)Math.Floor(pz)) & 1) == 0;
        double contrast = 1.0 - Math.Clamp((distance - CheckerFadeStart) / (CheckerFadeEnd - CheckerFadeStart), 0.0, 1.0);
        double side = light ? contrast : -contrast;
        double baseR = (LightSquare + DarkSquareR) / 2.0 + side * (LightSquare - DarkSquareR) / 2.0;
        double baseG = (LightSquare + DarkSquareG) / 2.0 + side * (LightSquare - DarkSquareG) / 2.0;
        double baseB = (LightSquare + DarkSquareB) / 2.0 + side * (LightSquare - DarkSquareB) / 2.0;

        double shade = Ambient + Diffuse * diffuse;
        double highlight = FloorSpecular * specular * 255.0;
        double fog = Math.Exp(-FogDensity * Math.Max(0.0, distance - FogStart));
        r = (baseR * shade + highlight) * fog + HorizonR * (1.0 - fog);
        g = (baseG * shade + highlight) * fog + HorizonG * (1.0 - fog);
        b = (baseB * shade + highlight) * fog + HorizonB * (1.0 - fog);
    }

    /// <summary>Lambert and Blinn-Phong terms of the light at a point with unit normal n seen along the unit ray d, shadows included.</summary>
    private void Light(double px, double py, double pz, double nx, double ny, double nz, double dx, double dy, double dz,
        int skip, double shininess, out double diffuse, out double specular)
    {
        double lx = LightX - px, ly = LightY - py, lz = LightZ - pz;
        double lightDistance = Math.Sqrt(lx * lx + ly * ly + lz * lz);
        lx /= lightDistance;
        ly /= lightDistance;
        lz /= lightDistance;

        double lambert = nx * lx + ny * ly + nz * lz;
        if (lambert <= 0.0)
        {
            diffuse = specular = 0.0;
            return;
        }
        double shadow = Shadow(px + nx * 0.01, py + ny * 0.01, pz + nz * 0.01, lx, ly, lz, lightDistance, skip);

        // Half vector between the light and the eye (−d).
        double hx = lx - dx, hy = ly - dy, hz = lz - dz;
        double hLength = Math.Sqrt(hx * hx + hy * hy + hz * hz);
        double facing = Math.Max(0.0, (nx * hx + ny * hy + nz * hz) / hLength);
        diffuse = lambert * shadow;
        specular = Math.Pow(facing, shininess) * shadow;
    }
}
