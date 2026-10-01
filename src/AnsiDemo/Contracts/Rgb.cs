namespace AnsiDemo.Contracts;

public readonly record struct Rgb(byte R, byte G, byte B)
{
    public static readonly Rgb Black = new(0, 0, 0), White = new(255, 255, 255);

    /// <summary>Perceived brightness, 0..255.</summary>
    public int Luma => (R * 299 + G * 587 + B * 114) / 1000;

    /// <summary>Linear interpolation per channel; t is clamped to 0..1 and channels are rounded away from zero.</summary>
    public static Rgb Lerp(Rgb a, Rgb b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return new Rgb(LerpChannel(a.R, b.R, t), LerpChannel(a.G, b.G, t), LerpChannel(a.B, b.B, t));
    }

    /// <summary>Standard HSV to RGB; the hue wraps around 360 degrees (negative too), s and v are clamped to 0..1.</summary>
    public static Rgb FromHsv(double hueDegrees, double s, double v)
    {
        s = Math.Clamp(s, 0.0, 1.0);
        v = Math.Clamp(v, 0.0, 1.0);
        double h = hueDegrees % 360.0;
        if (h < 0)
        {
            h += 360.0;
        }
        if (h >= 360.0)
        {
            h -= 360.0;
        }

        double c = v * s;
        double hp = h / 60.0;
        double x = c * (1.0 - Math.Abs(hp % 2.0 - 1.0));
        double m = v - c;
        (double r, double g, double b) = (int)hp switch
        {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        return new Rgb(ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    private static byte LerpChannel(byte a, byte b, double t) =>
        (byte)Math.Round(a + (b - a) * t, MidpointRounding.AwayFromZero);

    private static byte ToByte(double unit) =>
        (byte)Math.Clamp(Math.Round(unit * 255.0, MidpointRounding.AwayFromZero), 0.0, 255.0);
}

public enum PixelMode { HalfBlock, Braille }
