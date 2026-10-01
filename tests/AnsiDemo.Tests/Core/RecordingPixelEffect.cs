using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Core;

/// <summary>Runner test effect: logs the colour it finds at pixel (0, 0), then paints that pixel with <see cref="Color"/>.</summary>
[Effect("test-rec-pixel", PixelMode.HalfBlock)]
public sealed class RecordingPixelEffect : RecordingEffect, IPixelEffect
{
    public Rgb Color => new((byte)(100 + Id), 50, 25);

    public void Render(PixelBuffer pixels)
    {
        TestLog.Renders.Add($"{Id} pixel saw {Format(pixels[0, 0])}");
        pixels[0, 0] = Color;
    }
}
