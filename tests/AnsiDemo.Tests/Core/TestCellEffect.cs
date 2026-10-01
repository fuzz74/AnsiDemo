using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Core;

/// <summary>Catalog test effect: writes "TEST" in the top-left corner.</summary>
[Effect("test-cell", PixelMode.HalfBlock)]
public sealed class TestCellEffect : ICellEffect
{
    public void Reset(FrameContext ctx, DemoRandom rng)
    {
    }

    public void Update(FrameContext ctx)
    {
    }

    public void Render(Frame frame) => frame.Write(0, 0, "TEST", Rgb.White, Rgb.Black);
}
