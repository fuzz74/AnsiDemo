using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Demo;

/// <summary>Registered effect name that <see cref="AppOptionsParserTests"/> passes to <c>--effect</c>.</summary>
[Effect(AppOptionsParserTests.TestEffect, PixelMode.HalfBlock)]
public sealed class AppOptionsTestEffect : ICellEffect
{
    public void Reset(FrameContext ctx, DemoRandom rng)
    {
    }

    public void Update(FrameContext ctx)
    {
    }

    public void Render(Frame frame) => frame.Write(0, 0, "OPTIONS", Rgb.White, Rgb.Black);
}
