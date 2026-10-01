using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Transitions;

/// <summary>Frames whose cells differ by position and between "from" and "to", so every source cell is identifiable.</summary>
internal static class TestFrames
{
    public static Frame From(int cols, int rows) =>
        Build(cols, rows, (x, y) => new Cell(
            (char)('a' + (x + 3 * y) % 26),
            new Rgb((byte)(10 * x), (byte)(20 * y), 50),
            new Rgb(200, (byte)x, (byte)(7 * y))));

    public static Frame To(int cols, int rows) =>
        Build(cols, rows, (x, y) => new Cell(
            (char)('A' + (x + 5 * y) % 26),
            new Rgb((byte)(255 - x), 100, (byte)(5 * y)),
            new Rgb((byte)(3 * x), (byte)(255 - y), 7)));

    public static Frame Output(int cols, int rows) => new(cols, rows);

    /// <summary>Asserts that every cell of actual equals expected(x, y), naming the first differing cell.</summary>
    public static void AssertCells(Frame actual, Func<int, int, Cell> expected)
    {
        for (int y = 0; y < actual.Rows; y++)
        {
            for (int x = 0; x < actual.Cols; x++)
            {
                Cell want = expected(x, y);
                Cell got = actual[x, y];
                Assert.True(want == got, $"Cell ({x}, {y}): expected {want}, got {got}.");
            }
        }
    }

    public static void AssertSame(Frame expected, Frame actual)
    {
        Assert.Equal(expected.Cols, actual.Cols);
        Assert.Equal(expected.Rows, actual.Rows);
        AssertCells(actual, (x, y) => expected[x, y]);
    }

    private static Frame Build(int cols, int rows, Func<int, int, Cell> cell)
    {
        var frame = new Frame(cols, rows);
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                frame[x, y] = cell(x, y);
            }
        }
        return frame;
    }
}
