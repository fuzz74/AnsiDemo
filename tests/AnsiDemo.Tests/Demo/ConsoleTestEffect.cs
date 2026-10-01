using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Demo;

/// <summary>Registered effect that <see cref="ConsolePlaybackTests"/> builds its scenes from; writes "CONSOLE" at (0,0).</summary>
[Effect(Name, PixelMode.HalfBlock)]
public sealed class ConsoleTestEffect : ICellEffect
{
    public const string Name = "test-console";

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
    }

    public void Update(FrameContext ctx)
    {
    }

    public void Render(Frame frame) => frame.Write(0, 0, "CONSOLE", Rgb.White, Rgb.Black);
}
