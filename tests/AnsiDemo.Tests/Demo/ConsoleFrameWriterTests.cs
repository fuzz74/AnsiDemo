using System.Text;
using AnsiDemo.Contracts;
using AnsiDemo.Shell;

namespace AnsiDemo.Tests.Demo;

public class ConsoleFrameWriterTests
{
    private static readonly Rgb Red = new(255, 0, 0), Blue = new(0, 0, 255);

    [Fact]
    public void SmallFrameIsWrittenWithCursorPerRowAndColoursOnlyOnChange()
    {
        var frame = new Frame(3, 2);
        frame[0, 0] = new Cell('A', Red, Rgb.Black);
        frame[1, 0] = new Cell('B', Red, Blue);
        frame[2, 0] = new Cell('C', Red, Blue);
        frame[0, 1] = new Cell('D', Red, Blue);
        frame[1, 1] = new Cell('E', Rgb.White, Blue);
        frame[2, 1] = new Cell('F', Rgb.White, Blue);

        var output = new StringBuilder();
        ConsoleFrameWriter.Append(output, frame);

        Assert.Equal(
            "\e[1;1H\e[38;2;255;0;0m\e[48;2;0;0;0mA\e[48;2;0;0;255mBC" +
            "\e[2;1H\e[38;2;255;0;0m\e[48;2;0;0;255mD\e[38;2;255;255;255mEF" +
            "\e[0m",
            output.ToString());
    }

    [Fact]
    public void EveryRowStartsWithBothColoursEvenWhenUnchanged()
    {
        var output = new StringBuilder();
        ConsoleFrameWriter.Append(output, new Frame(2, 3));

        const string Row = "\e[38;2;255;255;255m\e[48;2;0;0;0m  ";
        Assert.Equal("\e[1;1H" + Row + "\e[2;1H" + Row + "\e[3;1H" + Row + "\e[0m", output.ToString());
    }

    [Fact]
    public void FrameNeverClearsTheScreenOrWritesANewline()
    {
        var frame = new Frame(80, 24);
        for (int y = 0; y < frame.Rows; y++)
        {
            for (int x = 0; x < frame.Cols; x++)
            {
                frame[x, y] = new Cell('▀', new Rgb((byte)x, (byte)y, 7), new Rgb((byte)y, (byte)x, 9));
            }
        }

        var output = new StringBuilder();
        ConsoleFrameWriter.Append(output, frame);
        string text = output.ToString();

        Assert.DoesNotContain('\n', text);
        Assert.DoesNotContain('\r', text);
        Assert.DoesNotContain("\e[2J", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\e[J", text, StringComparison.Ordinal);
        Assert.StartsWith("\e[1;1H", text, StringComparison.Ordinal);
        Assert.EndsWith("▀\e[0m", text, StringComparison.Ordinal);
        for (int row = 1; row <= frame.Rows; row++)
        {
            Assert.Contains($"\e[{row};1H", text, StringComparison.Ordinal);
        }
        Assert.Equal(frame.Cols * frame.Rows, text.Count(ch => ch == '▀'));
    }

    [Fact]
    public void FrameIsAppendedAfterExistingText()
    {
        var output = new StringBuilder("before");
        ConsoleFrameWriter.Append(output, new Frame(1, 1));

        Assert.Equal("before\e[1;1H\e[38;2;255;255;255m\e[48;2;0;0;0m \e[0m", output.ToString());
    }

    [Theory]
    [InlineData(79, 24, "Window too small: need 80x24, have 79x24")]
    [InlineData(120, 5, "Window too small: need 80x24, have 120x5")]
    [InlineData(0, 0, "Window too small: need 80x24, have 0x0")]
    public void TooSmallMessageNamesBothSizes(int cols, int rows, string expected)
    {
        Assert.Equal(expected, ConsoleFrameWriter.TooSmallMessage(cols, rows));
    }

    [Fact]
    public void TooSmallMessageIsDrawnOnRowZeroAndTheRestIsErased()
    {
        var output = new StringBuilder();
        ConsoleFrameWriter.AppendTooSmall(output, 79, 24);

        Assert.Equal("\e[0m\e[1;1HWindow too small: need 80x24, have 79x24\e[K\e[2;1H\e[J", output.ToString());
    }

    [Fact]
    public void TooSmallMessageIsClippedToTheWidth()
    {
        var output = new StringBuilder();
        ConsoleFrameWriter.AppendTooSmall(output, 20, 5);

        Assert.Equal("\e[0m\e[1;1HWindow too small: ne\e[2;1H\e[J", output.ToString());
    }

    [Fact]
    public void TooSmallMessageThatFillsTheRowKeepsItsLastCharacter()
    {
        var output = new StringBuilder();
        ConsoleFrameWriter.AppendTooSmall(output, 40, 10);

        Assert.Equal("\e[0m\e[1;1HWindow too small: need 80x24, have 40x10\e[2;1H\e[J", output.ToString());
    }

    [Fact]
    public void TooSmallMessageOnOneRowErasesOnlyThatRow()
    {
        var output = new StringBuilder();
        ConsoleFrameWriter.AppendTooSmall(output, 60, 1);

        Assert.Equal("\e[0m\e[1;1HWindow too small: need 80x24, have 60x1\e[K", output.ToString());
    }

    [Fact]
    public void TooSmallMessageHasNoNewlineAndNoScreenClear()
    {
        var output = new StringBuilder();
        ConsoleFrameWriter.AppendTooSmall(output, 50, 20);
        string text = output.ToString();

        Assert.DoesNotContain('\n', text);
        Assert.DoesNotContain("\e[2J", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ZeroWidthWritesNoMessageCharacters()
    {
        var output = new StringBuilder();
        ConsoleFrameWriter.AppendTooSmall(output, 0, 0);

        Assert.Equal("\e[0m\e[1;1H", output.ToString());
    }

    [Theory]
    [InlineData(80, 24, false)]
    [InlineData(200, 60, false)]
    [InlineData(79, 24, true)]
    [InlineData(80, 23, true)]
    [InlineData(0, 0, true)]
    public void WindowsBelow80x24AreTooSmall(int cols, int rows, bool expected)
    {
        Assert.Equal(expected, ConsoleFrameWriter.IsTooSmall(cols, rows));
    }
}
