using AnsiDemo.Contracts;
using AnsiDemo.Core;

namespace AnsiDemo.Tests.Core;

public class RendererTests
{
    private static readonly int[,] Bayer4 = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };

    [Fact]
    public void HalfBlockTakesUpperPixelAsFgAndLowerAsBg()
    {
        var pixels = new PixelBuffer(3, 4);
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                pixels[x, y] = new Rgb((byte)(10 * x), (byte)(20 * y), 7);
            }
        }
        var frame = new Frame(3, 2);

        HalfBlockRenderer.Render(pixels, frame);

        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                Assert.Equal(new Cell('▀', pixels[x, 2 * y], pixels[x, 2 * y + 1]), frame[x, y]);
            }
        }
    }

    [Theory]
    [InlineData(0, 0, 0x01)]
    [InlineData(0, 1, 0x02)]
    [InlineData(0, 2, 0x04)]
    [InlineData(1, 0, 0x08)]
    [InlineData(1, 1, 0x10)]
    [InlineData(1, 2, 0x20)]
    [InlineData(0, 3, 0x40)]
    [InlineData(1, 3, 0x80)]
    public void BrailleDotSetsItsBit(int dx, int dy, int bit)
    {
        // Cell (1, 1) of a 2x2 frame, so the pixel offsets of the cell are exercised too.
        var pixels = new PixelBuffer(4, 8);
        pixels[2 + dx, 4 + dy] = Rgb.White;
        var frame = new Frame(2, 2);

        BrailleRenderer.Render(pixels, frame);

        Assert.Equal(new Cell((char)(0x2800 + bit), Rgb.White, Rgb.Black), frame[1, 1]);
        Assert.Equal(Cell.Blank, frame[0, 0]);
        Assert.Equal(Cell.Blank, frame[1, 0]);
        Assert.Equal(Cell.Blank, frame[0, 1]);
    }

    [Fact]
    public void BrailleAllDotsOnIsFullCharacter()
    {
        var pixels = new PixelBuffer(2, 4);
        pixels.Clear(Rgb.White);
        var frame = new Frame(1, 1);

        BrailleRenderer.Render(pixels, frame);

        Assert.Equal(new Cell('⣿', Rgb.White, Rgb.Black), frame[0, 0]);
    }

    [Fact]
    public void BrailleDotIsOnOnlyAboveTheBayerThreshold()
    {
        // A 2x1 frame covers 4x4 pixels: every Bayer4 position once.
        for (int py = 0; py < 4; py++)
        {
            for (int px = 0; px < 4; px++)
            {
                int threshold = 16 * Bayer4[py, px];
                if (threshold > 0)
                {
                    Assert.Equal(Cell.Blank, RenderSingleGrey(px, py, (byte)threshold));
                }
                Assert.NotEqual(Cell.Blank, RenderSingleGrey(px, py, (byte)(threshold + 1)));
            }
        }
    }

    [Theory]
    [InlineData(0, 0, 0, false)]
    [InlineData(0, 0, 1, true)]
    [InlineData(1, 0, 128, false)]
    [InlineData(1, 0, 129, true)]
    [InlineData(0, 3, 240, false)]
    [InlineData(0, 3, 241, true)]
    public void BrailleThresholdEdgeIsOff(int px, int py, int grey, bool on)
    {
        Cell cell = RenderSingleGrey(px, py, (byte)grey);
        Assert.Equal(on, cell != Cell.Blank);
    }

    [Fact]
    public void BrailleFgIsTheIntegerMeanOfTheOnPixels()
    {
        var pixels = new PixelBuffer(2, 4);
        pixels[0, 0] = new Rgb(250, 100, 31);  // Luma 136 > 0: on
        pixels[0, 2] = new Rgb(201, 200, 0);   // Luma 177 > 48: on
        pixels[1, 0] = new Rgb(100, 100, 100); // Luma 100 <= 128: off, not part of the mean
        var frame = new Frame(1, 1);

        BrailleRenderer.Render(pixels, frame);

        Assert.Equal(new Cell((char)(0x2800 + 0x01 + 0x04), new Rgb(225, 150, 15), Rgb.Black), frame[0, 0]);
    }

    [Fact]
    public void BrailleCellWithoutDotsIsBlank()
    {
        var pixels = new PixelBuffer(2, 4);
        pixels[1, 0] = new Rgb(100, 100, 100);
        var frame = new Frame(1, 1);
        frame[0, 0] = new Cell('x', Rgb.Black, Rgb.White);

        BrailleRenderer.Render(pixels, frame);

        Assert.Equal(Cell.Blank, frame[0, 0]);
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock)]
    [InlineData(PixelMode.Braille)]
    public void DispatcherUsesTheRendererOfTheMode(PixelMode mode)
    {
        (int width, int height) = PixelBuffer.SizeFor(mode, 5, 3);
        var pixels = new PixelBuffer(width, height);
        for (int i = 0; i < pixels.Pixels.Length; i++)
        {
            pixels.Pixels[i] = Rgb.FromHsv(37.0 * i, 1.0, (i % 5) / 4.0);
        }
        var expected = new Frame(5, 3);
        var actual = new Frame(5, 3);

        if (mode == PixelMode.HalfBlock)
        {
            HalfBlockRenderer.Render(pixels, expected);
        }
        else
        {
            BrailleRenderer.Render(pixels, expected);
        }
        Renderers.Render(mode, pixels, actual);

        Assert.Equal(expected.ToAnsi(), actual.ToAnsi());
    }

    [Theory]
    [InlineData(8, 12)] // Braille size
    [InlineData(3, 6)]  // one column short
    [InlineData(4, 5)]  // one row short
    [InlineData(4, 3)]  // the size of the frame itself
    public void HalfBlockRejectsWrongBufferSize(int width, int height)
    {
        var frame = new Frame(4, 3);
        Assert.Throws<ArgumentException>(() => HalfBlockRenderer.Render(new PixelBuffer(width, height), frame));
        Assert.Throws<ArgumentException>(() => Renderers.Render(PixelMode.HalfBlock, new PixelBuffer(width, height), frame));
    }

    [Theory]
    [InlineData(4, 6)]   // half-block size
    [InlineData(7, 12)]  // one column short
    [InlineData(8, 11)]  // one row short
    [InlineData(9, 12)]  // one column too many
    public void BrailleRejectsWrongBufferSize(int width, int height)
    {
        var frame = new Frame(4, 3);
        Assert.Throws<ArgumentException>(() => BrailleRenderer.Render(new PixelBuffer(width, height), frame));
        Assert.Throws<ArgumentException>(() => Renderers.Render(PixelMode.Braille, new PixelBuffer(width, height), frame));
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 400, 120)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void RenderingDoesNotAllocate(PixelMode mode, int cols, int rows)
    {
        (int width, int height) = PixelBuffer.SizeFor(mode, cols, rows);
        var pixels = new PixelBuffer(width, height);
        for (int i = 0; i < pixels.Pixels.Length; i++)
        {
            pixels.Pixels[i] = Rgb.FromHsv(i, 1.0, (i % 7) / 6.0);
        }
        var frame = new Frame(cols, rows);
        Renderers.Render(mode, pixels, frame);

        long before = GC.GetAllocatedBytesForCurrentThread();
        Renderers.Render(mode, pixels, frame);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    private static Cell RenderSingleGrey(int px, int py, byte grey)
    {
        var pixels = new PixelBuffer(4, 4);
        pixels[px, py] = new Rgb(grey, grey, grey);
        var frame = new Frame(2, 1);
        BrailleRenderer.Render(pixels, frame);
        return frame[px / 2, 0];
    }
}
