using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Demo;

/// <summary>A pixel effect whose colours depend on position and time, rendered by <see cref="HeadlessHostTests"/>.</summary>
[Effect(HeadlessHostTests.TestEffect, PixelMode.HalfBlock)]
public sealed class HeadlessHostTestEffect : IPixelEffect
{
    private double _time;

    public void Reset(FrameContext ctx, DemoRandom rng) => _time = ctx.Time;

    public void Update(FrameContext ctx) => _time = ctx.Time;

    public void Render(PixelBuffer pixels)
    {
        int shift = (int)Math.Round(_time * 40);
        for (int y = 0; y < pixels.Height; y++)
        {
            for (int x = 0; x < pixels.Width; x++)
            {
                pixels[x, y] = new Rgb((byte)(x * 3 + shift), (byte)(y * 5), (byte)(x + y + shift / 2));
            }
        }
    }
}
