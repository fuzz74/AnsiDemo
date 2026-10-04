using System.Globalization;
using AnsiDemo.Contracts;

namespace AnsiDemo.Tracker;

/// <summary>Reads the text "Song format" of spec 4.3 into a <see cref="Song"/>.</summary>
public static class SongParser
{
    private static readonly RowCell EmptyCell = new RowCell.Empty();
    private static readonly RowCell OffCell = new RowCell.Off();
    private static readonly char[] TokenSeparators = [' ', '\t'];

    /// <exception cref="SongFormatException">The first problem in the text, with its 1-based line.</exception>
    public static Song Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        string[] lines = text.Split('\n');
        int lastLine = Math.Max(1, text.EndsWith('\n') ? lines.Length - 1 : lines.Length);

        int? bpm = null, rowsPerBeat = null;
        var instruments = new List<Instrument>();
        var instrumentIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var patterns = new List<Pattern>();
        var patternIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var order = new List<int>();
        string? openName = null;
        List<IReadOnlyList<RowCell>>? openRows = null;

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNo = i + 1;
            string line = StripComment(lines[i]).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (openRows is not null)
            {
                if (line == "end")
                {
                    patterns.Add(new Pattern(openName!, openRows));
                    openName = null;
                    openRows = null;
                }
                else
                {
                    openRows.Add(ParseRow(line, lineNo, instrumentIndex));
                }
                continue;
            }

