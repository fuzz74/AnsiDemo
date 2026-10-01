using AnsiDemo.Contracts;
using AnsiDemo.Transitions;

namespace AnsiDemo.Tests.Transitions;

public class CrossfadeTransitionTests
{
    private const int Cols = 9;
    private const int Rows = 4;

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.25)]
    [InlineData(0.4999)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(0.9)]
    public void BlendsColoursAndSwitchesCharacterAtHalf(double progress)
    {
        Frame from = TestFrames.From(Cols, Rows);
        Frame to = TestFrames.To(Cols, Rows);
        Frame output = TestFrames.Output(Cols, Rows);

        new CrossfadeTransition().Apply(from, to, progress, output);

        TestFrames.AssertCells(output, (x, y) => new Cell(
            progress >= 0.5 ? to[x, y].Ch : from[x, y].Ch,
            Rgb.Lerp(from[x, y].Fg, to[x, y].Fg, progress),
            Rgb.Lerp(from[x, y].Bg, to[x, y].Bg, progress)));
    }

    [Fact]
    public void MidpointBlendsToKnownColours()
    {
        var from = new Frame(1, 1) { [0, 0] = new Cell('f', new Rgb(0, 100, 255), new Rgb(10, 20, 30)) };
        var to = new Frame(1, 1) { [0, 0] = new Cell('t', new Rgb(255, 200, 0), new Rgb(30, 20, 10)) };
        var output = new Frame(1, 1);

        new CrossfadeTransition().Apply(from, to, 0.5, output);

        Assert.Equal(new Cell('t', new Rgb(128, 150, 128), new Rgb(20, 20, 20)), output[0, 0]);
    }

    [Fact]
    public void KeepsFromCharacterJustBelowHalf()
    {
        var from = new Frame(1, 1) { [0, 0] = new Cell('f', Rgb.Black, Rgb.Black) };
        var to = new Frame(1, 1) { [0, 0] = new Cell('t', Rgb.White, Rgb.White) };
        var output = new Frame(1, 1);

        new CrossfadeTransition().Apply(from, to, 0.25, output);

        Assert.Equal(new Cell('f', new Rgb(64, 64, 64), new Rgb(64, 64, 64)), output[0, 0]);
    }
}
