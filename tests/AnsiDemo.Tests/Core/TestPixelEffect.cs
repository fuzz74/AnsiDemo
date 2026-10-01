using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Core;

/// <summary>Catalog test effect: fills the buffer with a grey that brightens with time.</summary>
[Effect("test-pixel", PixelMode.Braille)]
public sealed class TestPixelEffect : IPixelEffect
{
    private double _time;

    public void Reset(FrameContext ctx, DemoRandom rng) => _time = ctx.Time;

    public void Update(FrameContext ctx) => _time = ctx.Time;

    public void Render(PixelBuffer pixels)
    {
        byte level = (byte)Math.Min(255.0, 128.0 + 32.0 * _time);
        pixels.Clear(new Rgb(level, level, level));
    }
}
