namespace AnsiDemo.Contracts;

/// <param name="Time">Seconds since Reset (scene-local).</param>
/// <param name="DeltaTime">Seconds since the previous Update (0 in Reset).</param>
/// <param name="Beats">Time * Bpm / 60.</param>
/// <param name="Mode">Pixel mode of the scene.</param>
/// <param name="Cols">Frame columns.</param>
/// <param name="Rows">Frame rows.</param>
/// <param name="PixelWidth">PixelBuffer.SizeFor(Mode, Cols, Rows).Width.</param>
/// <param name="PixelHeight">PixelBuffer.SizeFor(Mode, Cols, Rows).Height.</param>
public sealed record FrameContext(
    double Time, double DeltaTime,
    double Beats,
    PixelMode Mode, int Cols, int Rows,
    int PixelWidth, int PixelHeight);

[AttributeUsage(AttributeTargets.Class)]
public sealed class EffectAttribute(string name, PixelMode preferredMode) : Attribute
{
    public string Name { get; } = name;
    public PixelMode PreferredMode { get; } = preferredMode;
}

public interface IEffect
{
    /// <summary>Initial state for this size; keep rng for later randomness.</summary>
    void Reset(FrameContext ctx, DemoRandom rng);

    /// <summary>Advance by ctx.DeltaTime.</summary>
    void Update(FrameContext ctx);
}

public interface IPixelEffect : IEffect
{
    /// <summary>Draws over the current contents.</summary>
    void Render(PixelBuffer pixels);
}

public interface ICellEffect : IEffect
{
    /// <summary>Draws over the rendered cells.</summary>
    void Render(Frame frame);
}

public static class ScrollerText
{
    public const string Value = "ANSIDEMO  *  TWELVE EFFECTS  *  EIGHT SCENES  *  ONE TERMINAL  *  CHIPTUNES INCLUDED  *  GREETINGS TO ALL PARTY PEOPLE  *  ";
}
