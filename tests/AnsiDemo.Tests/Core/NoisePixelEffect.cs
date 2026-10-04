using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Core;

/// <summary>Runner test effect: fills the buffer with noise that changes on every Update and depends on the seed.</summary>
[Effect("test-noise", PixelMode.HalfBlock)]
public sealed class NoisePixelEffect : IPixelEffect
{
    private DemoRandom _rng = new(1);
    private uint _state;

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        _rng = rng;
        _state = rng.NextUInt();
    }

    public void Update(FrameContext ctx) => _state = _rng.NextUInt();

    public void Render(PixelBuffer pixels)
    {
        for (int y = 0; y < pixels.Height; y++)
        {
            for (int x = 0; x < pixels.Width; x++)
            {
                uint h = DemoRandom.Hash(x, y) ^ _state;
                pixels[x, y] = new Rgb((byte)h, (byte)(h >> 8), (byte)(h >> 16));
            }
        }
    }
}
