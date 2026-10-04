using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Contracts;

public class PixelBufferTests
{
    [Fact]
    public void StartsBlackWithRowMajorPixels()
    {
        var buffer = new PixelBuffer(4, 3);
        Assert.Equal(4, buffer.Width);
        Assert.Equal(3, buffer.Height);
        Assert.Equal(12, buffer.Pixels.Length);
        Assert.All(buffer.Pixels, p => Assert.Equal(Rgb.Black, p));

        var color = new Rgb(1, 2, 3);
        buffer[2, 1] = color;
        Assert.Equal(color, buffer.Pixels[1 * 4 + 2]);
        Assert.Equal(color, buffer[2, 1]);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(4, 0)]
    [InlineData(0, 3)]
    [InlineData(100, 100)]
    public void OutsideGetsBlackAndIgnoresSets(int x, int y)
    {
        var buffer = new PixelBuffer(4, 3);
        buffer.Clear(Rgb.White);
        buffer[x, y] = new Rgb(9, 9, 9);
        Assert.Equal(Rgb.Black, buffer[x, y]);
        Assert.All(buffer.Pixels, p => Assert.Equal(Rgb.White, p));
    }

    [Fact]
    public void ClearFillsEveryPixel()
    {
        var buffer = new PixelBuffer(5, 2);
        buffer[1, 1] = Rgb.White;
        var color = new Rgb(40, 50, 60);
        buffer.Clear(color);
        Assert.All(buffer.Pixels, p => Assert.Equal(color, p));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-2, 5)]
    public void NonPositiveSizeThrows(int width, int height)
    {
        Assert.Throws<ArgumentException>(() => new PixelBuffer(width, height));
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24, 80, 48)]
    [InlineData(PixelMode.Braille, 80, 24, 160, 96)]
    [InlineData(PixelMode.HalfBlock, 200, 60, 200, 120)]
    [InlineData(PixelMode.Braille, 200, 60, 400, 240)]
    public void SizeForMatchesMode(PixelMode mode, int cols, int rows, int width, int height)
    {
        Assert.Equal((width, height), PixelBuffer.SizeFor(mode, cols, rows));
    }
}
