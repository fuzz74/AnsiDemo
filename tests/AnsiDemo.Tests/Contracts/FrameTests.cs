using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Contracts;

public class FrameTests
{
    private static readonly Rgb Red = new(255, 0, 0);
    private static readonly Rgb Blue = new(0, 0, 255);

    [Fact]
    public void StartsBlank()
    {
        var frame = new Frame(3, 2);
        Assert.Equal(3, frame.Cols);
        Assert.Equal(2, frame.Rows);
        for (int y = 0; y < 2; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                Assert.Equal(Cell.Blank, frame[x, y]);
            }
        }
        Assert.Equal(new Cell(' ', Rgb.White, Rgb.Black), Cell.Blank);
    }

    [Fact]
    public void OutsideGetsBlankAndIgnoresSets()
    {
        var frame = new Frame(2, 2);
        frame[-1, 0] = new Cell('x', Red, Blue);
        frame[2, 1] = new Cell('x', Red, Blue);
        frame[0, 2] = new Cell('x', Red, Blue);
        Assert.Equal(Cell.Blank, frame[-1, 0]);
        Assert.Equal(Cell.Blank, frame[2, 1]);
        Assert.Equal("\n\n", frame.ToText());
    }

    [Fact]
    public void ClearResetsToBlank()
    {
        var frame = new Frame(2, 1);
        frame.Write(0, 0, "ab", Red, Blue);
        frame.Clear();
        Assert.Equal(Cell.Blank, frame[0, 0]);
        Assert.Equal(Cell.Blank, frame[1, 0]);
    }

    [Fact]
    public void WriteClipsAtEdges()
    {
        var frame = new Frame(4, 2);
        frame.Write(-2, 0, "abcd", Red, Blue);
        frame.Write(2, 1, "xyz", Red, Blue);
        frame.Write(0, 5, "nope", Red, Blue);
        Assert.Equal("cd\n  xy\n", frame.ToText());
        Assert.Equal(new Cell('c', Red, Blue), frame[0, 0]);
        Assert.Equal(Cell.Blank, frame[2, 0]);
        Assert.Equal(new Cell('y', Red, Blue), frame[3, 1]);
    }

    [Fact]
    public void CopyFromCopiesCells()
    {
        var source = new Frame(3, 2);
        source.Write(1, 1, "hi", Red, Blue);
        var target = new Frame(3, 2);
        target.Write(0, 0, "zz", Blue, Red);
        target.CopyFrom(source);
        Assert.Equal(source.ToAnsi(), target.ToAnsi());
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(3, 3)]
    public void CopyFromThrowsOnSizeMismatch(int cols, int rows)
    {
        var target = new Frame(3, 2);
        Assert.Throws<ArgumentException>(() => target.CopyFrom(new Frame(cols, rows)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, -1)]
    public void NonPositiveSizeThrows(int cols, int rows)
    {
        Assert.Throws<ArgumentException>(() => new Frame(cols, rows));
    }

    [Fact]
    public void LitCountCountsVisibleCharacters()
    {
        var frame = new Frame(6, 1);
        Assert.Equal(0, frame.LitCount());
        frame[0, 0] = new Cell('a', Red, Rgb.Black);
        frame[1, 0] = new Cell('b', Rgb.Black, Blue);
        frame[2, 0] = new Cell('c', Rgb.Black, Rgb.Black);
        frame[3, 0] = new Cell(' ', Red, Blue);
        frame[4, 0] = new Cell('▀', Red, Red);
        Assert.Equal(3, frame.LitCount());
    }

    [Fact]
    public void ToAnsiEmitsColourOnlyOnChangesWithinARow()
    {
        var frame = new Frame(3, 2);
        frame.Write(0, 0, "ab", Red, Rgb.Black);
        frame[1, 1] = new Cell('x', Rgb.White, Blue);

        const string expected =
            "\e[38;2;255;0;0m\e[48;2;0;0;0mab\e[38;2;255;255;255m \e[0m\n" +
            "\e[38;2;255;255;255m\e[48;2;0;0;0m \e[48;2;0;0;255mx\e[48;2;0;0;0m \e[0m\n";
        Assert.Equal(expected, frame.ToAnsi());
    }

    [Fact]
    public void ToAnsiOfBlankFrameRepeatsColoursAtEachRowStart()
    {
        var frame = new Frame(2, 2);
        const string row = "\e[38;2;255;255;255m\e[48;2;0;0;0m  \e[0m\n";
        Assert.Equal(row + row, frame.ToAnsi());
    }

    [Fact]
    public void ToTextTrimsTrailingSpacesAndEndsWithNewline()
    {
        var frame = new Frame(5, 3);
        frame.Write(1, 0, "a b", Red, Blue);
        frame.Write(0, 2, "xyz  ", Red, Blue);
        Assert.Equal(" a b\n\nxyz\n", frame.ToText());
    }
}
