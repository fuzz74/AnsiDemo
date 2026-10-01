using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Scroller;

public class ScrollerEffectTests
{
    private const string Name = "scroller";

    // N.1: no unseeded randomness, wall clock or console in the effect.
    private static readonly Regex Banned = new(@"System\.Random|DateTime|Stopwatch|Console\.", RegexOptions.CultureInvariant);

    [Fact]
    public void SameTimeRendersIdenticalFrames()
    {
        Frame first = Render(80, 24, 2.5);
        Frame second = Render(80, 24, 2.5);

        Assert.Equal(first.ToAnsi(), second.ToAnsi());
    }

    [Theory]
    [InlineData(0.0, "t0")]
    [InlineData(1.0, "t1")]
    [InlineData(4.0, "t4")]
    public void FrameMatchesGolden(double seconds, string golden)
    {
        Golden.Assert(Render(80, 24, seconds), golden);
    }

    [Fact]
    public void FrameAtOneSecondIsAnimatedAndLit()
    {
        Frame t0 = Render(80, 24, 0);
        Frame t1 = Render(80, 24, 1);

        Assert.NotEqual(t0.ToAnsi(), t1.ToAnsi());
        Assert.True(t1.LitCount() >= 39, $"LitCount {t1.LitCount()} is below 2% of 1920 cells.");
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void RendersInBothModesAtMinimumAndLargeSizes(PixelMode mode, int cols, int rows)
    {
        Frame frame = SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, 1.5);

        Assert.True(frame.LitCount() > 0);
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void MeanFrameTimeIsWithinBudget()
    {
        double ms = Perf.MeanFrameMs(Name, PixelMode.HalfBlock, 200, 60);

        Assert.True(ms <= 33, $"Mean frame time {ms:F2} ms exceeds 33 ms.");
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 123, 37)]
    public void LettersStayInTheMiddleThirdOfTheRows(PixelMode mode, int cols, int rows)
    {
        int top = rows / 3;
        int bottom = 2 * rows / 3 - 1;
        foreach (double seconds in new[] { 0.0, 0.5, 1.0, 2.0, 3.3 })
        {
            Frame frame = SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, seconds);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (IsLetter(frame[x, y].Ch))
                    {
                        Assert.InRange(y, top, bottom);
                    }
                }
            }
        }
    }

    [Fact]
    public void WaveSpansAtLeastSixRows()
    {
        int minRow = int.MaxValue;
        int maxRow = int.MinValue;
        foreach (double seconds in new[] { 0.0, 0.2, 0.4, 0.6 })
        {
            Frame frame = Render(80, 24, seconds);
            for (int y = 0; y < frame.Rows; y++)
            {
                for (int x = 0; x < frame.Cols; x++)
                {
                    if (IsLetter(frame[x, y].Ch))
                    {
                        minRow = Math.Min(minRow, y);
                        maxRow = Math.Max(maxRow, y);
                    }
                }
            }
        }

        Assert.True(maxRow - minRow + 1 >= 6, $"Letters span rows {minRow}..{maxRow} only.");
    }

    [Fact]
    public void TextScrollsLeftAtTwentyToThirtyColumnsPerSecond()
    {
        string before = Columns(Render(80, 24, 1));
        string after = Columns(Render(80, 24, 2));

        int shift = -1;
        for (int s = 1; s < 60; s++)
        {
            if (before.AsSpan(s).SequenceEqual(after.AsSpan(0, before.Length - s)))
            {
                shift = s;
                break;
            }
        }

        Assert.InRange(shift, 20, 30);
    }

    [Fact]
    public void LettersAreTheScrollerTextFillingTheWidthFromTheStart()
    {
        string repeated = ScrollerText.Value + ScrollerText.Value;
        string expected = repeated[..80].Replace(' ', '.');

        Assert.Equal(expected, Columns(Render(80, 24, 0)));
    }

    [Fact]
    public void LettersAreColouredByTheirPositionInTheText()
    {
        Frame frame = Render(80, 24, 0);
        var colours = new Dictionary<char, HashSet<Rgb>>();
        for (int y = 0; y < frame.Rows; y++)
        {
            for (int x = 0; x < frame.Cols; x++)
            {
                Cell cell = frame[x, y];
                if (IsLetter(cell.Ch))
                {
                    // Bright and fully saturated: one channel at full value, one at zero.
                    Rgb fg = cell.Fg;
                    Assert.Equal(255, Math.Max(fg.R, Math.Max(fg.G, fg.B)));
                    Assert.Equal(0, Math.Min(fg.R, Math.Min(fg.G, fg.B)));
                    if (!colours.TryGetValue(cell.Ch, out HashSet<Rgb>? set))
                    {
                        colours[cell.Ch] = set = [];
                    }
                    set.Add(cell.Fg);
                }
            }
        }

        // The same letter at different positions in the text gets different colours.
        Assert.True(colours['E'].Count > 1);
    }

    [Fact]
    public void EffectAvoidsBannedApis()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Scroller");
        string[] files = Directory.GetFiles(folder, "*.cs");
        Assert.NotEmpty(files);

        var hits = new List<string>();
        foreach (string file in files)
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (Banned.IsMatch(lines[i]))
                {
                    hits.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }
        Assert.Empty(hits);
    }

    private static Frame Render(int cols, int rows, double seconds) =>
        SceneRunner.RenderAt(SceneSpec.Solo(Name, PixelMode.HalfBlock), cols, rows, seconds);

    /// <summary>The letter drawn in each column, or '.' where the column has none.</summary>
    private static string Columns(Frame frame)
    {
        var columns = new char[frame.Cols];
        for (int x = 0; x < frame.Cols; x++)
        {
            columns[x] = '.';
            for (int y = 0; y < frame.Rows; y++)
            {
                char ch = frame[x, y].Ch;
                if (IsLetter(ch))
                {
                    Assert.Equal('.', columns[x]);
                    columns[x] = ch;
                }
            }
        }
        return new string(columns);
    }

    /// <summary>Anything but the blank and half-block background cells the renderers leave in a solo scene.</summary>
    private static bool IsLetter(char ch) => ch != ' ' && ch != '▀' && (ch < '⠀' || ch > '⣿');

    private static string FindRepoRoot([CallerFilePath] string callerFile = "")
    {
        for (string? dir = Path.GetDirectoryName(callerFile); dir is not null; dir = Path.GetDirectoryName(dir))
        {
            if (File.Exists(Path.Combine(dir, "AnsiDemo.slnx")))
            {
                return dir;
            }
        }
        throw new InvalidOperationException($"AnsiDemo.slnx not found above {callerFile}.");
    }
}
