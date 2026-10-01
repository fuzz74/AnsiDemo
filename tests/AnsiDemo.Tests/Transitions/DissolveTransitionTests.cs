using AnsiDemo.Contracts;
using AnsiDemo.Transitions;

namespace AnsiDemo.Tests.Transitions;

public class DissolveTransitionTests
{
    [Theory]
    [InlineData(0.01)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.75)]
    [InlineData(0.99)]
    public void ShowsToWhereHashIsBelowProgress(double progress)
    {
        Frame from = TestFrames.From(16, 6);
        Frame to = TestFrames.To(16, 6);
        Frame output = TestFrames.Output(16, 6);

        new DissolveTransition().Apply(from, to, progress, output);

        TestFrames.AssertCells(output, (x, y) =>
            DemoRandom.Hash(x, y) / 4294967296.0 < progress ? to[x, y] : from[x, y]);
    }

    [Fact]
    public void UsesTheHashOfEachCell()
    {
        // Hash(0, 0) = 0 is below any positive progress; Hash(1, 0) = 2182377942 is about 0.508.
        var from = new Frame(2, 1) { [0, 0] = new Cell('a', Rgb.Black, Rgb.Black), [1, 0] = new Cell('b', Rgb.Black, Rgb.Black) };
        var to = new Frame(2, 1) { [0, 0] = new Cell('A', Rgb.White, Rgb.White), [1, 0] = new Cell('B', Rgb.White, Rgb.White) };
        var output = new Frame(2, 1);
        var dissolve = new DissolveTransition();

        dissolve.Apply(from, to, 0.5, output);
        Assert.Equal(to[0, 0], output[0, 0]);
        Assert.Equal(from[1, 0], output[1, 0]);

        dissolve.Apply(from, to, 0.51, output);
        Assert.Equal(to[0, 0], output[0, 0]);
        Assert.Equal(to[1, 0], output[1, 0]);
    }

    [Fact]
    public void RevealedCellsGrowWithProgressAndSpreadEvenly()
    {
        Frame from = TestFrames.From(80, 24);
        Frame to = TestFrames.To(80, 24);
        var earlier = new Frame(80, 24);
        var later = new Frame(80, 24);
        var dissolve = new DissolveTransition();

        dissolve.Apply(from, to, 0.3, earlier);
        dissolve.Apply(from, to, 0.6, later);

        int revealedEarlier = 0;
        int revealedLater = 0;
        for (int y = 0; y < 24; y++)
        {
            for (int x = 0; x < 80; x++)
            {
                bool earlierTo = earlier[x, y] == to[x, y];
                bool laterTo = later[x, y] == to[x, y];
                Assert.True(!earlierTo || laterTo, $"Cell ({x}, {y}) reverted to from.");
                revealedEarlier += earlierTo ? 1 : 0;
                revealedLater += laterTo ? 1 : 0;
            }
        }
        Assert.InRange(revealedEarlier, 0.25 * 1920, 0.35 * 1920);
        Assert.InRange(revealedLater, 0.55 * 1920, 0.65 * 1920);
    }
}
