using AnsiDemo.Contracts;
using AnsiDemo.Transitions;

namespace AnsiDemo.Tests.Transitions;

public class WipeTransitionTests
{
    private const int Cols = 10;
    private const int Rows = 3;

    [Theory]
    [InlineData(0.05, 0)]
    [InlineData(0.1, 1)]
    [InlineData(0.25, 2)]
    [InlineData(0.5, 5)]
    [InlineData(0.79, 7)]
    [InlineData(0.99, 9)]
    public void ShowsToLeftOfFloorProgressTimesCols(double progress, int edge)
    {
        Frame from = TestFrames.From(Cols, Rows);
        Frame to = TestFrames.To(Cols, Rows);
        Frame output = TestFrames.Output(Cols, Rows);

        new WipeTransition().Apply(from, to, progress, output);

        TestFrames.AssertCells(output, (x, y) => x < edge ? to[x, y] : from[x, y]);
    }

    [Fact]
    public void EdgeIsTheSameOnEveryRow()
    {
        Frame from = TestFrames.From(80, 24);
        Frame to = TestFrames.To(80, 24);
        Frame output = TestFrames.Output(80, 24);

        new WipeTransition().Apply(from, to, 0.5, output);

        for (int y = 0; y < 24; y++)
        {
            Assert.Equal(to[39, y], output[39, y]);
            Assert.Equal(from[40, y], output[40, y]);
        }
    }
}
