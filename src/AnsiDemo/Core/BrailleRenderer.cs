using AnsiDemo.Contracts;

namespace AnsiDemo.Core;

/// <summary>
/// One cell per 2x4 pixels: a Braille character whose dots are the pixels brighter than an ordered-dither threshold,
/// drawn in the mean colour of those pixels on black.
/// </summary>
public static class BrailleRenderer
{
    private const int BrailleBase = 0x2800;

    // Bayer4[py & 3][px & 3], flattened row by row.
    private static ReadOnlySpan<byte> Bayer4 => [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5];

    // Bit of dot (dx, dy) at index dy * 2 + dx.
    private static ReadOnlySpan<byte> DotBits => [0x01, 0x08, 0x02, 0x10, 0x04, 0x20, 0x40, 0x80];

    public static void Render(PixelBuffer pixels, Frame frame)
    {
        (int width, int height) = PixelBuffer.SizeFor(PixelMode.Braille, frame.Cols, frame.Rows);
        if (pixels.Width != width || pixels.Height != height)
        {
            throw new ArgumentException(
                $"Braille rendering of {frame.Cols}x{frame.Rows} cells needs {width}x{height} pixels, got {pixels.Width}x{pixels.Height}.",
                nameof(pixels));
        }

        Rgb[] source = pixels.Pixels;
        ReadOnlySpan<byte> bayer = Bayer4;
        ReadOnlySpan<byte> dotBits = DotBits;
        for (int y = 0; y < frame.Rows; y++)
        {
            for (int x = 0; x < frame.Cols; x++)
            {
                int bits = 0, count = 0, r = 0, g = 0, b = 0;
                for (int dy = 0; dy < 4; dy++)
                {
                    int py = 4 * y + dy;
                    int row = py * width;
                    int bayerRow = (py & 3) * 4;
                    for (int dx = 0; dx < 2; dx++)
                    {
                        int px = 2 * x + dx;
                        Rgb pixel = source[row + px];
                        if (pixel.Luma > 16 * bayer[bayerRow + (px & 3)])
                        {
                            bits |= dotBits[dy * 2 + dx];
                            count++;
                            r += pixel.R;
                            g += pixel.G;
                            b += pixel.B;
                        }
                    }
                }

                frame[x, y] = count == 0
                    ? Cell.Blank
                    : new Cell((char)(BrailleBase + bits), new Rgb((byte)(r / count), (byte)(g / count), (byte)(b / count)), Rgb.Black);
            }
        }
    }
}
