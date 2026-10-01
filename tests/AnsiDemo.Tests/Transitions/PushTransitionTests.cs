using AnsiDemo.Contracts;
using AnsiDemo.Transitions;

namespace AnsiDemo.Tests.Transitions;

public class PushTransitionTests
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
    public void ShiftsFromOutLeftAndToInRight(double progress, int shift)
    {
        Frame from = TestFrames.From(Cols, Rows);
        Frame to = TestFrames.To(Cols, Rows);
        Frame output = TestFrames.Output(Cols, Rows);

        new PushTransition().Apply(from, to, progress, output);

        TestFrames.AssertCells(output, (x, y) =>
            x < Cols - shift ? from[x + shift, y] : to[x - (Cols - shift), y]);
    }

    [Fact]
    public void QuarterWayShowsRightOfFromThenLeftOfTo()
    {
        Frame from = TestFrames.From(Cols, Rows);
        Frame to = TestFrames.To(Cols, Rows);
        Frame output = TestFrames.Output(Cols, Rows);

        new PushTransition().Apply(from, to, 0.25, output);

        for (int y = 0; y < Rows; y++)
        {
            Assert.Equal(from[2, y], output[0, y]);
            Assert.Equal(from[9, y], output[7, y]);
            Assert.Equal(to[0, y], output[8, y]);
            Assert.Equal(to[1, y], output[9, y]);
        }
    }
}
