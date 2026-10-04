using AnsiDemo.Contracts;

namespace AnsiDemo.Core;

/// <summary>One cell per 1x2 pixels: '▀' with the upper pixel as Fg and the lower pixel as Bg.</summary>
public static class HalfBlockRenderer
{
    private const char UpperHalfBlock = '▀';

    public static void Render(PixelBuffer pixels, Frame frame)
    {
        (int width, int height) = PixelBuffer.SizeFor(PixelMode.HalfBlock, frame.Cols, frame.Rows);
        if (pixels.Width != width || pixels.Height != height)
        {
            throw new ArgumentException(
                $"Half-block rendering of {frame.Cols}x{frame.Rows} cells needs {width}x{height} pixels, got {pixels.Width}x{pixels.Height}.",
                nameof(pixels));
        }

        Rgb[] source = pixels.Pixels;
        for (int y = 0; y < frame.Rows; y++)
        {
            int upper = 2 * y * width;
            int lower = upper + width;
            for (int x = 0; x < frame.Cols; x++)
            {
                frame[x, y] = new Cell(UpperHalfBlock, source[upper + x], source[lower + x]);
            }
        }
    }
}
