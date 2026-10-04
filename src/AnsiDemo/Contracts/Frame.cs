using System.Globalization;
using System.Text;

namespace AnsiDemo.Contracts;

public readonly record struct Cell(char Ch, Rgb Fg, Rgb Bg)
{
    public static readonly Cell Blank = new(' ', Rgb.White, Rgb.Black);
}

public sealed class Frame
{
    private readonly Cell[] _cells;

    /// <summary>A frame of cols x rows cells, all <see cref="Cell.Blank"/>.</summary>
    public Frame(int cols, int rows)
    {
        if (cols <= 0 || rows <= 0)
        {
            throw new ArgumentException($"Frame size must be positive, got {cols}x{rows}.");
        }
        Cols = cols;
        Rows = rows;
        _cells = new Cell[cols * rows];
        Clear();
    }

    public int Cols { get; }
    public int Rows { get; }

    /// <summary>Outside the frame: get returns Blank, set is ignored.</summary>
    public Cell this[int x, int y]
    {
        get => Contains(x, y) ? _cells[y * Cols + x] : Cell.Blank;
        set
        {
            if (Contains(x, y))
            {
                _cells[y * Cols + x] = value;
            }
        }
    }

    public void Clear() => Array.Fill(_cells, Cell.Blank);

    /// <summary>Writes text left to right from (x, y); characters outside the frame are dropped.</summary>
    public void Write(int x, int y, string text, Rgb fg, Rgb bg)
    {
        for (int i = 0; i < text.Length; i++)
        {
            this[x + i, y] = new Cell(text[i], fg, bg);
        }
    }

    public void CopyFrom(Frame other)
    {
        if (other.Cols != Cols || other.Rows != Rows)
        {
            throw new ArgumentException(
                $"Frame size mismatch: {other.Cols}x{other.Rows} cannot be copied into {Cols}x{Rows}.", nameof(other));
        }
        Array.Copy(other._cells, _cells, _cells.Length);
    }

    /// <summary>Cells with a non-space character and a non-black foreground or background.</summary>
    public int LitCount()
    {
        int count = 0;
        foreach (Cell cell in _cells)
        {
            if (cell.Ch != ' ' && (cell.Fg != Rgb.Black || cell.Bg != Rgb.Black))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Per row: SGR 38;2 when Fg changes from the previous cell (always for the first), then SGR 48;2 likewise
    /// for Bg, then the character; each row ends with ESC[0m and '\n'.
    /// </summary>
    public string ToAnsi()
    {
        var sb = new StringBuilder(Cols * Rows * 4);
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                Cell cell = _cells[y * Cols + x];
                if (x == 0 || cell.Fg != _cells[y * Cols + x - 1].Fg)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"\e[38;2;{cell.Fg.R};{cell.Fg.G};{cell.Fg.B}m");
                }
                if (x == 0 || cell.Bg != _cells[y * Cols + x - 1].Bg)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"\e[48;2;{cell.Bg.R};{cell.Bg.G};{cell.Bg.B}m");
                }
                sb.Append(cell.Ch);
            }
            sb.Append("\e[0m\n");
        }
        return sb.ToString();
    }

    /// <summary>Rows of characters joined by '\n' with trailing spaces trimmed, plus a final '\n'.</summary>
    public string ToText()
    {
        var sb = new StringBuilder((Cols + 1) * Rows);
        var row = new char[Cols];
        for (int y = 0; y < Rows; y++)
        {
            for (int x = 0; x < Cols; x++)
            {
                row[x] = _cells[y * Cols + x].Ch;
            }
            sb.Append(row.AsSpan().TrimEnd(' ')).Append('\n');
        }
        return sb.ToString();
    }

    private bool Contains(int x, int y) => (uint)x < (uint)Cols && (uint)y < (uint)Rows;
}
