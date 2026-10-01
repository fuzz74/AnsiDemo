using System.Globalization;
using System.Text;
using AnsiDemo.Contracts;

namespace AnsiDemo.Shell;

/// <summary>
/// The escape sequences the <see cref="ConsoleHost"/> writes: a frame drawn in place without clearing or scrolling,
/// and the message shown while the window is too small.
/// </summary>
public static class ConsoleFrameWriter
{
    public const int MinCols = 80, MinRows = 24;

    public static bool IsTooSmall(int cols, int rows) => cols < MinCols || rows < MinRows;

    public static string TooSmallMessage(int cols, int rows) =>
        string.Create(CultureInfo.InvariantCulture, $"Window too small: need {MinCols}x{MinRows}, have {cols}x{rows}");

    /// <summary>
    /// Appends <paramref name="frame"/>: per row <c>ESC[row;1H</c>, then the cells with <c>ESC[38;2;R;G;Bm</c> and
    /// <c>ESC[48;2;R;G;Bm</c> only where Fg or Bg differ from the previous cell (always for the first cell of a row),
    /// and <c>ESC[0m</c> at the end. No newline and no screen clear, so the terminal never scrolls.
    /// </summary>
    public static void Append(StringBuilder output, Frame frame)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(frame);
        for (int y = 0; y < frame.Rows; y++)
        {
            AppendCursor(output, y);
            Cell previous = default;
            for (int x = 0; x < frame.Cols; x++)
            {
                Cell cell = frame[x, y];
                if (x == 0 || cell.Fg != previous.Fg)
                {
                    AppendColor(output, 38, cell.Fg);
                }
                if (x == 0 || cell.Bg != previous.Bg)
                {
                    AppendColor(output, 48, cell.Bg);
                }
                output.Append(cell.Ch);
                previous = cell;
            }
        }
        output.Append("\e[0m");
    }

    /// <summary>
    /// Appends <see cref="TooSmallMessage"/> on row 0 in the default colours, clipped to <paramref name="cols"/>,
    /// and erases the rest of the window (<c>ESC[K</c> for the rest of row 0, <c>ESC[J</c> below it), without a newline.
    /// </summary>
    public static void AppendTooSmall(StringBuilder output, int cols, int rows)
    {
        ArgumentNullException.ThrowIfNull(output);
        string message = TooSmallMessage(cols, rows);
        int visible = Math.Clamp(cols, 0, message.Length);
        output.Append("\e[0m");
        AppendCursor(output, 0);
        output.Append(message, 0, visible);
        // Erasing at the last column would delete the message's last character, so ESC[K only when there is room.
        if (visible < cols)
        {
            output.Append("\e[K");
        }
        if (rows > 1)
        {
            AppendCursor(output, 1);
            output.Append("\e[J");
        }
    }

    private static void AppendCursor(StringBuilder output, int row) => output.Append("\e[").Append(row + 1).Append(";1H");

    private static void AppendColor(StringBuilder output, int layer, Rgb color) =>
        output.Append("\e[").Append(layer).Append(";2;")
              .Append(color.R).Append(';').Append(color.G).Append(';').Append(color.B).Append('m');
}