            string[] tokens = line.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
            string directive = tokens[0];
            bool beforePatterns = patterns.Count == 0;
            switch (directive)
            {
                case "bpm":
                    RequirePlace(bpm is null, lineNo, "'bpm' may appear only once");
                    RequirePlace(beforePatterns, lineNo, "'bpm' must come before the first pattern");
                    bpm = ParsePositiveInt(tokens, lineNo);
                    break;

                case "rows-per-beat":
                    RequirePlace(rowsPerBeat is null, lineNo, "'rows-per-beat' may appear only once");
                    RequirePlace(beforePatterns, lineNo, "'rows-per-beat' must come before the first pattern");
                    rowsPerBeat = ParsePositiveInt(tokens, lineNo);
                    break;

                case "instrument":
                    RequirePlace(beforePatterns, lineNo, "instruments must come before the first pattern");
                    Instrument instrument = ParseInstrument(tokens, lineNo);
                    if (!instrumentIndex.TryAdd(instrument.Name, instruments.Count))
                    {
                        throw Fail(lineNo, $"duplicate instrument name '{instrument.Name}'");
                    }
                    instruments.Add(instrument);
                    break;

                case "pattern":
                    RequirePlace(order.Count == 0, lineNo, "patterns must come before the order");
                    if (tokens.Length != 2)
                    {
                        throw Fail(lineNo, "expected 'pattern <name>'");
                    }
                    string name = RequireName(tokens[1], lineNo);
                    if (!patternIndex.TryAdd(name, patterns.Count))
                    {
                        throw Fail(lineNo, $"duplicate pattern name '{name}'");
                    }
                    openName = name;
                    openRows = [];
                    break;

                case "order":
                    if (tokens.Length < 2)
                    {
                        throw Fail(lineNo, "expected 'order <name> [<name> ...]'");
                    }
                    for (int t = 1; t < tokens.Length; t++)
                    {
                        if (!patternIndex.TryGetValue(tokens[t], out int index))
                        {
                            throw Fail(lineNo, $"unknown pattern '{tokens[t]}'");
                        }
                        order.Add(index);
                    }
                    break;

                case "end":
                    throw Fail(lineNo, "'end' outside a pattern");

                default:
                    throw Fail(lineNo, $"unknown directive '{directive}'");
            }
        }

        if (openRows is not null)
        {
            throw Fail(lastLine, $"pattern '{openName}' has no 'end'");
        }
        if (bpm is null)
        {
            throw Fail(lastLine, "missing 'bpm'");
        }
        if (rowsPerBeat is null)
        {
            throw Fail(lastLine, "missing 'rows-per-beat'");
        }
        if (order.Count == 0)
        {
            throw Fail(lastLine, "missing 'order'");
        }

        return new Song(bpm.Value, rowsPerBeat.Value, instruments, patterns, order);
    }

    private static string StripComment(string line)
    {
        int comment = line.IndexOf(';');
        return comment < 0 ? line : line[..comment];
    }

    private static void RequirePlace(bool ok, int lineNo, string message)
    {
        if (!ok)
        {
            throw Fail(lineNo, message);
        }
    }

    private static int ParsePositiveInt(string[] tokens, int lineNo)
    {
        if (tokens.Length != 2)
        {
            throw Fail(lineNo, $"expected '{tokens[0]} <positive integer>'");
        }
        if (!int.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value <= 0)
        {
            throw Fail(lineNo, $"'{tokens[1]}' is not a positive integer");
        }
        return value;
    }

    private static Instrument ParseInstrument(string[] tokens, int lineNo)
    {
        if (tokens.Length != 5)
        {
            throw Fail(lineNo, "expected 'instrument <name> duty=<0.125|0.25|0.5> vol=<0..1> decay=<0..>'");
        }
        string name = RequireName(tokens[1], lineNo);
        double duty = ParseSetting(tokens[2], "duty", lineNo);
        if (duty is not (0.125 or 0.25 or 0.5))
        {
            throw Fail(lineNo, $"duty must be 0.125, 0.25 or 0.5, not '{tokens[2]}'");
        }
        double volume = ParseSetting(tokens[3], "vol", lineNo);
        if (volume is < 0 or > 1)
        {
            throw Fail(lineNo, $"vol must be between 0 and 1, not '{tokens[3]}'");
        }
        double decay = ParseSetting(tokens[4], "decay", lineNo);
        if (decay < 0)
        {
            throw Fail(lineNo, $"decay must be 0 or more, not '{tokens[4]}'");
        }
        return new Instrument(name, duty, volume, decay);
    }

    private static double ParseSetting(string token, string key, int lineNo)
    {
        string prefix = key + "=";
        if (!token.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw Fail(lineNo, $"expected '{prefix}<number>', not '{token}'");
        }
        if (!double.TryParse(token.AsSpan(prefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            || !double.IsFinite(value))
        {
            throw Fail(lineNo, $"'{token}' is not a number");
        }
        return value;
    }

    private static string RequireName(string name, int lineNo)
    {
        foreach (char c in name)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c == '_'))
            {
                throw Fail(lineNo, $"bad name '{name}': use letters, digits and '_'");
            }
        }
        return name;
    }

    private static IReadOnlyList<RowCell> ParseRow(string line, int lineNo, Dictionary<string, int> instrumentIndex)
    {
        string[] cells = line.Split('|');
        if (cells.Length != AudioFormat.Channels)
        {
            throw Fail(lineNo, $"a row needs {AudioFormat.Channels} cells separated by '|', found {cells.Length}");
        }
        var row = new RowCell[cells.Length];
        for (int ch = 0; ch < cells.Length; ch++)
        {
            row[ch] = ParseCell(cells[ch].Trim(), lineNo, instrumentIndex);
        }
        return row;
    }

    private static RowCell ParseCell(string cell, int lineNo, Dictionary<string, int> instrumentIndex)
    {
        if (cell == "---")
        {
            return EmptyCell;
        }
        if (cell == "off")
        {
            return OffCell;
        }

        string[] tokens = cell.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2)
        {
            throw Fail(lineNo, $"bad cell '{cell}': expected '---', 'off' or '<note> <instrument> [v<0-9>] [a<X><Y>]'");
        }

        int midi;
        try
        {
            midi = Notes.Parse(tokens[0]);
        }
        catch (FormatException e)
        {
            throw Fail(lineNo, e.Message);
        }
        if (!instrumentIndex.TryGetValue(tokens[1], out int instrument))
        {
            throw Fail(lineNo, $"unknown instrument '{tokens[1]}'");
        }

        int next = 2;
        int? volume = null;
        if (next < tokens.Length && tokens[next] is ['v', >= '0' and <= '9'])
        {
            volume = tokens[next][1] - '0';
            next++;
        }
        int arpX = 0, arpY = 0;
        if (next < tokens.Length && tokens[next] is ['a', char x, char y] && HexValue(x) >= 0 && HexValue(y) >= 0)
        {
            arpX = HexValue(x);
            arpY = HexValue(y);
            next++;
        }
        if (next < tokens.Length)
        {
            throw Fail(lineNo, $"bad cell '{cell}': unexpected '{tokens[next]}' (expected v<0-9> then a<X><Y>)");
        }
        return new RowCell.Note(midi, instrument, volume, arpX, arpY);
    }

    private static int HexValue(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'A' and <= 'F' => c - 'A' + 10,
        >= 'a' and <= 'f' => c - 'a' + 10,
        _ => -1,
    };

    private static SongFormatException Fail(int lineNo, string message) => new($"Line {lineNo}: {message}", lineNo);
}
