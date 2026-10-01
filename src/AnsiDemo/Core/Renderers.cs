using AnsiDemo.Contracts;

namespace AnsiDemo.Core;

public static class Renderers
{
    public static void Render(PixelMode mode, PixelBuffer pixels, Frame frame)
    {
        switch (mode)
        {
            case PixelMode.HalfBlock:
                HalfBlockRenderer.Render(pixels, frame);
                break;
            case PixelMode.Braille:
                BrailleRenderer.Render(pixels, frame);
                break;
            default:
                throw new ArgumentException($"Unknown pixel mode {mode}.", nameof(mode));
        }
    }
}
