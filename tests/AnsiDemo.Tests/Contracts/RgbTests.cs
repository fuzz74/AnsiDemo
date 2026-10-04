using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Contracts;

public class RgbTests
{
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255)]
    [InlineData(255, 0, 0, 76)]
    [InlineData(0, 255, 0, 149)]
    [InlineData(0, 0, 255, 29)]
    public void LumaUsesIntegerWeights(byte r, byte g, byte b, int expected)
    {
        Assert.Equal(expected, new Rgb(r, g, b).Luma);
    }

    [Fact]
    public void LerpReturnsEndsExactly()
    {
        var a = new Rgb(10, 200, 33);
        var b = new Rgb(250, 3, 129);
        Assert.Equal(a, Rgb.Lerp(a, b, 0));
        Assert.Equal(b, Rgb.Lerp(a, b, 1));
    }

    [Fact]
    public void LerpClampsT()
    {
        var a = new Rgb(10, 20, 30);
        var b = new Rgb(200, 100, 0);
        Assert.Equal(a, Rgb.Lerp(a, b, -0.5));
        Assert.Equal(b, Rgb.Lerp(a, b, 7));
    }

    [Fact]
    public void LerpRoundsHalfAwayFromZero()
    {
        Assert.Equal(new Rgb(128, 128, 1), Rgb.Lerp(new Rgb(0, 255, 0), new Rgb(255, 0, 1), 0.5));
        Assert.Equal(new Rgb(64, 191, 3), Rgb.Lerp(new Rgb(0, 255, 0), new Rgb(255, 0, 10), 0.25));
    }

    [Theory]
    [InlineData(0, 255, 0, 0)]
    [InlineData(60, 255, 255, 0)]
    [InlineData(120, 0, 255, 0)]
    [InlineData(180, 0, 255, 255)]
    [InlineData(240, 0, 0, 255)]
    [InlineData(300, 255, 0, 255)]
    [InlineData(30, 255, 128, 0)]
    public void FromHsvGivesPrimariesAndSecondaries(double hue, byte r, byte g, byte b)
    {
        Assert.Equal(new Rgb(r, g, b), Rgb.FromHsv(hue, 1, 1));
    }

    [Theory]
    [InlineData(360, 0)]
    [InlineData(480, 120)]
    [InlineData(-120, 240)]
    [InlineData(-30, 330)]
    [InlineData(-720, 0)]
    public void FromHsvWrapsHue(double hue, double equivalent)
    {
        Assert.Equal(Rgb.FromHsv(equivalent, 1, 1), Rgb.FromHsv(hue, 1, 1));
    }

    [Fact]
    public void FromHsvClampsSaturationAndValue()
    {
        Assert.Equal(new Rgb(255, 0, 0), Rgb.FromHsv(0, 2, 5));
        Assert.Equal(Rgb.Black, Rgb.FromHsv(0, 1, -1));
        Assert.Equal(Rgb.White, Rgb.FromHsv(200, -3, 1));
    }

    [Fact]
    public void FromHsvRoundsToBytes()
    {
        Assert.Equal(new Rgb(128, 128, 128), Rgb.FromHsv(90, 0, 0.5));
    }
}
